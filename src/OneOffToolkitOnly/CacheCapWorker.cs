using FatCat.Toolkit.Caching;
using FatCat.Toolkit.Console;

namespace OneOffToolkitOnly;

public class CacheCapWorker(
	IFatCatCache<CappedSampleItem> cappedCache,
	IFatCatCache<UncappedSampleItem> uncappedCache,
	FatCatCacheOptions<CappedSampleItem> cappedOptions
)
{
	private const int DistinctItemCount = 10_000;

	public void DoWork()
	{
		AddDistinctItems();

		ConsoleLog.WriteCyan($"Added {DistinctItemCount} distinct items to each cache");
		ConsoleLog.WriteGreen(
			$"Capped cache (registered MaximumItems = {cappedOptions.MaximumItems}) count := <{cappedCache.GetAll().Count}>"
		);
		ConsoleLog.WriteGreen($"Uncapped cache count := <{uncappedCache.GetAll().Count}>");
	}

	private void AddDistinctItems()
	{
		foreach (var itemNumber in Enumerable.Range(0, DistinctItemCount))
		{
			cappedCache.Add(new CappedSampleItem { CacheId = $"capped-{itemNumber}" });
			uncappedCache.Add(new UncappedSampleItem { CacheId = $"uncapped-{itemNumber}" });
		}
	}
}
