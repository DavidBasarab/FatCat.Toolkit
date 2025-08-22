namespace FatCat.Toolkit.Cryptography;

public interface IAesKeyGenerator
{
	byte[] CreateIv();

	byte[] CreateKey(AesKeySize keySize);
}

public class AesKeyGenerator(IGenerator generator) : IAesKeyGenerator
{
	public byte[] CreateIv()
	{
		// AES-GCM standard 96-bit nonce (12 bytes)
		return generator.Bytes(12).ToArray();
	}

	public byte[] CreateKey(AesKeySize keySize)
	{
		return generator.Bytes((int)keySize / 8).ToArray();
	}
}
