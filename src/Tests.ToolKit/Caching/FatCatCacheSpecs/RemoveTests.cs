using FatCat.Toolkit.Caching;

namespace Tests.FatCat.Toolkit.Caching.FatCatCacheSpecs;

public class RemoveTests : FatCatCacheTests
{
	private readonly TestCacheItem item;

	public RemoveTests()
	{
		item = AddItem();
	}

	[Fact]
	public void IgnoreAnUnknownId()
	{
		Cache.Remove(Faker.RandomString());

		Cache.InCache(item.CacheId).Should().BeTrue();
	}

	[Fact]
	public void RemoveTheItem()
	{
		Cache.Remove(item.CacheId);

		Cache.InCache(item.CacheId).Should().BeFalse();
	}

	[Fact]
	public void RemoveThroughTheItemOverload()
	{
		IFatCatCache<TestCacheItem> cache = Cache;

		cache.Remove(item);

		Cache.InCache(item.CacheId).Should().BeFalse();
	}
}
