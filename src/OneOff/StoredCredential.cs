using FatCat.Toolkit;
using Fido2NetLib.Objects;

namespace OneOff;

public class StoredCredential : EqualObject
{
	public Guid AaGuid { get; set; } = Guid.Empty;

	public string CredType { get; set; } = "public-key";

	public PublicKeyCredentialDescriptor Descriptor { get; set; } = null!;

	public List<byte[]> DevicePublicKeys { get; set; } = new();

	public byte[] Id { get; set; } = [];

	public byte[] PublicKey { get; set; } = [];

	public uint SignatureCounter { get; set; }

	public string UserName { get; set; } = string.Empty;
}
