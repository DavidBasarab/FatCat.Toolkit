using FatCat.Toolkit.Caching;
using Humanizer;

namespace Tests.FatCat.Toolkit.Caching.FatCatCacheSpecs;

public class ConstructorTests : FatCatCacheTests
{
	[Fact]
	public void AcceptAPositiveMaximumItems()
	{
		options.MaximumItems = Faker.RandomInt(1, 1000);

		Action buildAction = BuildTheCache;

		buildAction.Should().Not.Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	public void NameMaximumItemsInTheException()
	{
		options.MaximumItems = 0;

		Action buildAction = BuildTheCache;

		buildAction
			.Should()
			.Throw<ArgumentOutOfRangeException>()
			.WithParameterName(nameof(FatCatCacheOptions<TestCacheItem>.MaximumItems));
	}

	[Fact]
	public void NameSweepIntervalInTheException()
	{
		options.SweepInterval = TimeSpan.Zero;

		Action buildAction = BuildTheCache;

		buildAction
			.Should()
			.Throw<ArgumentOutOfRangeException>()
			.WithParameterName(nameof(FatCatCacheOptions<TestCacheItem>.SweepInterval));
	}

	[Fact]
	public void StoreItemsWhenBuiltWithTheClockOnlyConstructor()
	{
		var cache = new FatCatCache<TestCacheItem>(dateTimeUtilities);
		var item = Faker.Create<TestCacheItem>();

		cache.Add(item);

		cache.Get(item.CacheId).Should().Be(item);
	}

	[Fact]
	public void StoreItemsWhenBuiltWithTheParameterlessConstructor()
	{
		// ADR-1: this constructor wires the real clock; the item has no timeout, so the outcome never depends on it
		var cache = new FatCatCache<TestCacheItem>();
		var item = Faker.Create<TestCacheItem>();

		cache.Add(item);

		cache.Get(item.CacheId).Should().Be(item);
	}

	[Fact]
	public void ThrowWhenMaximumItemsIsNegative()
	{
		options.MaximumItems = -Faker.RandomInt(1, 1000);

		Action buildAction = BuildTheCache;

		buildAction.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	public void ThrowWhenMaximumItemsIsZero()
	{
		options.MaximumItems = 0;

		Action buildAction = BuildTheCache;

		buildAction.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	public void ThrowNamingOptionsWhenOptionsAreNull()
	{
		Action buildAction = () => _ = new FatCatCache<TestCacheItem>(dateTimeUtilities, null);

		buildAction.Should().Throw<ArgumentNullException>().WithParameterName("options");
	}

	[Fact]
	public void ThrowNamingTheClockWhenTheClockIsNull()
	{
		Action buildAction = () => _ = new FatCatCache<TestCacheItem>(null, options);

		buildAction.Should().Throw<ArgumentNullException>().WithParameterName("dateTimeUtilities");
	}

	[Fact]
	public void ThrowWhenSweepIntervalIsNegative()
	{
		options.SweepInterval = -Faker.RandomInt(1, 1000).Seconds();

		Action buildAction = BuildTheCache;

		buildAction.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	public void ThrowWhenSweepIntervalIsZero()
	{
		options.SweepInterval = TimeSpan.Zero;

		Action buildAction = BuildTheCache;

		buildAction.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	public void UseTheInjectedClockWhenBuiltWithTheClockOnlyConstructor()
	{
		var cache = new FatCatCache<TestCacheItem>(dateTimeUtilities);
		var item = Faker.Create<TestCacheItem>();

		cache.Add(item, 5.Minutes());

		AdvanceClock(6.Minutes());

		cache.Get(item.CacheId).Should().BeNull();
	}
}
