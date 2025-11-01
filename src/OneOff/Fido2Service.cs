using System.Text;
using Fido2NetLib;
using Fido2NetLib.Objects;

namespace OneOff;

public interface IFido2Service
{
	void MakeCredentialOptions(
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
	private DevelopmentInMemoryStore DemoStorage { get; } = new();

	private readonly IFido2 _fido2 = new Fido2(
		new Fido2Configuration
		{
			ServerDomain = "localhost",
			ServerName = "Fido2 Test",
			Origins = new HashSet<string> { "https://localhost:14555", "http://localhost:5000" },
		}
	);

	public void MakeCredentialOptions(
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
	}
}
