using FatCat.Toolkit.Testing;

namespace Tests.FatCat.Toolkit.Data.Mongo.DataRepositorySpecs;

public class CreateItemListTests : EnsureCollectionTests
{
	[Fact]
	public async Task CallInsertOneForEachItemInList()
	{
		await repository.Create(itemList);

		foreach (var currentItem in itemList)
		{
			A.CallTo(() => collection.InsertOneAsync(currentItem, default, default)).MustHaveHappened();
		}
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
