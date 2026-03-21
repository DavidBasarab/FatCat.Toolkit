using FatCat.Toolkit;

namespace Tests.FatCat.Toolkit;

public class ByteToolTests
{
	[Fact]
	public void CanConvertToByteArrayAndFrom()
	{
		var byteTools = new ByteTools();

		var bytes = Faker.RandomBytes(124);

		var base64String = byteTools.ToBase64String(bytes);

		var decodedBytes = byteTools.FromBase64String(base64String);

		decodedBytes.Should().BeEquivalentTo(bytes);
	}

	[Fact]
	public void CanConvertToByteArrayAndFromEncoded()
	{
		var byteTools = new ByteTools();

		var textToChange = Faker.RandomString();

		var bytes = byteTools.FromBase64Encoded(textToChange);

		var plainText = byteTools.ToBase64Encoded(bytes);

		plainText.Should().Be(textToChange);
	}
}
