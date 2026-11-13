using Humanizer;

namespace Tests.FatCat.Toolkit.Caching.FatCatCacheSpecs;

public class CapacityTests : FatCatCacheTests
{
	[Fact]
	public void FreeAtLeastOneSlotForASmallCap()
	{
		options.MaximumItems = 1;

		AddItem();

		var secondItem = AddItem();

		Cache.GetAll().Should().BeEquivalentTo([secondItem]);
	}

	[Fact]
	public void KeepTheNewestItem()
	{
		options.MaximumItems = 50;

		var items = AddItems(500);

		Cache.InCache(items.Last().CacheId).Should().BeTrue();
	}

	[Fact]
	public void KeepTheOldestEntryWhenUpdatingAnExistingKey()
	{
		options.MaximumItems = 10;

		var items = FillToCapacityOneSecondApart();

		UpdateAfterASecond(items.Last());

		Cache.InCache(items.First().CacheId).Should().BeTrue();
	}

	[Fact]
	public void NeverExceedTheCapWhenAddingOneAtATime()
	{
		options.MaximumItems = 50;

		var largestStoredCount = Enumerable.Range(0, 500).Max(_ => AddItemAndCountTheCache());

		largestStoredCount.Should().BeLessThanOrEqualTo(50);
	}

	[Fact]
	public void NotEvictWhenUpdatingAnExistingKey()
	{
		options.MaximumItems = 10;

		var items = FillToCapacityOneSecondApart();

		UpdateAfterASecond(items.Last());

		Cache.StoredCount.Should().Be(10);
	}

	[Fact]
	public void RestartTheSweepIntervalWhenMakingRoom()
	{
		options.MaximumItems = 10;

		AddItems(9);

		AddItemAfterASecond(25.Seconds());

		AdvanceClock(20.Seconds());

		AddItem();

		AdvanceClock(20.Seconds());

		Cache.Get(Faker.RandomString());

		Cache.StoredCount.Should().Be(10);
	}

	[Fact]
	public void RetainEveryItemWhenUnbounded()
	{
		var items = AddItems(500);

		items.Should().OnlyContain(item => Cache.InCache(item.CacheId));
	}

	[Fact]
	public void TrimToTheLowWaterMark()
	{
		options.MaximumItems = 100;

		FillToCapacity();

		AddItem();

		Cache.StoredCount.Should().Be(100 - 10 + 1);
	}

	private int AddItemAndCountTheCache()
	{
		AddItem();

		return Cache.StoredCount;
	}

	private void UpdateAfterASecond(TestCacheItem item)
	{
		AdvanceClock(1.Seconds());

		Cache.Add(item);
	}
}
