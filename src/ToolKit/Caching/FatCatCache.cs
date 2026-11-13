#nullable enable
using System.Collections.Concurrent;

namespace FatCat.Toolkit.Caching;

public interface IFatCatCache<T>
	where T : class, ICacheItem
{
	public void Add(T cacheItem, TimeSpan? timeout = null);

	public void Add(List<T> cacheItems, TimeSpan? timeout = null);

	public void Clear();

	public T? Get(string cacheId);

	public IList<T> GetAll();

	public bool InCache(string cacheId);

	public bool InCache(T cacheItem)
	{
		return InCache(cacheItem.CacheId);
	}

	public void Remove(string cacheId);

	public void Remove(T cacheItem)
	{
		Remove(cacheItem.CacheId);
	}
}

public class FatCatCache<T>(IDateTimeUtilities dateTimeUtilities, FatCatCacheOptions<T> options) : IFatCatCache<T>
	where T : class, ICacheItem
{
	private readonly ConcurrentDictionary<string, CacheEntry> cache = new();

	private readonly IDateTimeUtilities dateTimeUtilities = dateTimeUtilities
		?? throw new ArgumentNullException(nameof(dateTimeUtilities));

	private readonly int? maximumItems = RequirePositiveMaximumItems(RequireOptions(options).MaximumItems);
	private readonly TimeSpan sweepInterval = RequirePositiveSweepInterval(RequireOptions(options).SweepInterval);

	private long lastSweepTicks;

	public FatCatCache(IDateTimeUtilities dateTimeUtilities)
		: this(dateTimeUtilities, new FatCatCacheOptions<T>()) { }

	// Kept so consumers can still write new FatCatCache<T>(); containers that register IDateTimeUtilities never select it
	public FatCatCache()
		: this(new DateTimeUtilities(), new FatCatCacheOptions<T>()) { }

	internal int StoredCount
	{
		get { return cache.Count; }
	}

	public void Add(T cacheItem, TimeSpan? timeout = null)
	{
		var now = dateTimeUtilities.UtcNow();

		SweepIfDue(now);

		cache[cacheItem.CacheId] = new CacheEntry(cacheItem, timeout, now);
	}

	public void Add(List<T> cacheItems, TimeSpan? timeout = null)
	{
		foreach (var item in cacheItems)
		{
			Add(item, timeout);
		}
	}

	public void Clear()
	{
		cache.Clear();
	}

	public T? Get(string cacheId)
	{
		var now = dateTimeUtilities.UtcNow();

		SweepIfDue(now);

		return FindLiveEntry(cacheId, now)?.Item;
	}

	public IList<T> GetAll()
	{
		var now = dateTimeUtilities.UtcNow();

		SweepIfDue(now);

		return cache.Where(pair => !pair.Value.HasExpired(now)).Select(pair => pair.Value.Item).ToList();
	}

	public bool InCache(string cacheId)
	{
		var now = dateTimeUtilities.UtcNow();

		SweepIfDue(now);

		return FindLiveEntry(cacheId, now) != null;
	}

	public void Remove(string cacheId)
	{
		SweepIfDue(dateTimeUtilities.UtcNow());

		cache.TryRemove(cacheId, out _);
	}

	private static FatCatCacheOptions<T> RequireOptions(FatCatCacheOptions<T> options)
	{
		if (options == null)
		{
			throw new ArgumentNullException(nameof(options));
		}

		return options;
	}

	private static int? RequirePositiveMaximumItems(int? maximumItems)
	{
		if (maximumItems <= 0)
		{
			throw new ArgumentOutOfRangeException(
				nameof(FatCatCacheOptions<T>.MaximumItems),
				maximumItems,
				"FatCatCacheOptions.MaximumItems must be greater than zero when set"
			);
		}

		return maximumItems;
	}

	private static TimeSpan RequirePositiveSweepInterval(TimeSpan sweepInterval)
	{
		if (sweepInterval <= TimeSpan.Zero)
		{
			throw new ArgumentOutOfRangeException(
				nameof(FatCatCacheOptions<T>.SweepInterval),
				sweepInterval,
				"FatCatCacheOptions.SweepInterval must be greater than zero"
			);
		}

		return sweepInterval;
	}

	private CacheEntry? FindLiveEntry(string cacheId, DateTime now)
	{
		if (!cache.TryGetValue(cacheId, out var entry))
		{
			return null;
		}

		if (!entry.HasExpired(now))
		{
			return entry;
		}

		cache.TryRemove(new KeyValuePair<string, CacheEntry>(cacheId, entry));

		return null;
	}

	private void RemoveExpired(DateTime now)
	{
		foreach (var pair in cache.Where(pair => pair.Value.HasExpired(now)))
		{
			cache.TryRemove(pair);
		}
	}

	private void SweepIfDue(DateTime now)
	{
		var lastSweep = Interlocked.Read(ref lastSweepTicks);

		if (now.Ticks - lastSweep < sweepInterval.Ticks)
		{
			return;
		}

		if (Interlocked.CompareExchange(ref lastSweepTicks, now.Ticks, lastSweep) != lastSweep)
		{
			return;
		}

		RemoveExpired(now);
	}

	private class CacheEntry(T item, TimeSpan? timeout, DateTime entryTime)
	{
		public DateTime EntryTime { get; } = entryTime;

		public T Item { get; } = item;

		public TimeSpan? Timeout { get; } = timeout;

		public bool HasExpired(DateTime now)
		{
			if (Timeout == null)
			{
				return false;
			}

			return now - EntryTime > Timeout;
		}
	}
}
