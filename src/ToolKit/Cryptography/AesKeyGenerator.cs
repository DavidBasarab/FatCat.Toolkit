namespace FatCat.Toolkit.Cryptography;

public interface IAesKeyGenerator
{
	public byte[] CreateIv();

	public byte[] CreateKey(AesKeySize keySize);
}

public class AesKeyGenerator(IGenerator generator) : IAesKeyGenerator
{
	public byte[] CreateIv()
	{
		// AES-GCM standard 96-bit nonce (12 bytes), drawn from a cryptographically secure RNG
		return generator.CsprngBytes(12);
	}

	public byte[] CreateKey(AesKeySize keySize)
	{
		return generator.CsprngBytes((int)keySize / 8);
	}
}
