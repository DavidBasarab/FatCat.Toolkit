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

public class FatCatCache<T> : IFatCatCache<T>
	where T : class, ICacheItem
{
	private readonly ConcurrentDictionary<string, CacheEntry<T>> cache = new();

	private bool timeoutEnabled;

	public void Add(T cacheItem, TimeSpan? timeout = null)
	{
		RemoveExpiredItems();

		if (!timeoutEnabled && timeout != null)
		{
			timeoutEnabled = true;
		}

		cache.AddOrUpdate(
			cacheItem.CacheId,
			new CacheEntry<T>(cacheItem, timeout),
			(key, value) => new CacheEntry<T>(cacheItem, timeout)
		);
	}

	public void Add(List<T> cacheItems, TimeSpan? timeout = null)
	{
		foreach (var item in cacheItems)
		{
			Add(item);
		}
	}

	public void Clear()
	{
		cache.Clear();
	}

	public T? Get(string cacheId)
	{
		RemoveExpiredItems();

		cache.TryGetValue(cacheId, out var entry);

		return entry?.Item;
	}

	public IList<T> GetAll()
	{
		RemoveExpiredItems();

		return cache.Values.Select(i => i.Item).ToList();
	}

	public bool InCache(string cacheId)
	{
		RemoveExpiredItems();

		return cache.ContainsKey(cacheId);
	}

	public void Remove(string cacheId)
	{
		RemoveExpiredItems();

		cache.TryRemove(cacheId, out _);
	}

	private void RemoveExpiredItems()
	{
		if (!timeoutEnabled || cache.IsEmpty)
		{
			return;
		}

		foreach (var value in cache.Values.Where(i => i.HasExpired()))
		{
			cache.Remove(value.Item.CacheId, out _);
		}

		if (cache.IsEmpty)
		{
			timeoutEnabled = false;
		}
	}

	private class CacheEntry<TType>
		where TType : class, ICacheItem
	{
		public DateTime EntryTime { get; }

		public TType Item { get; }

		public TimeSpan? Timeout { get; }

		public CacheEntry(TType item, TimeSpan? timeout)
		{
			Item = item;
			Timeout = timeout;
			EntryTime = DateTime.UtcNow;
		}

		public bool HasExpired()
		{
			if (Timeout == null)
			{
				return false;
			}

			var timeSinceEntry = DateTime.UtcNow - EntryTime;

			return timeSinceEntry > Timeout;
		}
	}
}
