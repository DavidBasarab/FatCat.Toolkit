using FatCat.Toolkit.Testing;
using MongoDB.Driver;

namespace Tests.FatCat.Toolkit.Data.Mongo.DataRepositorySpecs;

public class GetAllByFilterTests : EnsureCollectionTests
{
	private readonly EasyCapture<ExpressionFilterDefinition<TestingMongoObject>> expressionCapture;
	private readonly List<TestingMongoObject> filterItems;
	private readonly int filterNumber;

	public GetAllByFilterTests()
	{
		filterNumber = Faker.RandomInt();

		filterItems = Faker.Create<List<TestingMongoObject>>(3);

		foreach (var filterItem in filterItems)
		{
			filterItem.Number = filterNumber;
		}

		expressionCapture = new EasyCapture<ExpressionFilterDefinition<TestingMongoObject>>();

		A.CallTo(() => collection.FindAsync<TestingMongoObject>(expressionCapture, default, default))
			.Returns(new TestingAsyncCursor<TestingMongoObject>(filterItems));
	}

	[Fact]
	public async Task CallFindAsyncWithFilter()
	{
		await repository.GetAllByFilter(i => i.Number == filterNumber);

		A.CallTo(
				() =>
					collection.FindAsync<TestingMongoObject>(
						A<ExpressionFilterDefinition<TestingMongoObject>>._,
						default,
						default
					)
			)
			.MustHaveHappened();

		expressionCapture.Value.Should().NotBeNull();

		var filter = expressionCapture.Value.Expression.Compile();

		foreach (var currentItem in itemList)
		{
			filter(currentItem).Should().BeFalse();
		}

		foreach (var currentFilterItem in filterItems)
		{
			filter(currentFilterItem).Should().BeTrue();
		}
	}

	[Fact]
	public void ReturnFilteredList()
	{
		repository.GetAllByFilter(i => i.Number == filterNumber).Should().BeEquivalentTo(filterItems);
	}

	protected override Task TestMethod()
	{
		return repository.GetAllByFilter(i => i.Number == filterNumber);
	}
}
