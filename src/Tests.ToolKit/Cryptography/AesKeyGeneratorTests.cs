using FatCat.Toolkit;
using FatCat.Toolkit.Cryptography;

namespace Tests.FatCat.Toolkit.Cryptography;

public class AesKeyGeneratorTests
{
	private readonly byte[] createdBytes = Faker.RandomBytes(3);
	private readonly IGenerator generator = A.Fake<IGenerator>();
	private readonly AesKeyGenerator keyGenerator;

	public AesKeyGeneratorTests()
	{
		A.CallTo(() => generator.CsprngBytes(A<int>._)).Returns(createdBytes);

		keyGenerator = new AesKeyGenerator(generator);
	}

	[Fact]
	public void ReturnCreatedKey()
	{
		var key = keyGenerator.CreateKey(AesKeySize.Aes128);

		key.Should().BeEquivalentTo(createdBytes);
	}

	[Fact]
	public void WillCreateCryptographicBytesForIv()
	{
		keyGenerator.CreateIv();

		A.CallTo(() => generator.CsprngBytes(12)).MustHaveHappened();
	}

	[Theory]
	[InlineData(AesKeySize.Aes128, 16)]
	[InlineData(AesKeySize.Aes192, 24)]
	[InlineData(AesKeySize.Aes256, 32)]
	public void WillCreateValidKeySizes(AesKeySize keySize, int expectedSize)
	{
		keyGenerator.CreateKey(keySize);

		A.CallTo(() => generator.CsprngBytes(expectedSize)).MustHaveHappened();
	}

	[Fact]
	public void WillReturnBytesForIv()
	{
		var iv = keyGenerator.CreateIv();

		iv.Should().BeEquivalentTo(createdBytes);
	}
}
