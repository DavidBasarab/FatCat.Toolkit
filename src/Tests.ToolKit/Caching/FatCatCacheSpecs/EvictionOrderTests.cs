using Humanizer;

namespace Tests.FatCat.Toolkit.Caching.FatCatCacheSpecs;

public class EvictionOrderTests : FatCatCacheTests
{
	public EvictionOrderTests()
	{
		options.MaximumItems = 10;
	}

	[Fact]
	public void EvictTheOldestEntryFirst()
	{
		var items = FillToCapacityOneSecondApart();

		AddItemAfterASecond();

		Cache.InCache(items.First().CacheId).Should().BeFalse();
	}

	[Fact]
	public void EvictUnexpiredEntriesOnlyWhenExpiryFreesNoRoom()
	{
		var items = AddItemsOneSecondApart(10, 10.Minutes());

		AddItemAfterASecond();

		Cache.InCache(items.First().CacheId).Should().BeFalse();
	}

	[Fact]
	public void KeepEveryUnexpiredEntryWhenExpiryFreesRoom()
	{
		var permanentItems = FillWithPermanentItemsBeforeExpiredOnes();

		var newItem = AddItem();

		Cache.GetAll().Should().BeEquivalentTo([.. permanentItems, newItem]);
	}

	[Fact]
	public void KeepTheMostRecentlyUpdatedEntry()
	{
		var items = FillToCapacityOneSecondApart();
		var secondOldestItem = items[1];

		AdvanceClock(1.Seconds());

		Cache.Add(items[0]);

		var newItem = AddItemAfterASecond();

		Cache.GetAll().Should().BeEquivalentTo([.. items.Where(item => item != secondOldestItem), newItem]);
	}

	[Fact]
	public void RemoveExpiredEntriesBeforeEvicting()
	{
		var permanentItems = FillWithPermanentItemsBeforeExpiredOnes();

		AddItem();

		Cache.StoredCount.Should().Be(permanentItems.Count + 1);
	}

	private List<TestCacheItem> FillWithPermanentItemsBeforeExpiredOnes()
	{
		DelayTheSweepPastEveryScenario();

		var permanentItems = AddItemsOneSecondApart(5);

		AddItemsOneSecondApart(5, 1.Minutes());

		AdvanceClock(2.Minutes());

		return permanentItems;
	}
}
