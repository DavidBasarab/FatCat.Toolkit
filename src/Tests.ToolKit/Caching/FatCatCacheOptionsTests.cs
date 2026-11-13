using FatCat.Toolkit.Caching;
using Humanizer;

namespace Tests.FatCat.Toolkit.Caching;

public class FatCatCacheOptionsTests
{
	[Fact]
	public void DefaultTheSweepIntervalToThirtySeconds()
	{
		new FatCatCacheOptions<TestCacheItem>().SweepInterval.Should().Be(30.Seconds());
	}

	[Fact]
	public void DefaultToUnboundedOptions()
	{
		new FatCatCacheOptions<TestCacheItem>().MaximumItems.Should().BeNull();
	}
}
