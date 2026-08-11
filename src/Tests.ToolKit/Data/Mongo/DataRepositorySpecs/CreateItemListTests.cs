using FatCat.Toolkit.Testing;

namespace Tests.FatCat.Toolkit.Data.Mongo.DataRepositorySpecs;

public class CreateItemListTests : EnsureCollectionTests
{
	[Fact]
	public async Task CallInsertManyOnce()
	{
		await repository.Create(itemList);

		A.CallTo(() => collection.InsertManyAsync(itemList, default, default)).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task NotCallInsertOne()
	{
		await repository.Create(itemList);

		A.CallTo(() => collection.InsertOneAsync(A<TestingMongoObject>._, default, default)).MustNotHaveHappened();
	}

	[Fact]
	public async Task NotTouchTheCollectionForAnEmptyList()
	{
		await repository.Create([]);

		A.CallTo(collection).MustNotHaveHappened();
	}

	[Fact]
	public void ReturnListOfCreatedItems()
	{
		repository.Create(itemList).Should().BeEquivalentTo(itemList);
	}

	protected override async Task TestMethod()
	{
		await repository.Create(itemList);
	}
}
