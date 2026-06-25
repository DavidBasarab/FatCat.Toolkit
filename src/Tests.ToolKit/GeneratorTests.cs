using System.Diagnostics;
using FatCat.Toolkit;
using FatCat.Toolkit.Utilities;
using Humanizer;

namespace Tests.FatCat.Toolkit;

public class GeneratorTests
{
	private readonly Generator generator = new();

	[Fact]
	public void ByteArrayLengthIsCorrect()
	{
		var bytes = generator.Bytes(1024);

		bytes.LongCount().Should().Be(1024);
	}

	[Fact]
	public void CsprngBytesLengthIsCorrect()
	{
		var bytes = generator.CsprngBytes(32);

		bytes.Should().HaveCount(32);
	}

	[Fact]
	public void CsprngBytesProducesDistinctValues()
	{
		var results = Enumerable.Range(0, 100).Select(_ => Convert.ToBase64String(generator.CsprngBytes(32)));

		results.Should().OnlyHaveUniqueItems();
	}

	[Fact]
	public void CanGenerateALargeByteArrayQuickly()
	{
		var testSizeInMb = 113;

		var sizeInBytes = testSizeInMb * ByteUtilities.BytesInMegaBytes;

		var timer = Stopwatch.StartNew();

		var bytes = generator.Bytes(sizeInBytes);

		timer.Stop();

		bytes.LongCount().Should().Be(sizeInBytes);

		timer.Elapsed.Should().BeLessThanOrEqualTo(400.Milliseconds());
	}
}
