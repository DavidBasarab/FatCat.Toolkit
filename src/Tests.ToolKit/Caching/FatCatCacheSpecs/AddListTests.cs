using Humanizer;

namespace Tests.FatCat.Toolkit.Caching.FatCatCacheSpecs;

public class AddListTests : FatCatCacheTests
{
	private readonly List<TestCacheItem> items = CreateItems();

	[Fact]
	public void ExpireEveryItemAfterTheTimeout()
	{
		Cache.Add(items, 5.Minutes());

		AdvanceClock(6.Minutes());

		items.Should().OnlyContain(item => !Cache.InCache(item.CacheId));
	}

	[Fact]
	public void KeepEveryItemBeforeTheTimeout()
	{
		Cache.Add(items, 5.Minutes());

		AdvanceClock(4.Minutes());

		items.Should().OnlyContain(item => Cache.InCache(item.CacheId));
	}

	[Fact]
	public void StoreEveryItem()
	{
		Cache.Add(items);

		items.Should().OnlyContain(item => Cache.Get(item.CacheId) == item);
	}
}
