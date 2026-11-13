using FatCat.Toolkit;
using FatCat.Toolkit.Caching;

namespace Tests.FatCat.Toolkit.Caching;

public class SecondTestCacheItem : EqualObject, ICacheItem
{
	public string CacheId
	{
		get { return SomeId; }
	}

	public string SomeId { get; set; }
}
