using FatCat.Toolkit.Testing;
using Humanizer;
using Tests.FatCat.Toolkit.Caching;

namespace Tests.FatCat.Toolkit.Testing.FakeFatCatCacheSpecs;

public abstract class FakeFatCatCacheTests
{
	protected readonly FakeFatCatCache<TestCacheItem> fake = new();
	protected readonly TestCacheItem item = Faker.Create<TestCacheItem>();
	protected readonly List<TestCacheItem> items = Faker.Create<List<TestCacheItem>>();
	protected readonly TestCacheItem otherItem = Faker.Create<TestCacheItem>();
	protected readonly List<TestCacheItem> otherItems = Faker.Create<List<TestCacheItem>>();
	protected readonly TimeSpan otherTimeout;
	protected readonly TimeSpan timeout = Faker.Create<TimeSpan>();

	protected FakeFatCatCacheTests()
	{
		otherTimeout = timeout + 1.Minutes();
	}
}
