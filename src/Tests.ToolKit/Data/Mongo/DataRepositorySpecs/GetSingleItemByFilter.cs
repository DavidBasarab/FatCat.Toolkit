using FatCat.Toolkit.Testing;
using MongoDB.Driver;

namespace Tests.FatCat.Toolkit.Data.Mongo.DataRepositorySpecs;

public class GetSingleItemByFilter : EnsureCollectionTests
{
	private readonly EasyCapture<ExpressionFilterDefinition<TestingMongoObject>> expressionCapture;
	private readonly TestingMongoObject filterItem;
	private readonly int filterNumber;

	public GetSingleItemByFilter()
	{
		filterNumber = Faker.RandomInt();

		filterItem = Faker.Create<TestingMongoObject>(afterCreate: i => i.Number = filterNumber);

		expressionCapture = new EasyCapture<ExpressionFilterDefinition<TestingMongoObject>>();

		A.CallTo(() => collection.FindAsync<TestingMongoObject>(expressionCapture!, default, default))
			.Returns(new TestingAsyncCursor<TestingMongoObject>(new List<TestingMongoObject> { filterItem }));
	}

	[Fact]
	public async Task CallFindAsyncWithFilter()
	{
		await repository.GetByFilter(i => i!.Number == filterNumber);

		A.CallTo(
				() =>
					collection.FindAsync<TestingMongoObject>(
						A<ExpressionFilterDefinition<TestingMongoObject>>._!,
						default,
						default
					)
			)
			.MustHaveHappened();

		expressionCapture.Value.Should().NotBeNull();

		var filter = expressionCapture.Value.Expression.Compile();

		foreach (var currentItem in itemList)
		{
			filter(currentItem!).Should().BeFalse();
		}

		filter(filterItem!).Should().BeTrue();
	}

	[Fact]
	public void ReturnFilterItem()
	{
		repository.GetByFilter(i => i!.Number == filterNumber).Should().Be(filterItem);
	}

	protected override Task TestMethod()
	{
		return repository.GetByFilter(i => i!.Number == filterNumber);
	}
}
