using FatCat.Toolkit.Cryptography;
using Microsoft.JSInterop;

namespace OneOffBlazor;

public class WebCryptoAesGcm(IJSRuntime javaScript) : IFatCatAesEncryption
{
	public async Task<byte[]> Decrypt(byte[] cypherData, byte[] key, byte[] iv)
	{
		var cypherString = Convert.ToBase64String(cypherData);
		var keyString = Convert.ToBase64String(key);
		var ivString = Convert.ToBase64String(iv);

		var openData = await javaScript.InvokeAsync<string>(
			"cryptoInterop.decrypt",
			cypherString,
			keyString,
			ivString
		);

		return Convert.FromBase64String(openData);
	}

	public async Task<byte[]> Encrypt(byte[] openData, byte[] key, byte[] iv)
	{
		var openString = Convert.ToBase64String(openData);
		var keyString = Convert.ToBase64String(key);
		var ivString = Convert.ToBase64String(iv);

		var encryptedData = await javaScript.InvokeAsync<string>(
			"cryptoInterop.encrypt",
			openString,
			keyString,
			ivString
		);

		return Convert.FromBase64String(encryptedData);
	}
}
