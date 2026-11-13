namespace Tests.FatCat.Toolkit.Caching.FatCatCacheSpecs;

public class ClearTests : FatCatCacheTests
{
	[Fact]
	public void EmptyTheCache()
	{
		Cache.Add(CreateItems());

		Cache.Clear();

		Cache.StoredCount.Should().Be(0);
	}
}
