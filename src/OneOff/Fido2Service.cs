using System.Text;
using Fido2NetLib;
using Fido2NetLib.Objects;

namespace OneOff;

public interface IFido2Service
{
	AssertionOptions AssertionOptionsPost(string username, string userVerification);

	Task<VerifyAssertionResult> MakeAssertion(
		AssertionOptions options,
		AuthenticatorAssertionRawResponse clientResponse
	);

	Task<RegisteredPublicKeyCredential> MakeCredential(
		CredentialCreateOptions options,
		AuthenticatorAttestationRawResponse attestationResponse
	);

	CredentialCreateOptions MakeCredentialOptions(
		string username,
		string displayName,
		string attType,
		string authType,
		string residentKey,
		string userVerification
	);
}

public class Fido2Service : IFido2Service
{
	private readonly IFido2 _fido2 = new Fido2(
		new Fido2Configuration
		{
			ServerDomain = "localhost",
			ServerName = "Fido2 Test",
			Origins = new HashSet<string> { "https://localhost:14555", "http://localhost:5000" },
		}
	);

	private DevelopmentInMemoryStore DemoStorage { get; } = new();

	public AssertionOptions AssertionOptionsPost(string username, string userVerification)
	{
		try
		{
			List<PublicKeyCredentialDescriptor> existingCredentials = [];

			if (!string.IsNullOrEmpty(username))
			{
				// 1. Get user from DB
				var user =
					DemoStorage.GetUser(username) ?? throw new ArgumentException("Username was not registered");

				// 2. Get registered credentials from database
				existingCredentials = DemoStorage.GetCredentialsByUser(user).Select(c => c.Descriptor).ToList();
			}

			var exts = new AuthenticationExtensionsClientInputs
			{
				Extensions = true,
				UserVerificationMethod = true,
			};

			// 3. Create options
			var uv = string.IsNullOrEmpty(userVerification)
				? UserVerificationRequirement.Discouraged
				: userVerification.ToEnum<UserVerificationRequirement>();

			var options = _fido2.GetAssertionOptions(
				new GetAssertionOptionsParams
				{
					AllowedCredentials = existingCredentials,
					UserVerification = uv,
					Extensions = exts,
				}
			);

			// 5. Return options to client
			return options;
		}
		catch (Exception e)
		{
			return null;
		}
	}

	public async Task<VerifyAssertionResult> MakeAssertion(
		AssertionOptions options,
		AuthenticatorAssertionRawResponse clientResponse
	)
	{
		// 2. Get registered credential from database
		var creds =
			DemoStorage.GetCredentialById(clientResponse.RawId) ?? throw new Exception("Unknown credentials");

		// 3. Get credential counter from database
		var storedCounter = creds.SignCount;

		// 4. Create callback to check if the user handle owns the credentialId
		IsUserHandleOwnerOfCredentialIdAsync callback = async (args, cancellationToken) =>
		{
			var storedCreds = await DemoStorage.GetCredentialsByUserHandleAsync(
				args.UserHandle,
				cancellationToken
			);

			return storedCreds.Exists(c => c.Descriptor.Id.SequenceEqual(args.CredentialId));
		};

		// 5. Make the assertion
		var res = await _fido2.MakeAssertionAsync(
			new MakeAssertionParams
			{
				AssertionResponse = clientResponse,
				OriginalOptions = options,
				StoredPublicKey = creds.PublicKey,
				StoredSignatureCounter = storedCounter,
				IsUserHandleOwnerOfCredentialIdCallback = callback,
			}
		);

		// 6. Store the updated counter
		DemoStorage.UpdateCounter(res.CredentialId, res.SignCount);

		return res;
	}

	public async Task<RegisteredPublicKeyCredential> MakeCredential(
		CredentialCreateOptions options,
		AuthenticatorAttestationRawResponse attestationResponse
	)
	{
		// 2. Create callback so that lib can verify credential id is unique to this user
		IsCredentialIdUniqueToUserAsyncDelegate callback = async (args, cancellationToken) =>
		{
			var users = await DemoStorage.GetUsersByCredentialIdAsync(args.CredentialId, cancellationToken);

			if (users.Count > 0)
			{
				return false;
			}

			return true;
		};

		// 2. Verify and make the credentials
		var credential = await _fido2.MakeNewCredentialAsync(
			new MakeNewCredentialParams
			{
				AttestationResponse = attestationResponse,
				OriginalOptions = options,
				IsCredentialIdUniqueToUserCallback = callback,
			}
		);

		// 3. Store the credentials in db
		DemoStorage.AddCredentialToUser(
			options.User,
			new StoredCredential
			{
				Id = credential.Id,
				PublicKey = credential.PublicKey,
				UserHandle = credential.User.Id,
				SignCount = credential.SignCount,
				AttestationFormat = credential.AttestationFormat,
				RegDate = DateTimeOffset.UtcNow,
				AaGuid = credential.AaGuid,
				Transports = credential.Transports,
				IsBackupEligible = credential.IsBackupEligible,
				IsBackedUp = credential.IsBackedUp,
				AttestationObject = credential.AttestationObject,
				AttestationClientDataJson = credential.AttestationClientDataJson,
			}
		);

		return credential;
	}

	public CredentialCreateOptions MakeCredentialOptions(
		string username,
		string displayName,
		string attType,
		string authType,
		string residentKey,
		string userVerification
	)
	{
		if (string.IsNullOrEmpty(username))
		{
			username = $"{displayName} (Usernameless user created at {DateTime.UtcNow})";
		}

		// 1. Get user from DB by username (in our example, auto create missing users)
		var user = DemoStorage.GetOrAddUser(
			username,
			() =>
				new Fido2User
				{
					DisplayName = displayName,
					Name = username,
					Id = Encoding.UTF8.GetBytes(username), // byte representation of userID is required
				}
		);

		// 2. Get user existing keys by username
		var existingKeys = DemoStorage.GetCredentialsByUser(user).Select(c => c.Descriptor).ToList();

		// 3. Create options
		var authenticatorSelection = new AuthenticatorSelection
		{
			ResidentKey = residentKey.ToEnum<ResidentKeyRequirement>(),
			UserVerification = userVerification.ToEnum<UserVerificationRequirement>(),
		};

		if (!string.IsNullOrEmpty(authType))
		{
			authenticatorSelection.AuthenticatorAttachment = authType.ToEnum<AuthenticatorAttachment>();
		}

		var exts = new AuthenticationExtensionsClientInputs
		{
			Extensions = true,
			UserVerificationMethod = true,
			CredProps = true,
		};

		var options = _fido2.RequestNewCredential(
			new RequestNewCredentialParams
			{
				User = user,
				ExcludeCredentials = existingKeys,
				AuthenticatorSelection = authenticatorSelection,
				AttestationPreference = attType.ToEnum<AttestationConveyancePreference>(),
				Extensions = exts,
			}
		);

		return options;
	}
}
