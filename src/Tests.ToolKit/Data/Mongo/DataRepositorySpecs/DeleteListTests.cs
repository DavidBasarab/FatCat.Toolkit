using FatCat.Toolkit.Testing;
using MongoDB.Driver;

namespace Tests.FatCat.Toolkit.Data.Mongo.DataRepositorySpecs;

public class DeleteListTests : EnsureCollectionTests
{
	[Fact]
	public async Task CallDeleteManyOnce()
	{
		await repository.Delete(itemList);

		A.CallTo(() => collection.DeleteManyAsync(A<FilterDefinition<TestingMongoObject>>._, default))
			.MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task NotCallDeleteOne()
	{
		await repository.Delete(itemList);

		A.CallTo(() => collection.DeleteOneAsync(A<FilterDefinition<TestingMongoObject>>._, default))
			.MustNotHaveHappened();
	}

	[Fact]
	public async Task NotTouchTheCollectionForAnEmptyList()
	{
		await repository.Delete([]);

		A.CallTo(collection).MustNotHaveHappened();
	}

	[Fact]
	public void ReturnDeleteItemList()
	{
		repository.Delete(itemList).Should().BeEquivalentTo(itemList);
	}

	protected override Task TestMethod()
	{
		return repository.Delete(itemList);
	}
}
