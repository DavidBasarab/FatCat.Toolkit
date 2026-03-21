using FatCat.Toolkit.Testing;
using MongoDB.Driver;

namespace Tests.FatCat.Toolkit.Data.Mongo.DataRepositorySpecs;

public class UpdateListTests : EnsureCollectionTests
{
	private readonly EasyCapture<ExpressionFilterDefinition<TestingMongoObject>> updateFilterCapture;

	public UpdateListTests()
	{
		updateFilterCapture = new EasyCapture<ExpressionFilterDefinition<TestingMongoObject>>();
		var updateCapture = new EasyCapture<TestingMongoObject>();

		A.CallTo(
				() => collection.ReplaceOneAsync(updateFilterCapture, updateCapture, A<ReplaceOptions>._, default)
			)
			.Returns(new ReplaceOneResult.Acknowledged(1, 1, default));
	}

	[Fact]
	public async Task CallReplaceOneToUpdate()
	{
		await repository.Update(itemList);

		foreach (var currentItem in itemList)
		{
			A.CallTo(
					() =>
						collection.ReplaceOneAsync(
							A<ExpressionFilterDefinition<TestingMongoObject>>._,
							currentItem,
							A<ReplaceOptions>._,
							default
						)
				)
				.MustHaveHappened();
		}

		var filter = updateFilterCapture.Values.FirstOrDefault()!.Expression.Compile();

		foreach (var currentItem in itemList)
		{
			filter(currentItem).Should().BeTrue();
		}
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
