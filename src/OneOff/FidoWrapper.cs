using System.Collections.Concurrent;
using Fido2NetLib.Objects;

namespace OneOff;

public interface IInMemoryFido2Store
{
	/// <summary>
	///  Save the current challenge so we can verify the next step.
	/// </summary>
	void AddChallenge(string username, byte[] challenge);

	/// <summary>
	///  Add a credential for a given username.
	/// </summary>
	void AddCredential(string username, StoredCredential credential);

	/// <summary>
	///  Retrieve the last challenge for this user.
	/// </summary>
	byte[] GetChallenge(string username);

	/// <summary>
	///  Get all credentials registered for a user.
	/// </summary>
	IEnumerable<StoredCredential> GetCredentialsByUser(string username);
}

public class InMemoryFido2Store : IInMemoryFido2Store
{
	// Temporary challenge storage per username
	private readonly ConcurrentDictionary<string, byte[]> userChallenges = new();

	// Store registered credentials per username
	private readonly ConcurrentDictionary<string, List<StoredCredential>> userCredentials = new();

	/// <summary>
	///  Save the current challenge so we can verify the next step.
	/// </summary>
	public void AddChallenge(string username, byte[] challenge)
	{
		userChallenges[username] = challenge;
	}

	/// <summary>
	///  Add a credential for a given username.
	/// </summary>
	public void AddCredential(string username, StoredCredential credential)
	{
		var list = userCredentials.GetOrAdd(username, _ => new List<StoredCredential>());

		lock (list)
		{
			list.Add(credential);
		}
	}

	/// <summary>
	///  Retrieve the last challenge for this user.
	/// </summary>
	public byte[] GetChallenge(string username)
	{
		if (userChallenges.TryGetValue(username, out var challenge))
		{
			return challenge;
		}

		throw new InvalidOperationException($"No challenge found for user {username}");
	}

	/// <summary>
	///  Get all credentials registered for a user.
	/// </summary>
	public IEnumerable<StoredCredential> GetCredentialsByUser(string username)
	{
		if (userCredentials.TryGetValue(username, out var creds))
		{
			return creds;
		}

		return [];
	}
}

public class StoredCredential
{
	public Guid AaGuid { get; set; } = Guid.Empty;

	public string CredType { get; set; } = "public-key";

	public PublicKeyCredentialDescriptor Descriptor { get; set; } = null!;

	public byte[] Id { get; set; } = [];

	public byte[] PublicKey { get; set; } = [];

	public uint SignatureCounter { get; set; }

	public string UserName { get; set; } = string.Empty;
}
