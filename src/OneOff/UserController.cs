using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.Json;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace OneOff;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
	private static readonly DevelopmentInMemoryStore _demoStorage = new();
	private static readonly Dictionary<string, AssertionOptions> _pendingAssertions = new();
	private static readonly Dictionary<string, CredentialCreateOptions> _pendingCredentials = new();

	private static readonly SigningCredentials _signingCredentials = new(
		new SymmetricSecurityKey(
			"This is my very long and totally secret key for signing tokens, which clients may never learn or I'd have to replace it."u8.ToArray()
		),
		SecurityAlgorithms.HmacSha256
	);

	private readonly IFido2 fido = FidoFactory.GetFido2();

	// --------------------------------------------------------
	//  REGISTER: Step 2 - Submit credential
	// --------------------------------------------------------
	[HttpPut("{username}/credential")]
	public async Task<string> CreateCredentialAsync(
		[FromRoute] string username,
		[FromBody] AuthenticatorAttestationRawResponse attestationResponse,
		CancellationToken cancellationToken
	)
	{
		try
		{
			if (!_pendingCredentials.TryGetValue(username, out var options))
			{
				return "Error: registration options not found (request /credential-options first)";
			}

			var credential = await fido.MakeNewCredentialAsync(
				new MakeNewCredentialParams
				{
					AttestationResponse = attestationResponse,
					OriginalOptions = options,
					IsCredentialIdUniqueToUserCallback = CredentialIdUniqueToUserAsync,
				},
				cancellationToken
			);

			_demoStorage.AddCredentialToUser(
				options.User,
				new StoredCredential
				{
					AttestationFormat = credential.AttestationFormat,
					Id = credential.Id,
					PublicKey = credential.PublicKey,
					UserHandle = credential.User.Id,
					SignCount = credential.SignCount,
					RegDate = DateTimeOffset.UtcNow,
					AaGuid = credential.AaGuid,
					Transports = credential.Transports,
					IsBackupEligible = credential.IsBackupEligible,
					IsBackedUp = credential.IsBackedUp,
					AttestationObject = credential.AttestationObject,
					AttestationClientDataJson = credential.AttestationClientDataJson,
				}
			);

			_pendingCredentials.Remove(username);

			return "OK";
		}
		catch (Exception e)
		{
			return $"Error: {FormatException(e)}";
		}
	}

	// --------------------------------------------------------
	//  REGISTER: Step 1 - Get credential options
	// --------------------------------------------------------
	[HttpGet("{username}/credential-options")]
	[HttpGet("credential-options")]
	public IActionResult GetCredentialOptions(
		[FromRoute] string? username,
		[FromQuery] string? displayName,
		[FromQuery] AttestationConveyancePreference? attestationType,
		[FromQuery] AuthenticatorAttachment? authenticator,
		[FromQuery] UserVerificationRequirement? userVerification,
		[FromQuery] ResidentKeyRequirement? residentKey
	)
	{
		var key = username ?? Guid.NewGuid().ToString("N");
		displayName ??= username ?? "User";

		// 1️⃣ Create or fetch the user
		var user = _demoStorage.GetOrAddUser(
			username ?? displayName,
			() =>
				new Fido2User
				{
					DisplayName = displayName,
					Name = username ?? displayName,
					Id = Encoding.UTF8.GetBytes(username ?? displayName),
				}
		);

		// 2️⃣ Get existing credentials
		var existingKeys = _demoStorage.GetCredentialsByUser(user).Select(c => c.Descriptor).ToList();

		// 3️⃣ Authenticator preferences
		var authenticatorSelection = new AuthenticatorSelection
		{
			AuthenticatorAttachment = authenticator ?? AuthenticatorAttachment.CrossPlatform,
			ResidentKey = residentKey ?? ResidentKeyRequirement.Discouraged,
			UserVerification = userVerification ?? UserVerificationRequirement.Required,
		};

		// 4️⃣ Request options
		var options = fido.RequestNewCredential(
			new RequestNewCredentialParams
			{
				User = user,
				ExcludeCredentials = existingKeys,
				AuthenticatorSelection = authenticatorSelection,
				AttestationPreference = attestationType ?? AttestationConveyancePreference.None,
				Extensions = new AuthenticationExtensionsClientInputs
				{
					Extensions = true,
					UserVerificationMethod = true,
					CredProps = true,
				},
			}
		);

		// ✅ Normalize / fix WebAuthn required fields
		options.Rp.Id = "localhost";
		options.Rp.Name = "OneOff FIDO2 Demo";
		options.Attestation = AttestationConveyancePreference.None;

		// ✅ Replace invalid PubKeyCredParams
		options.PubKeyCredParams = new List<PubKeyCredParam>
		{
			new(COSE.Algorithm.ES256),
			new(COSE.Algorithm.RS256),
		};

		// ✅ Normalize enums to lowercase for browser validation
		options.AuthenticatorSelection.AuthenticatorAttachment = AuthenticatorAttachment.CrossPlatform;
		options.AuthenticatorSelection.ResidentKey = ResidentKeyRequirement.Discouraged;
		options.AuthenticatorSelection.UserVerification = UserVerificationRequirement.Required;

		// 5️⃣ Cache options
		_pendingCredentials[key] = options;

		ConsoleLog.WriteMagenta("Credential Options:");
		ConsoleLog.WriteMagenta(new JsonOperations().Serialize(options));

		// ✅ Wrap in `publicKey` for JS interop
		var result = new { publicKey = options };
		return new JsonResult(result);
	}

	// --------------------------------------------------------
	//  LOGIN: Step 2 - Verify assertion
	// --------------------------------------------------------
	[HttpPost("assertion")]
	public async Task<string> MakeAssertionAsync(
		[FromBody] AuthenticatorAssertionRawResponse clientResponse,
		CancellationToken cancellationToken
	)
	{
		try
		{
			var response = JsonSerializer.Deserialize<AuthenticatorResponse>(
				clientResponse.Response.ClientDataJson
			);

			if (response is null)
			{
				return "Error: Could not deserialize client data";
			}

			var key = Convert.ToBase64String(response.Challenge);

			if (!_pendingAssertions.TryGetValue(key, out var options))
			{
				return "Error: Challenge not found (request /assertion-options first)";
			}

			_pendingAssertions.Remove(key);

			var creds =
				_demoStorage.GetCredentialById(clientResponse.RawId) ?? throw new Exception("Unknown credentials");

			var res = await fido.MakeAssertionAsync(
				new MakeAssertionParams
				{
					AssertionResponse = clientResponse,
					OriginalOptions = options,
					StoredPublicKey = creds.PublicKey,
					StoredSignatureCounter = creds.SignCount,
					IsUserHandleOwnerOfCredentialIdCallback = UserHandleOwnerOfCredentialIdAsync,
				},
				cancellationToken
			);

			_demoStorage.UpdateCounter(res.CredentialId, res.SignCount);

			// ✅ Return JWT token after successful verification
			var handler = new JwtSecurityTokenHandler();

			var token = handler.CreateEncodedJwt(
				HttpContext.Request.Host.Host,
				HttpContext.Request.Headers.Referer,
				new ClaimsIdentity(
					new[] { new Claim(ClaimTypes.Actor, Encoding.UTF8.GetString(creds.UserHandle)) }
				),
				DateTime.Now.AddMinutes(-1),
				DateTime.Now.AddDays(1),
				DateTime.Now,
				_signingCredentials,
				null
			);

			return token is null ? "Error: Token couldn't be created" : $"Bearer {token}";
		}
		catch (Exception e)
		{
			return $"Error: {FormatException(e)}";
		}
	}

	// --------------------------------------------------------
	//  LOGIN: Step 1 - Get assertion options
	// --------------------------------------------------------
	[HttpGet("{username}/assertion-options")]
	[HttpGet("assertion-options")]
	public IActionResult MakeAssertionOptions(
		[FromRoute] string? username,
		[FromQuery] UserVerificationRequirement? userVerification
	)
	{
		var existingKeys = new List<PublicKeyCredentialDescriptor>();

		if (!string.IsNullOrEmpty(username))
		{
			var user = _demoStorage.GetUser(username);

			if (user != null)
			{
				existingKeys = _demoStorage.GetCredentialsByUser(user).Select(c => c.Descriptor).ToList();
			}
		}

		var exts = new AuthenticationExtensionsClientInputs { UserVerificationMethod = true, Extensions = true };

		var options = fido.GetAssertionOptions(
			new GetAssertionOptionsParams
			{
				AllowedCredentials = existingKeys,
				UserVerification = userVerification ?? UserVerificationRequirement.Required,
				Extensions = exts,
			}
		);

		var key = Convert.ToBase64String(options.Challenge);
		_pendingAssertions[key] = options;

		return new JsonResult(new { publicKey = options });
	}

	// --------------------------------------------------------
	//  Helper callbacks
	// --------------------------------------------------------
	private static async Task<bool> CredentialIdUniqueToUserAsync(
		IsCredentialIdUniqueToUserParams args,
		CancellationToken cancellationToken
	)
	{
		var users = await _demoStorage.GetUsersByCredentialIdAsync(args.CredentialId, cancellationToken);
		return users.Count <= 0;
	}

	private static string FormatException(Exception e)
	{
		return $"{e.Message} {e.InnerException?.Message ?? string.Empty}";
	}

	private static async Task<bool> UserHandleOwnerOfCredentialIdAsync(
		IsUserHandleOwnerOfCredentialIdParams args,
		CancellationToken cancellationToken
	)
	{
		var storedCreds = await _demoStorage.GetCredentialsByUserHandleAsync(args.UserHandle, cancellationToken);
		return storedCreds.Exists(c => c.Descriptor.Id.SequenceEqual(args.CredentialId));
	}
}
