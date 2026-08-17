using FatCat.Toolkit.Data.Mongo;
using FatCat.Toolkit.Testing;
using MongoDB.Driver;

namespace Tests.FatCat.Toolkit.Data.Mongo.DataRepositorySpecs;

public class DeleteByFilterTests : EnsureCollectionTests
{
	private readonly EasyCapture<ExpressionFilterDefinition<TestingMongoObject>> filterCapture;
	private readonly int filterNumber;

	private long deletedCount;

	public DeleteByFilterTests()
	{
		filterNumber = Faker.RandomInt();
		deletedCount = Faker.RandomInt();

		filterCapture = new EasyCapture<ExpressionFilterDefinition<TestingMongoObject>>();

		A.CallTo(() => collection.DeleteManyAsync(filterCapture, default))
			.ReturnsLazily<DeleteResult>(() => new DeleteResult.Acknowledged(deletedCount));
	}

	[Fact]
	public async Task DeleteEveryMatchWithOneCommand()
	{
		await repository.DeleteByFilter(i => i.Number == filterNumber);

		A.CallTo(() => collection.DeleteManyAsync(A<FilterDefinition<TestingMongoObject>>._, default))
			.MustHaveHappenedOnceExactly();

		filterCapture.Value.Expression.Compile()(MatchingItem()).Should().BeTrue();
	}

	[Fact]
	public async Task ReturnTheDeletedCount()
	{
		var count = await repository.DeleteByFilter(i => i.Number == filterNumber);

		count.Should().Be(deletedCount);
	}

	[Fact]
	public async Task ReturnZeroWhenNothingMatchesTheDelete()
	{
		deletedCount = 0;

		var count = await repository.DeleteByFilter(i => i.Number == filterNumber);

		count.Should().BeZero();
	}

	[Fact]
	public void RequireAConnectionToDelete()
	{
		repository.Collection = null;

		Func<Task> deleteAction = TestMethod;

		deleteAction.Should().ThrowAsync<ConnectionToMongoIsRequired>();

		A.CallTo(() => collection.DeleteManyAsync(A<FilterDefinition<TestingMongoObject>>._, default)).MustNotHaveHappened();
	}

	protected override Task TestMethod()
	{
		return repository.DeleteByFilter(i => i.Number == filterNumber);
	}

	private TestingMongoObject MatchingItem()
	{
		var matchingItem = Faker.Create<TestingMongoObject>();

		matchingItem.Number = filterNumber;

		return matchingItem;
	}
}
