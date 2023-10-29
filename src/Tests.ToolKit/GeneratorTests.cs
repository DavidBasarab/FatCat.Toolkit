﻿using System.Diagnostics;
using FatCat.Toolkit;
using FatCat.Toolkit.Utilities;
using FluentAssertions;
using Humanizer;
using Xunit;

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
	public void CanGenerateALargeByteArrayQuickly()
	{
		var testSizeInMb = 113;

		var sizeInBytes = testSizeInMb * ByteUtilities.BytesInMegaBytes;

		var timer = Stopwatch.StartNew();

		var bytes = generator.Bytes(sizeInBytes);

		timer.Stop();

		bytes.LongCount().Should().Be(sizeInBytes);

		timer.Elapsed.Should().BeLessOrEqualTo(200.Milliseconds());
	}
}
