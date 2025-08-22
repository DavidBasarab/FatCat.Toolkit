using System;
using System.Security.Cryptography;
using FatCat.Toolkit.Cryptography;

namespace OneOffBlazor;

// A WebAssembly-safe implementation of IFatCatAesEncryption using AES-GCM.
// Encrypt returns ciphertext || tag (tag appended to the end). The IV parameter is used as the 12-byte nonce.
public class WasmAesEncryption : IFatCatAesEncryption
{
	private const int TagSizeBytes = 16; // AES-GCM standard tag length

	public byte[] Encrypt(byte[] openData, byte[] key, byte[] iv)
	{
		ArgumentNullException.ThrowIfNull(openData);
		ArgumentNullException.ThrowIfNull(key);
		ArgumentNullException.ThrowIfNull(iv);

		if (iv.Length != 12)
		{
			throw new ArgumentException("AES-GCM requires a 12-byte IV (nonce).", nameof(iv));
		}

		var ciphertext = new byte[openData.Length];
		var tag = new byte[TagSizeBytes];

		using var aesGcm = new AesGcm(key);
		aesGcm.Encrypt(iv, openData, ciphertext, tag);

		// Return ciphertext concatenated with tag so the existing interface can carry the tag to decrypt
		var result = new byte[ciphertext.Length + tag.Length];
		Buffer.BlockCopy(ciphertext, 0, result, 0, ciphertext.Length);
		Buffer.BlockCopy(tag, 0, result, ciphertext.Length, tag.Length);
		return result;
	}

	public byte[] Decrypt(byte[] cypherData, byte[] key, byte[] iv)
	{
		ArgumentNullException.ThrowIfNull(cypherData);
		ArgumentNullException.ThrowIfNull(key);
		ArgumentNullException.ThrowIfNull(iv);

		if (iv.Length != 12)
		{
			throw new ArgumentException("AES-GCM requires a 12-byte IV (nonce).", nameof(iv));
		}
		if (cypherData.Length < TagSizeBytes)
		{
			throw new ArgumentException(
				"Ciphertext is too short to contain an authentication tag.",
				nameof(cypherData)
			);
		}

		int cipherLen = cypherData.Length - TagSizeBytes;
		var ciphertext = new byte[cipherLen];
		var tag = new byte[TagSizeBytes];
		Buffer.BlockCopy(cypherData, 0, ciphertext, 0, cipherLen);
		Buffer.BlockCopy(cypherData, cipherLen, tag, 0, TagSizeBytes);

		var plaintext = new byte[cipherLen];
		using var aesGcm = new AesGcm(key);
		aesGcm.Decrypt(iv, ciphertext, tag, plaintext);
		return plaintext;
	}
}
