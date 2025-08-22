using FatCat.Fakes;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.Cryptography;
using FatCat.Toolkit.Extensions;
using Microsoft.AspNetCore.Components;

namespace OneOffBlazor.Pages;

public partial class DavidTest(
	ITestingService testingService,
	IFatCatAesEncryption aesEncryption,
	IAesKeyGenerator aesKeyGenerator
) : ComponentBase
{
	public async Task DoSomeWork()
	{
		try
		{
			// Use the testing service to verify DI and remove unused parameter warning
			testingService.PrintAMessage();

			var key = aesKeyGenerator.CreateKey(AesKeySize.Aes256);
			var iv = aesKeyGenerator.CreateIv();

			ConsoleLog.Write($"Key := <{key.ToReadableString()}> ");
			ConsoleLog.Write($"IV := <{iv.ToReadableString()}> ");

			var openData = Faker.RandomBytes(4);

			ConsoleLog.Write($"OpenData := <{openData.ToReadableString()}> ");

			var encryptedData = await aesEncryption.Encrypt(openData, key, iv);

			ConsoleLog.Write($"EncryptedData := <{encryptedData.ToReadableString()}> ");

			var decryptedData = await aesEncryption.Decrypt(encryptedData, key, iv);

			ConsoleLog.Write($"DecryptedData := <{decryptedData.ToReadableString()}> ");
		}
		catch (Exception ex)
		{
			ConsoleLog.Write(ex.Message);
			ConsoleLog.Write(ex.StackTrace);
		}
	}
}
