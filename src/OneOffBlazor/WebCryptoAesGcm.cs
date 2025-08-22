using FatCat.Toolkit.Cryptography;
using Microsoft.JSInterop;

namespace OneOffBlazor;

public class WebCryptoAesGcm(IJSRuntime javaScript) : IFatCatAesEncryption
{
	public async Task<byte[]> Decrypt(byte[] cypherData, byte[] key, byte[] iv)
	{
		ArgumentNullException.ThrowIfNull(cypherData);
		ArgumentNullException.ThrowIfNull(key);
		ArgumentNullException.ThrowIfNull(iv);

		if (iv.Length != 12)
		{
			throw new ArgumentException("AES-GCM requires a 12-byte IV (nonce).", nameof(iv));
		}

		var ctB64 = Convert.ToBase64String(cypherData);
		var keyB64 = Convert.ToBase64String(key);
		var ivB64 = Convert.ToBase64String(iv);
		var ptB64 = await javaScript.InvokeAsync<string>("cryptoInterop.decrypt", ctB64, keyB64, ivB64);
		return Convert.FromBase64String(ptB64);
	}

	public async Task<byte[]> Encrypt(byte[] openData, byte[] key, byte[] iv)
	{
		ArgumentNullException.ThrowIfNull(openData);
		ArgumentNullException.ThrowIfNull(key);
		ArgumentNullException.ThrowIfNull(iv);

		if (iv.Length != 12)
		{
			throw new ArgumentException("AES-GCM requires a 12-byte IV (nonce).", nameof(iv));
		}

		var ptB64 = Convert.ToBase64String(openData);
		var keyB64 = Convert.ToBase64String(key);
		var ivB64 = Convert.ToBase64String(iv);
		var ctB64 = await javaScript.InvokeAsync<string>("cryptoInterop.encrypt", ptB64, keyB64, ivB64);
		return Convert.FromBase64String(ctB64);
	}
}
