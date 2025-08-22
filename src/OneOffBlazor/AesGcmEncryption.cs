using System.Security.Cryptography;
using FatCat.Toolkit.Cryptography;

namespace OneOffBlazor;

public class AesGcmEncryption : IFatCatAesEncryption
{
	private const int TagSizeBytes = 16; // 128-bit tag

	public byte[] Decrypt(byte[] cypherData, byte[] key, byte[] iv)
	{
		if (iv is not { Length: 12 })
		{
			throw new ArgumentException("AES-GCM requires a 12-byte IV (nonce).", nameof(iv));
		}

		if (cypherData == null || cypherData.Length < TagSizeBytes)
		{
			throw new ArgumentException("Ciphertext is too short.", nameof(cypherData));
		}

		var cipherLen = cypherData.Length - TagSizeBytes;
		var ciphertext = new byte[cipherLen];
		var tag = new byte[TagSizeBytes];
		Buffer.BlockCopy(cypherData, 0, ciphertext, 0, cipherLen);
		Buffer.BlockCopy(cypherData, cipherLen, tag, 0, TagSizeBytes);

		var plaintext = new byte[cipherLen];
		using var aesGcm = new AesGcm(key, TagSizeBytes);
		aesGcm.Decrypt(iv, ciphertext, tag, plaintext);

		return plaintext;
	}

	public byte[] Encrypt(byte[] openData, byte[] key, byte[] iv)
	{
		if (iv == null || iv.Length != 12)
		{
			throw new ArgumentException("AES-GCM requires a 12-byte IV (nonce).", nameof(iv));
		}

		if (openData == null)
		{
			throw new ArgumentNullException(nameof(openData));
		}

		var ciphertext = new byte[openData.Length];
		var tag = new byte[TagSizeBytes];

		using var aesGcm = new AesGcm(key, TagSizeBytes);

		aesGcm.Encrypt(iv, openData, ciphertext, tag);

		var result = new byte[ciphertext.Length + tag.Length];

		Buffer.BlockCopy(ciphertext, 0, result, 0, ciphertext.Length);
		Buffer.BlockCopy(tag, 0, result, ciphertext.Length, tag.Length);

		return result;
	}
}
