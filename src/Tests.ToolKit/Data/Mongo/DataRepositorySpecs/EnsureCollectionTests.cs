using FatCat.Toolkit.Data.Mongo;

namespace Tests.FatCat.Toolkit.Data.Mongo.DataRepositorySpecs;

public abstract class EnsureCollectionTests : DataRepositoryTests
{
	[Fact]
	public void EnsureCollection()
	{
		repository.Collection = null;

		Func<Task> exceptionAction = TestMethod;

		exceptionAction.Should().ThrowAsync<ConnectionToMongoIsRequired>();
	}

	protected abstract Task TestMethod();
}
