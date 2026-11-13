using Humanizer;

namespace Tests.FatCat.Toolkit.Caching.FatCatCacheSpecs;

public class SweepTests : FatCatCacheTests
{
	[Fact]
	public void KeepTheThrottleWhenACallReadTheClockBeforeTheLastSweep()
	{
		AddItem();

		MakeACallThatReadTheClockBeforeTheLastSweep();

		AddItem(5.Seconds());

		AdvanceClock(options.SweepInterval);

		Cache.Get(Faker.RandomString());

		Cache.StoredCount.Should().Be(2);
	}

	[Fact]
	public void LeaveOtherExpiredEntriesStoredBeforeTheSweepInterval()
	{
		AddItem(5.Seconds());

		AdvanceClock(10.Seconds());

		Cache.Get(Faker.RandomString());

		Cache.StoredCount.Should().Be(1);
	}

	[Fact]
	public void RemoveEveryExpiredEntryOnceTheSweepIntervalHasPassed()
	{
		ExpireEntriesAndPassTheSweepInterval();

		Cache.Get(Faker.RandomString());

		Cache.StoredCount.Should().Be(1);
	}

	[Fact]
	public void SweepOnceExactlyTheSweepIntervalHasPassed()
	{
		AddItem();
		AddItem(5.Seconds());

		AdvanceClock(options.SweepInterval);

		Cache.Get(Faker.RandomString());

		Cache.StoredCount.Should().Be(1);
	}

	[Fact]
	public void SweepWhenAnItemIsAdded()
	{
		ExpireEntriesAndPassTheSweepInterval();

		AddItem();

		Cache.StoredCount.Should().Be(2);
	}

	[Fact]
	public void SweepWhenAllItemsAreRead()
	{
		ExpireEntriesAndPassTheSweepInterval();

		Cache.GetAll();

		Cache.StoredCount.Should().Be(1);
	}

	[Fact]
	public void SweepWhenAnIdIsCheckedInTheCache()
	{
		ExpireEntriesAndPassTheSweepInterval();

		Cache.InCache(Faker.RandomString());

		Cache.StoredCount.Should().Be(1);
	}

	[Fact]
	public void SweepWhenAnIdIsRemoved()
	{
		ExpireEntriesAndPassTheSweepInterval();

		Cache.Remove(Faker.RandomString());

		Cache.StoredCount.Should().Be(1);
	}

	[Fact]
	public void SweepAgainOnceTheNextIntervalHasPassed()
	{
		ExpireAnEntryJustAfterASweep();

		PassTheSweepInterval();

		Cache.Get(Faker.RandomString());

		Cache.StoredCount.Should().Be(1);
	}

	[Fact]
	public void SweepAtMostOncePerInterval()
	{
		ExpireAnEntryJustAfterASweep();

		Cache.Get(Faker.RandomString());

		Cache.StoredCount.Should().Be(2);
	}

	private void ExpireEntriesAndPassTheSweepInterval()
	{
		AddItem();
		AddItem(5.Seconds());
		AddItem(5.Seconds());

		PassTheSweepInterval();
	}

	private void MakeACallThatReadTheClockBeforeTheLastSweep()
	{
		AdvanceClock(-10.Seconds());

		Cache.Get(Faker.RandomString());
	}

	private void ExpireAnEntryJustAfterASweep()
	{
		AddItem();

		PassTheSweepInterval();

		Cache.Get(Faker.RandomString());

		AddItem(5.Seconds());

		AdvanceClock(10.Seconds());
	}
}
