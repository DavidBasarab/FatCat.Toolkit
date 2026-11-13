namespace Tests.FatCat.Toolkit.Testing.FakeFatCatCacheSpecs;

public class AddListTests : FakeFatCatCacheTests
{
	[Fact]
	public void ForwardANullTimeoutWhenNoneIsGiven()
	{
		fake.Add(items);

		A.CallTo(() => fake.Cache.Add(items, null)).MustHaveHappened();
	}

	[Fact]
	public void ForwardTheTimeoutToTheInnerCache()
	{
		fake.Add(items, timeout);

		A.CallTo(() => fake.Cache.Add(items, timeout)).MustHaveHappened();
	}
}
