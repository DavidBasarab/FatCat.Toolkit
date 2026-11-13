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
	///     A capped cache may exceed this transiently by at most the number of concurrent writers.
	/// </summary>
	public int? MaximumItems { get; set; }

	/// <summary>
	///     The least time between two full sweeps of expired entries. Defaults to 30 seconds.
	/// </summary>
	public TimeSpan SweepInterval { get; set; } = 30.Seconds();
}
