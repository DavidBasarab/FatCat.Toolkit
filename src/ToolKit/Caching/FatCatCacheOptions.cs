using Humanizer;

namespace FatCat.Toolkit.Caching;

/// <summary>
///     Settings for the <see cref="FatCatCache{T}" /> of one item type. The cache reads and validates these values once,
///     when it is constructed; changing them afterwards has no effect on a cache that already exists.
/// </summary>
public class FatCatCacheOptions<T>
	where T : class, ICacheItem
{
	/// <summary>
	///     The most entries the cache holds. <c>null</c>, the default, means the cache is unbounded.
	///     When a new key is added to a full cache, expired entries are removed first; if the cache is still full,
	///     the oldest entries by the time they were added or last updated are evicted until about 90% of this value
	///     remains, which frees at least one slot when no other thread is writing. Updating an existing key never evicts.
	///     Under concurrent writes a capped cache may hold more than this by at most the number of concurrent writers;
	///     that excess stays until the next new key is added.
	/// </summary>
	public int? MaximumItems { get; set; }

	/// <summary>
	///     The least time between two full sweeps of expired entries. Defaults to 30 seconds.
	/// </summary>
	public TimeSpan SweepInterval { get; set; } = 30.Seconds();
}
