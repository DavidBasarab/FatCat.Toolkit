using Humanizer;

namespace Tests.FatCat.Toolkit.Caching.FatCatCacheSpecs;

public class GetAllTests : FatCatCacheTests
{
	[Fact]
	public void ExcludeExpiredItemsBeforeTheSweepIsDue()
	{
		DelayTheSweepPastEveryScenario();

		var permanentItem = AddItem();

		AddItem(5.Minutes());

		AdvanceClock(6.Minutes());

		Cache.GetAll().Should().BeEquivalentTo([permanentItem]);
	}

	[Fact]
	public void ReturnEveryItem()
	{
		var items = CreateItems();

		Cache.Add(items);

		Cache.GetAll().Should().BeEquivalentTo(items);
	}
}
