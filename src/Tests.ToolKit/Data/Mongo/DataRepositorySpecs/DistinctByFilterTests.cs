using FatCat.Toolkit.Data.Mongo;
using FatCat.Toolkit.Testing;
using MongoDB.Driver;

namespace Tests.FatCat.Toolkit.Data.Mongo.DataRepositorySpecs;

public class DistinctByFilterTests : EnsureCollectionTests
{
	private readonly EasyCapture<FieldDefinition<TestingMongoObject, string>> fieldCapture;
	private readonly EasyCapture<ExpressionFilterDefinition<TestingMongoObject>> filterCapture;
	private readonly int filterNumber;

	private List<string> distinctNames;

	public DistinctByFilterTests()
	{
		filterNumber = Faker.RandomInt();
		distinctNames = Faker.Create<List<string>>(3);

		fieldCapture = new EasyCapture<FieldDefinition<TestingMongoObject, string>>();
		filterCapture = new EasyCapture<ExpressionFilterDefinition<TestingMongoObject>>();

		A.CallTo(() => collection.DistinctAsync<string>(fieldCapture, filterCapture, default, default))
			.ReturnsLazily<IAsyncCursor<string>>(() => new TestingAsyncCursor<string>(distinctNames));
	}

	[Fact]
	public async Task CallDistinctWithTheFieldAndFilter()
	{
		await repository.DistinctByFilter(i => i.Name, i => i.Number == filterNumber);

		A.CallTo(() =>
				collection.DistinctAsync<string>(
					A<FieldDefinition<TestingMongoObject, string>>._,
					A<FilterDefinition<TestingMongoObject>>._,
					default,
					default
				)
			)
			.MustHaveHappenedOnceExactly();

		var field = (ExpressionFieldDefinition<TestingMongoObject, string>)fieldCapture.Value;

		field.Expression.Compile()(item).Should().Be(item.Name);

		filterCapture.Value.Expression.Compile()(MatchingItem()).Should().BeTrue();
	}

	[Fact]
	public async Task ReturnANullValueForDocumentsWithNoValue()
	{
		distinctNames = [Faker.RandomString(), null, Faker.RandomString()];

		var values = await repository.DistinctByFilter(i => i.Name, i => i.Number == filterNumber);

		values.Should().ContainNulls().Equal(distinctNames);
	}

	[Fact]
	public async Task ReturnAnEmptyListWhenNothingMatches()
	{
		distinctNames = [];

		var values = await repository.DistinctByFilter(i => i.Name, i => i.Number == filterNumber);

		var aListWasReturned = values is not null;

		aListWasReturned.Should().BeTrue();

		values.Should().BeEmpty();
	}

	[Fact]
	public async Task ReturnTheDistinctValues()
	{
		var values = await repository.DistinctByFilter(i => i.Name, i => i.Number == filterNumber);

		values.Should().Equal(distinctNames);
	}

	[Fact]
	public void RequireAConnectionToGetDistinctValues()
	{
		repository.Collection = null;

		Func<Task> distinctAction = TestMethod;

		distinctAction.Should().ThrowAsync<ConnectionToMongoIsRequired>();

		A.CallTo(() =>
				collection.DistinctAsync<string>(
					A<FieldDefinition<TestingMongoObject, string>>._,
					A<FilterDefinition<TestingMongoObject>>._,
					default,
					default
				)
			)
			.MustNotHaveHappened();
	}

	protected override Task TestMethod()
	{
		return repository.DistinctByFilter(i => i.Name, i => i.Number == filterNumber);
	}

	private TestingMongoObject MatchingItem()
	{
		var matchingItem = Faker.Create<TestingMongoObject>();

		matchingItem.Number = filterNumber;

		return matchingItem;
	}
}
