using FatCat.Toolkit.Testing;
using MongoDB.Driver;

namespace Tests.FatCat.Toolkit.Data.Mongo.DataRepositorySpecs;

public class UpdateListTests : EnsureCollectionTests
{
	[Fact]
	public async Task CallBulkWriteOnce()
	{
		await repository.Update(itemList);

		A.CallTo(() => collection.BulkWriteAsync(A<IEnumerable<WriteModel<TestingMongoObject>>>._, default, default))
			.MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task ReplaceEveryItemInTheBulkWrite()
	{
		await repository.Update(itemList);

		A.CallTo(() =>
				collection.BulkWriteAsync(
					A<IEnumerable<WriteModel<TestingMongoObject>>>.That.Matches(requests =>
						requests
							.Cast<ReplaceOneModel<TestingMongoObject>>()
							.Select(model => model.Replacement)
							.SequenceEqual(itemList)
					),
					default,
					default
				)
			)
			.MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task NotCallReplaceOne()
	{
		await repository.Update(itemList);

		A.CallTo(() =>
				collection.ReplaceOneAsync(
					A<FilterDefinition<TestingMongoObject>>._,
					A<TestingMongoObject>._,
					A<ReplaceOptions>._,
					default
				)
			)
			.MustNotHaveHappened();
	}

	[Fact]
	public async Task NotTouchTheCollectionForAnEmptyList()
	{
		await repository.Update([]);

		A.CallTo(collection).MustNotHaveHappened();
	}

	[Fact]
	public void ReturnUpdatedItem()
	{
		repository.Update(itemList).Should().Be(itemList);
	}

	protected override Task TestMethod()
	{
		return repository.Update(itemList);
	}
}
