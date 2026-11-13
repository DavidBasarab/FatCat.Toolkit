#nullable enable
using FakeItEasy;
using FatCat.Fakes;
using FatCat.Toolkit.Caching;

namespace FatCat.Toolkit.Testing;

public class FakeFatCatCache<T> : IFatCatCache<T>
	where T : class, ICacheItem
{
	public IFatCatCache<T> Cache { get; }

	public T CacheItem { get; set; }

	public List<T> CacheList { get; set; }

	public FakeFatCatCache()
	{
		Cache = A.Fake<IFatCatCache<T>>();
		CacheItem = Faker.Create<T>();

		A.CallTo(() => Cache.Get(A<string>._)).ReturnsLazily(() => CacheItem);

		CacheList = Faker.Create<List<T>>();

		A.CallTo(() => Cache.GetAll()).ReturnsLazily(() => CacheList);
	}

	public void Add(T cacheItem, TimeSpan? timeout = null)
	{
		Cache.Add(cacheItem, timeout);
	}

	public void Add(List<T> cacheItems, TimeSpan? timeout = null)
	{
		Cache.Add(cacheItems, timeout);
	}

	public void Clear()
	{
		Cache.Clear();
	}

	public T? Get(string cacheId)
	{
		return Cache.Get(cacheId);
	}

	public IList<T> GetAll()
	{
		return Cache.GetAll();
	}

	public bool InCache(string cacheId)
	{
		return Cache.InCache(cacheId);
	}

	public void Remove(string cacheId)
	{
		Cache.Remove(cacheId);
	}

	public void SetItemInCache()
	{
		A.CallTo(() => Cache.InCache(A<string>._)).Returns(true);

		A.CallTo(() => Cache.Get(A<string>._)).Returns(CacheItem);
	}

	public void SetItemNotInCache()
	{
		A.CallTo(() => Cache.InCache(A<string>._)).Returns(false);

		A.CallTo(() => Cache.Get(A<string>._)).Returns(null);
	}

	public void VerifyAdd(T? expectedItem = null)
	{
		if (expectedItem == null)
		{
			A.CallTo(() => Cache.Add(A<T>._, A<TimeSpan?>._)).MustHaveHappened();
		}
		else
		{
			A.CallTo(() => Cache.Add(expectedItem, A<TimeSpan?>._)).MustHaveHappened();
		}
	}

	public void VerifyAdd(T expectedItem, TimeSpan? expectedTimeout)
	{
		A.CallTo(() => Cache.Add(expectedItem, expectedTimeout)).MustHaveHappened();
	}

	public void VerifyAddList(List<T>? expectedItems = null)
	{
		if (expectedItems == null)
		{
			A.CallTo(() => Cache.Add(A<List<T>>._, A<TimeSpan?>._)).MustHaveHappened();
		}
		else
		{
			A.CallTo(() => Cache.Add(expectedItems, A<TimeSpan?>._)).MustHaveHappened();
		}
	}

	public void VerifyAddList(List<T> expectedItems, TimeSpan? expectedTimeout)
	{
		A.CallTo(() => Cache.Add(expectedItems, expectedTimeout)).MustHaveHappened();
	}

	public void VerifyClear()
	{
		A.CallTo(() => Cache.Clear()).MustHaveHappened();
	}

	public void VerifyDidNotAdd(T? expectedItem = null)
	{
		if (expectedItem == null)
		{
			A.CallTo(() => Cache.Add(A<T>._, A<TimeSpan?>._)).MustNotHaveHappened();
		}
		else
		{
			A.CallTo(() => Cache.Add(expectedItem, A<TimeSpan?>._)).MustNotHaveHappened();
		}
	}

	public void VerifyDidNotAddList(List<T>? expectedItems = null)
	{
		if (expectedItems == null)
		{
			A.CallTo(() => Cache.Add(A<List<T>>._, A<TimeSpan?>._)).MustNotHaveHappened();
		}
		else
		{
			A.CallTo(() => Cache.Add(expectedItems, A<TimeSpan?>._)).MustNotHaveHappened();
		}
	}

	public void VerifyDidNotClear()
	{
		A.CallTo(() => Cache.Clear()).MustNotHaveHappened();
	}

	public void VerifyDidNotGet(string? expectedCacheId)
	{
		if (expectedCacheId == null)
		{
			A.CallTo(() => Cache.Get(A<string>._)).MustNotHaveHappened();
		}
		else
		{
			A.CallTo(() => Cache.Get(expectedCacheId)).MustNotHaveHappened();
		}
	}

	public void VerifyDidNotGetAll()
	{
		A.CallTo(() => Cache.GetAll()).MustNotHaveHappened();
	}

	public void VerifyDidNotInCache(string? expectedId = null)
	{
		if (expectedId == null)
		{
			A.CallTo(() => Cache.InCache(A<string>._)).MustNotHaveHappened();
		}
		else
		{
			A.CallTo(() => Cache.InCache(expectedId)).MustNotHaveHappened();
		}
	}

	public void VerifyDidNotRemove(string? expectedId = null)
	{
		if (expectedId == null)
		{
			A.CallTo(() => Cache.Remove(A<string>._)).MustNotHaveHappened();
		}
		else
		{
			A.CallTo(() => Cache.Remove(expectedId)).MustNotHaveHappened();
		}
	}

	public void VerifyGet(string? expectedCacheId)
	{
		if (expectedCacheId == null)
		{
			A.CallTo(() => Cache.Get(A<string>._)).MustHaveHappened();
		}
		else
		{
			A.CallTo(() => Cache.Get(expectedCacheId)).MustHaveHappened();
		}
	}

	public void VerifyGetAll()
	{
		A.CallTo(() => Cache.GetAll()).MustHaveHappened();
	}

	public void VerifyInCache(string? expectedId = null)
	{
		if (expectedId == null)
		{
			A.CallTo(() => Cache.InCache(A<string>._)).MustHaveHappened();
		}
		else
		{
			A.CallTo(() => Cache.InCache(expectedId)).MustHaveHappened();
		}
	}

	public void VerifyRemove(string? expectedId = null)
	{
		if (expectedId == null)
		{
			A.CallTo(() => Cache.Remove(A<string>._)).MustHaveHappened();
		}
		else
		{
			A.CallTo(() => Cache.Remove(expectedId)).MustHaveHappened();
		}
	}
}
