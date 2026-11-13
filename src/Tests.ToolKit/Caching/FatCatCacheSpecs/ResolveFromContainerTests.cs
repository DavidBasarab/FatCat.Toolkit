using Autofac;
using FatCat.Toolkit;
using FatCat.Toolkit.Caching;
using FatCat.Toolkit.Injection;
using FatCat.Toolkit.Web.Api.SignalR;
using Humanizer;

namespace Tests.FatCat.Toolkit.Caching.FatCatCacheSpecs;

public class ResolveFromContainerTests : FatCatCacheTests
{
	private const int MoreItemsThanAnyTestCap = 500;

	private readonly ContainerBuilder builder = new();

	[Fact]
	public void HonourRegisteredOptions()
	{
		RegisterTheToolkitModuleAndClock();

		builder.RegisterInstance(new FatCatCacheOptions<TestCacheItem> { MaximumItems = 10 });

		var cache = ResolveCache<TestCacheItem>();

		AddItemsTo(cache, 50);

		((FatCatCache<TestCacheItem>)cache).StoredCount.Should().BeLessThanOrEqualTo(10);
	}

	[Fact]
	public void LeaveOtherTypesUnbounded()
	{
		RegisterTheToolkitModuleAndClock();

		builder.RegisterInstance(new FatCatCacheOptions<TestCacheItem> { MaximumItems = 10 });

		var cache = ResolveCache<SecondTestCacheItem>();

		var items = AddItemsTo(cache, MoreItemsThanAnyTestCap);

		items.Should().OnlyContain(item => cache.InCache(item.CacheId));
	}

	[Fact]
	public void ResolveAnUnboundedCacheWhenScannedLikeSystemScope()
	{
		RegisterTheClock();
		ScanTheToolkitAssemblyLikeSystemScope();

		var cache = ResolveCache<TestCacheItem>();

		var items = AddItemsTo(cache, MoreItemsThanAnyTestCap);

		items.Should().OnlyContain(item => cache.InCache(item.CacheId));
	}

	[Fact]
	public void ResolveAnUnboundedCacheWithoutOptions()
	{
		RegisterTheToolkitModuleAndClock();

		var cache = ResolveCache<TestCacheItem>();

		var items = AddItemsTo(cache, MoreItemsThanAnyTestCap);

		items.Should().OnlyContain(item => cache.InCache(item.CacheId));
	}

	[Fact]
	public void UseTheContainerClockWithoutOptions()
	{
		RegisterTheToolkitModuleAndClock();

		var cache = ResolveCache<TestCacheItem>();
		var item = Faker.Create<TestCacheItem>();

		cache.Add(item, 5.Minutes());

		AdvanceClock(6.Minutes());

		cache.Get(item.CacheId).Should().BeNull();
	}

	private static List<TItem> AddItemsTo<TItem>(IFatCatCache<TItem> cache, int count)
		where TItem : class, ICacheItem
	{
		var items = Enumerable.Range(0, count).Select(_ => Faker.Create<TItem>()).ToList();

		cache.Add(items);

		return items;
	}

	private void RegisterTheClock()
	{
		builder.RegisterInstance(dateTimeUtilities).As<IDateTimeUtilities>();
	}

	private void RegisterTheToolkitModuleAndClock()
	{
		builder.RegisterModule<ToolkitModule>();

		RegisterTheClock();
	}

	private IFatCatCache<TItem> ResolveCache<TItem>()
		where TItem : class, ICacheItem
	{
		return builder.Build().Resolve<IFatCatCache<TItem>>();
	}

	private void ScanTheToolkitAssemblyLikeSystemScope()
	{
		var toolkitAssembly = typeof(FatCatCache<>).Assembly;

		builder.RegisterAssemblyModules(toolkitAssembly);

		builder
			.RegisterAssemblyTypes(toolkitAssembly)
			.AsImplementedInterfaces()
			.HasPublicConstructor()
			.PublicOnly()
			.PreserveExistingDefaults()
			.Except<ISystemScope>()
			.AsSelf();
	}
}
