using FatCat.Toolkit;
using FatCat.Toolkit.Caching;
using Humanizer;

namespace Tests.FatCat.Toolkit.Caching.FatCatCacheSpecs;

public abstract class FatCatCacheTests
{
	protected readonly IDateTimeUtilities dateTimeUtilities = A.Fake<IDateTimeUtilities>();
	protected readonly FatCatCacheOptions<TestCacheItem> options = new();

	protected DateTime now = Faker.Create<DateTime>();

	protected FatCatCache<TestCacheItem> Cache
	{
		get { return field ??= new FatCatCache<TestCacheItem>(dateTimeUtilities, options); }
	}

	protected FatCatCacheTests()
	{
		A.CallTo(() => dateTimeUtilities.UtcNow()).ReturnsLazily(() => now);
	}

	protected TestCacheItem AddItem(TimeSpan? timeout = null)
	{
		var item = Faker.Create<TestCacheItem>();

		Cache.Add(item, timeout);

		return item;
	}

	protected TestCacheItem AddItemAfterASecond(TimeSpan? timeout = null)
	{
		AdvanceClock(1.Seconds());

		return AddItem(timeout);
	}

	protected List<TestCacheItem> AddItems(int count, TimeSpan? timeout = null)
	{
		return Enumerable.Range(0, count).Select(_ => AddItem(timeout)).ToList();
	}

	protected List<TestCacheItem> AddItemsOneSecondApart(int count, TimeSpan? timeout = null)
	{
		return Enumerable.Range(0, count).Select(_ => AddItemAfterASecond(timeout)).ToList();
	}

	protected void AdvanceClock(TimeSpan amount)
	{
		now += amount;
	}

	protected static List<TestCacheItem> CreateItems()
	{
		return Enumerable.Range(0, Faker.RandomInt(3, 10)).Select(_ => Faker.Create<TestCacheItem>()).ToList();
	}

	protected void BuildTheCache()
	{
		_ = Cache;
	}

	protected void DelayTheSweepPastEveryScenario()
	{
		options.SweepInterval = 1.Days();
	}

	protected List<TestCacheItem> FillToCapacity()
	{
		return AddItems(options.MaximumItems.Value);
	}

	protected List<TestCacheItem> FillToCapacityOneSecondApart()
	{
		return AddItemsOneSecondApart(options.MaximumItems.Value);
	}

	protected void PassTheSweepInterval()
	{
		AdvanceClock(options.SweepInterval + 1.Seconds());
	}
}
