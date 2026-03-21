using FatCat.Toolkit.Testing;
using MongoDB.Driver;

namespace Tests.FatCat.Toolkit.Data.Mongo.DataRepositorySpecs;

public class UpdateTests : EnsureCollectionTests
{
	private readonly EasyCapture<ExpressionFilterDefinition<TestingMongoObject>> updateFilterCapture;

	public UpdateTests()
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
		await repository.Update(item);

		A.CallTo(
				() =>
					collection.ReplaceOneAsync(
						A<ExpressionFilterDefinition<TestingMongoObject>>._,
						item,
						A<ReplaceOptions>._,
						default
					)
			)
			.MustHaveHappened();

		var filter = updateFilterCapture.Value.Expression.Compile();

		filter(item).Should().BeTrue();
	}

	[Fact]
	public void ReturnUpdatedItem()
	{
		repository.Update(item).Should().Be(item);
	}

	protected override Task TestMethod()
	{
		return repository.Update(item);
	}
}
