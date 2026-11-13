namespace Tests.FatCat.Toolkit.Testing.FakeFatCatCacheSpecs;

public class AddTests : FakeFatCatCacheTests
{
	[Fact]
	public void ForwardANullTimeoutWhenNoneIsGiven()
	{
		fake.Add(item);

		A.CallTo(() => fake.Cache.Add(item, null)).MustHaveHappened();
	}

	[Fact]
	public void ForwardTheTimeoutToTheInnerCache()
	{
		fake.Add(item, timeout);

		A.CallTo(() => fake.Cache.Add(item, timeout)).MustHaveHappened();
	}
}
