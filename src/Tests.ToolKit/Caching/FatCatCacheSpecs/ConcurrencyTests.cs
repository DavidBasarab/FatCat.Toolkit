using System.Collections.Concurrent;

namespace Tests.FatCat.Toolkit.Caching.FatCatCacheSpecs;

// Owner-approved real-thread exception: ADR-6 of src/task/todo/fatcat-cache-cap/00-overview.md
public class ConcurrencyTests : FatCatCacheTests
{
	private const int ItemsPerWorker = 1_000;
	private const int MaximumItems = 100;
	private const int WorkerCount = 8;

	private readonly ConcurrentBag<Exception> exceptions = [];
	private readonly ConcurrentBag<int> observedStoredCounts = [];
	private readonly List<List<(TestCacheItem Item, string IdToRead)>> workPerWorker;

	public ConcurrencyTests()
	{
		options.MaximumItems = MaximumItems;

		workPerWorker = PlanTheWork();

		BuildTheCache();

		Parallel.For(0, WorkerCount, new ParallelOptions { MaxDegreeOfParallelism = WorkerCount }, RunWorker);

		observedStoredCounts.Add(Cache.StoredCount);
	}

	[Fact]
	public void NeverThrow()
	{
		exceptions.ToList().Should().BeEmpty();
	}

	[Fact]
	public void StayWithinTheCapPlusConcurrentWriters()
	{
		observedStoredCounts.Max().Should().BeLessThanOrEqualTo(MaximumItems + WorkerCount);
	}

	private static List<List<(TestCacheItem Item, string IdToRead)>> PlanTheWork()
	{
		var itemsPerWorker = Enumerable
			.Range(0, WorkerCount)
			.Select(_ => Enumerable.Range(0, ItemsPerWorker).Select(_ => Faker.Create<TestCacheItem>()).ToList())
			.ToList();

		var allIds = itemsPerWorker.SelectMany(items => items).Select(item => item.CacheId).ToList();

		return itemsPerWorker
			.Select(items => items.Select(item => (item, allIds[Faker.RandomInt(0, allIds.Count - 1)])).ToList())
			.ToList();
	}

	private void RunWorker(int worker)
	{
		try
		{
			foreach (var (item, idToRead) in workPerWorker[worker])
			{
				Cache.Add(item);
				observedStoredCounts.Add(Cache.StoredCount);
				Cache.Get(idToRead);
				Cache.InCache(idToRead);
			}
		}
		catch (Exception exception)
		{
			exceptions.Add(exception);
		}
	}
}
