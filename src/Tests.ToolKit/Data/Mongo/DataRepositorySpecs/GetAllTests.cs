using FatCat.Toolkit.Testing;
using MongoDB.Driver;

namespace Tests.FatCat.Toolkit.Data.Mongo.DataRepositorySpecs;

public class GetAllTests : EnsureCollectionTests
{
	public GetAllTests()
	{
		A.CallTo(
				() =>
					collection.FindAsync<TestingMongoObject>(
						A<ExpressionFilterDefinition<TestingMongoObject>>._,
						default,
						default
					)
			)
			.Returns(new TestingAsyncCursor<TestingMongoObject>(itemList));
	}

	[Fact]
	public async Task FindFromCollection()
	{
		await repository.GetAll();

		A.CallTo(
				() =>
					collection.FindAsync<TestingMongoObject>(
						A<ExpressionFilterDefinition<TestingMongoObject>>._,
						default,
						default
					)
			)
			.MustHaveHappened();
	}

	[Fact]
	public void ReturnTheItems()
	{
		repository.GetAll().Should().BeEquivalentTo(itemList);
	}

	protected override Task TestMethod()
	{
		return repository.GetAll();
	}
}
