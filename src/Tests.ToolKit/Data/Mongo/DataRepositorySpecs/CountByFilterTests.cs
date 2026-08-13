using FatCat.Toolkit.Data.Mongo;
using FatCat.Toolkit.Testing;
using MongoDB.Driver;

namespace Tests.FatCat.Toolkit.Data.Mongo.DataRepositorySpecs;

public class CountByFilterTests : EnsureCollectionTests
{
	private readonly EasyCapture<ExpressionFilterDefinition<TestingMongoObject>> filterCapture;
	private readonly int filterNumber;

	private long matchingDocumentCount;

	public CountByFilterTests()
	{
		filterNumber = Faker.RandomInt();
		matchingDocumentCount = Faker.RandomInt();

		filterCapture = new EasyCapture<ExpressionFilterDefinition<TestingMongoObject>>();

		A.CallTo(() => collection.CountDocumentsAsync(filterCapture, default, default))
			.ReturnsLazily(() => matchingDocumentCount);
	}

	[Fact]
	public async Task CallCountDocumentsWithTheFilter()
	{
		await repository.CountByFilter(i => i.Number == filterNumber);

		A.CallTo(() => collection.CountDocumentsAsync(A<FilterDefinition<TestingMongoObject>>._, default, default))
			.MustHaveHappenedOnceExactly();

		filterCapture.Value.Should().Not.BeNull();

		var filter = filterCapture.Value.Expression.Compile();

		foreach (var currentItem in itemList)
		{
			filter(currentItem).Should().BeFalse();
		}

		filter(MatchingItem()).Should().BeTrue();
	}

	[Fact]
	public void RequireAConnectionToCount()
	{
		repository.Collection = null;

		Func<Task> countAction = TestMethod;

		countAction.Should().ThrowAsync<ConnectionToMongoIsRequired>();

		A.CallTo(() => collection.CountDocumentsAsync(A<FilterDefinition<TestingMongoObject>>._, default, default))
			.MustNotHaveHappened();
	}

	[Fact]
	public async Task ReturnTheCount()
	{
		var count = await repository.CountByFilter(i => i.Number == filterNumber);

		count.Should().Be(matchingDocumentCount);
	}

	[Fact]
	public async Task ReturnZeroWhenNothingMatches()
	{
		matchingDocumentCount = 0;

		var count = await repository.CountByFilter(i => i.Number == filterNumber);

		count.Should().BeZero();
	}

	protected override Task TestMethod()
	{
		return repository.CountByFilter(i => i.Number == filterNumber);
	}

	private TestingMongoObject MatchingItem()
	{
		var matchingItem = Faker.Create<TestingMongoObject>();

		matchingItem.Number = filterNumber;

		return matchingItem;
	}
}
