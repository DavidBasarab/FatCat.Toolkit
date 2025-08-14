using FatCat.Toolkit.Caching;

namespace OneOff.Old;

public class TestCacheItem : ICacheItem
{
	public string CacheId
	{
		get => Name;
	}

	public string Name { get; set; }

	public int Number { get; set; }
}
