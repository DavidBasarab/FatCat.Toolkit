using Humanizer;

namespace Tests.FatCat.Toolkit.Caching.FatCatCacheSpecs;

public class GetTests : FatCatCacheTests
{
	private readonly TestCacheItem item;

	public GetTests()
	{
		DelayTheSweepPastEveryScenario();

		item = AddItem(5.Minutes());
	}

	[Fact]
	public void RemoveTheExpiredEntry()
	{
		AdvanceClock(6.Minutes());

		Cache.Get(item.CacheId);

		Cache.StoredCount.Should().Be(0);
	}

	[Fact]
	public void ReturnNullForAnUnknownId()
	{
		Cache.Get(Faker.RandomString()).Should().BeNull();
	}

	[Fact]
	public void ReturnNullOnceExpired()
	{
		AdvanceClock(6.Minutes());

		Cache.Get(item.CacheId).Should().BeNull();
	}

	[Fact]
	public void ReturnTheItem()
	{
		Cache.Get(item.CacheId).Should().Be(item);
	}

	[Fact]
	public void ReturnTheItemAtExactlyTheTimeout()
	{
		AdvanceClock(5.Minutes());

		Cache.Get(item.CacheId).Should().Be(item);
	}
}
