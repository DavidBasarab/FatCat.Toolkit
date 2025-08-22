using FatCat.Toolkit;
using FatCat.Toolkit.Cryptography;

namespace Tests.FatCat.Toolkit.Cryptography;

public class FatCatAesEncryptionTests
{
	private readonly IFatCatAesEncryption encryption = new FatCatAesEncryption();
	private readonly AesKeyGenerator keyGenerator = new(new Generator());
	private readonly byte[] openData = Faker.RandomBytes(1024 * 8);

	[Fact]
	public async Task CanDecryptAsgmGeneratedData()
	{
		var key = new byte[]
		{
			0x7B,
			0x2F,
			0x7F,
			0x9F,
			0x2C,
			0x90,
			0x94,
			0xFA,
			0xEE,
			0x7A,
			0xC2,
			0x21,
			0xC2,
			0x07,
			0xB3,
			0x2F,
			0x1A,
			0x7B,
			0xA6,
			0xB4,
			0xE2,
			0xD2,
			0xDB,
			0x2F,
			0x0F,
			0xAA,
			0x24,
			0x46,
			0x91,
			0x84,
			0xB5,
			0xC2,
		};

		var iv = new byte[] { 0x9C, 0xCA, 0xAC, 0xDC, 0x3C, 0xA4, 0x47, 0x15, 0x03, 0x9D, 0x24, 0x7F };

		var openData = new byte[] { 0xBF, 0xAE, 0x4F, 0x2E };

		var encryptedData = new byte[]
		{
			0xDA,
			0xC1,
			0x2A,
			0x9E,
			0xBB,
			0xA7,
			0x06,
			0xC8,
			0xF6,
			0xC9,
			0x8A,
			0x26,
			0x79,
			0x29,
			0x6D,
			0xB9,
			0x87,
			0x8A,
			0x89,
			0xC9,
		};

		var decryptedData = await encryption.Decrypt(encryptedData, key, iv);

		decryptedData.Should().BeEquivalentTo(openData);
	}

	[Theory]
	[InlineData(AesKeySize.Aes128)]
	[InlineData(AesKeySize.Aes192)]
	[InlineData(AesKeySize.Aes256)]
	public async Task CanDecryptDataBackToTheSameOpen(AesKeySize keySize)
	{
		var key = keyGenerator.CreateKey(keySize);
		var iv = keyGenerator.CreateIv();

		var encryptedData = await encryption.Encrypt(openData, key, iv);

		var decryptedData = await encryption.Decrypt(encryptedData, key, iv);

		decryptedData.Should().BeEquivalentTo(openData);
	}

	[Theory]
	[InlineData(AesKeySize.Aes128)]
	[InlineData(AesKeySize.Aes192)]
	[InlineData(AesKeySize.Aes256)]
	public async Task CanEncryptAByteArray(AesKeySize keySize)
	{
		var key = keyGenerator.CreateKey(keySize);
		var iv = keyGenerator.CreateIv();

		var encryptedData = await encryption.Encrypt(openData, key, iv);

		encryptedData.Should().NotBeEquivalentTo(openData);
	}
}
