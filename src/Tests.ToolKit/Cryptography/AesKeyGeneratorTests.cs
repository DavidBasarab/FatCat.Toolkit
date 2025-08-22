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
		A.CallTo(() => generator.Bytes(A<int>._)).Returns(createdBytes);

		keyGenerator = new AesKeyGenerator(generator);
	}

	[Fact]
	public void ReturnCreatedKey()
	{
		var key = keyGenerator.CreateKey(AesKeySize.Aes128);

		key.Should().BeEquivalentTo(createdBytes);
	}

	[Fact]
	public void WillCreateBytesForIv()
	{
		keyGenerator.CreateIv();

		A.CallTo(() => generator.Bytes(12)).MustHaveHappened();
	}

	[Theory]
	[InlineData(AesKeySize.Aes128, 16)]
	[InlineData(AesKeySize.Aes192, 24)]
	[InlineData(AesKeySize.Aes256, 32)]
	public void WillCreateValidKeySizes(AesKeySize keySize, int expectedSize)
	{
		keyGenerator.CreateKey(keySize);

		A.CallTo(() => generator.Bytes(expectedSize)).MustHaveHappened();
	}

	[Fact]
	public void WillReturnBytesForIv()
	{
		var iv = keyGenerator.CreateIv();

		iv.Should().BeEquivalentTo(createdBytes);
	}
}
