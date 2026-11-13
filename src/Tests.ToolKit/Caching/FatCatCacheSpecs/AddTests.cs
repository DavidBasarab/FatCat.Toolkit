using Humanizer;

namespace Tests.FatCat.Toolkit.Caching.FatCatCacheSpecs;

public class AddTests : FatCatCacheTests
{
	[Fact]
	public void KeepAnItemWithoutATimeoutIndefinitely()
	{
		var item = AddItem();

		AdvanceClock(3650.Days());

		Cache.Get(item.CacheId).Should().Be(item);
	}

	[Fact]
	public void ReplaceTheItemWithTheSameCacheId()
	{
		var original = AddItem();
		var replacement = new TestCacheItem { SomeId = original.SomeId };

		Cache.Add(replacement);

		ReferenceEquals(Cache.Get(original.CacheId), replacement).Should().BeTrue();
	}

	[Fact]
	public void RestartTheTimeoutWhenTheItemIsReplaced()
	{
		var item = AddItem(5.Minutes());

		AdvanceClock(4.Minutes());

		Cache.Add(item, 5.Minutes());

		AdvanceClock(4.Minutes());

		Cache.Get(item.CacheId).Should().Be(item);
	}

	[Fact]
	public void RetainEveryItemWhenUnbounded()
	{
		var items = Enumerable.Range(0, 5_000).Select(_ => AddItem()).ToList();

		items.Should().OnlyContain(item => Cache.InCache(item.CacheId));
	}

	[Fact]
	public void StoreTheItem()
	{
		var item = AddItem();

		Cache.Get(item.CacheId).Should().Be(item);
	}
}
