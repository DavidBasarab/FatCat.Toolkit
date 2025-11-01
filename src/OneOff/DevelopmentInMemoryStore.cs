using System.Collections.Concurrent;
using Fido2NetLib;

namespace OneOff;

public class DevelopmentInMemoryStore
{
	private readonly List<StoredCredential> _storedCredentials = new();
	private readonly ConcurrentDictionary<string, Fido2User> _storedUsers = new();

	public void AddCredentialToUser(Fido2User user, StoredCredential credential)
	{
		credential.UserId = user.Id;
		_storedCredentials.Add(credential);
	}

	public StoredCredential? GetCredentialById(byte[] id)
	{
		return _storedCredentials.FirstOrDefault(c => c.Descriptor.Id.AsSpan().SequenceEqual(id));
	}

	public List<StoredCredential> GetCredentialsByUser(Fido2User user)
	{
		return _storedCredentials.Where(c => c.UserId.AsSpan().SequenceEqual(user.Id)).ToList();
	}

	public Task<List<StoredCredential>> GetCredentialsByUserHandleAsync(
		byte[] userHandle,
		CancellationToken cancellationToken = default
	)
	{
		return Task.FromResult(
			_storedCredentials.Where(c => c.UserHandle.AsSpan().SequenceEqual(userHandle)).ToList()
		);
	}

	public Fido2User GetOrAddUser(string username, Func<Fido2User> addCallback)
	{
		return _storedUsers.GetOrAdd(username, addCallback());
	}

	public Fido2User? GetUser(string username)
	{
		_storedUsers.TryGetValue(username, out var user);
		return user;
	}

	public Task<List<Fido2User>> GetUsersByCredentialIdAsync(
		byte[] credentialId,
		CancellationToken cancellationToken = default
	)
	{
		// our in-mem storage does not allow storing multiple users for a given credentialId. Yours shouldn't either.
		var cred = _storedCredentials.FirstOrDefault(c => c.Descriptor.Id.AsSpan().SequenceEqual(credentialId));

		if (cred is null)
		{
			return Task.FromResult(new List<Fido2User>());
		}

		return Task.FromResult(
			_storedUsers.Where(u => u.Value.Id.SequenceEqual(cred.UserId)).Select(u => u.Value).ToList()
		);
	}

	public void UpdateCounter(byte[] credentialId, uint counter)
	{
		var cred = _storedCredentials.First(c => c.Descriptor.Id.AsSpan().SequenceEqual(credentialId));
		cred.SignCount = counter;
	}
}
