using FatCat.Toolkit.Caching;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.Threading;
using Humanizer;
using Newtonsoft.Json;

namespace OneOff.Old;

public class CacheWorker
{
	private readonly IFatCatCache<TestCacheItem> cache;
	private readonly IThread thread;

	public CacheWorker(IFatCatCache<TestCacheItem> cache, IThread thread)
	{
		this.cache = cache;
		this.thread = thread;
	}

	public void DoWork()
	{
		var firstItem = new TestCacheItem { Name = "First Item", Number = 1 };

		cache.Add(firstItem, 250.Milliseconds());

		var secondItem = new TestCacheItem { Name = "Second Item", Number = 2 };

		cache.Add(secondItem);

		var thirdItem = new TestCacheItem { Name = "Third Item", Number = 3 };

		cache.Add(thirdItem, 1.Seconds());

		var allItems = cache.GetAll();

		ConsoleLog.WriteCyan($"{JsonConvert.SerializeObject(allItems, Formatting.Indented)}");

		thread.Sleep(500.Milliseconds()).Wait();

		var currentItems = cache.GetAll();

		ConsoleLog.WriteMagenta($"{JsonConvert.SerializeObject(currentItems, Formatting.Indented)}");

		thread.Sleep(2.Seconds()).Wait();

		var lastItems = cache.GetAll();

		ConsoleLog.WriteBlue($"{JsonConvert.SerializeObject(lastItems, Formatting.Indented)}");
	}
}

public class TestCacheItem : ICacheItem
{
	public string CacheId
	{
		get => Name;
	}

	public string Name { get; set; }

	public int Number { get; set; }
}
