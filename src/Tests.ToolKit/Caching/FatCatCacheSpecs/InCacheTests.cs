using FatCat.Toolkit.Caching;
using Humanizer;

namespace Tests.FatCat.Toolkit.Caching.FatCatCacheSpecs;

public class InCacheTests : FatCatCacheTests
{
	private readonly TestCacheItem item;

	public InCacheTests()
	{
		DelayTheSweepPastEveryScenario();

		item = AddItem(5.Minutes());
	}

	[Fact]
	public void RemoveTheExpiredEntry()
	{
		AdvanceClock(6.Minutes());

		Cache.InCache(item.CacheId);

		Cache.StoredCount.Should().Be(0);
	}

	[Fact]
	public void ReturnFalseForAnUnknownId()
	{
		Cache.InCache(Faker.RandomString()).Should().BeFalse();
	}

	[Fact]
	public void ReturnFalseOnceExpired()
	{
		AdvanceClock(6.Minutes());

		Cache.InCache(item.CacheId).Should().BeFalse();
	}

	[Fact]
	public void ReturnTrueAtExactlyTheTimeout()
	{
		AdvanceClock(5.Minutes());

		Cache.InCache(item.CacheId).Should().BeTrue();
	}

	[Fact]
	public void ReturnTrueForTheItemOverload()
	{
		IFatCatCache<TestCacheItem> cache = Cache;

		cache.InCache(item).Should().BeTrue();
	}

	[Fact]
	public void ReturnTrueWhenPresent()
	{
		Cache.InCache(item.CacheId).Should().BeTrue();
	}
}
