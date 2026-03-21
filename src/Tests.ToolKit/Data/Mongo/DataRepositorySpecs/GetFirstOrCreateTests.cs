using FatCat.Toolkit.Testing;
using MongoDB.Driver;

namespace Tests.FatCat.Toolkit.Data.Mongo.DataRepositorySpecs;

public class GetFirstOrCreateTests : EnsureCollectionTests
{
	private readonly EasyCapture<ExpressionFilterDefinition<TestingMongoObject>> expressionCapture;
	private readonly TestingMongoObject firstItem;
	private EasyCapture<TestingMongoObject> insertCapture = null!;

	public GetFirstOrCreateTests()
	{
		SetUpInsertCapture();

		firstItem = Faker.Create<TestingMongoObject>();

		expressionCapture = new EasyCapture<ExpressionFilterDefinition<TestingMongoObject>>();

		SetUpToReturnItem();
	}

	[Fact]
	public async Task GetFirstItem()
	{
		await repository.GetFirstOrCreate();

		A.CallTo(
				() =>
					collection.FindAsync<TestingMongoObject>(
						A<ExpressionFilterDefinition<TestingMongoObject>>._!,
						default,
						default
					)
			)
			.MustHaveHappened();

		var filter = expressionCapture.Value.Expression.Compile();

		filter(Faker.Create<TestingMongoObject>()).Should().BeTrue();
	}

	[Fact]
	public void IfFirstItemIsNotNullReturnItem()
	{
		SetUpToReturnItem();

		repository.GetFirstOrCreate().Should().Be(firstItem);

		A.CallTo(() => collection.InsertOneAsync(A<TestingMongoObject>._, default, default)).MustNotHaveHappened();
	}

	[Fact]
	public async Task IfFirstItemIsNullCreateIt()
	{
		SetUpToNotReturnAnItem();

		await repository.GetFirstOrCreate();

		A.CallTo(() => collection.InsertOneAsync(A<TestingMongoObject>._, default, default)).MustHaveHappened();

		var insertedItem = insertCapture.Value;

		insertedItem.Should().NotBeNull();

		insertedItem.Id.Should().NotBeNull();

		insertedItem.Name.Should().BeNullOrEmpty();

		insertedItem.SomeDate.Should().Be(default);
	}

	[Fact]
	public void IfFirstItemIsNullReturnCreatedItem()
	{
		SetUpToNotReturnAnItem();

		repository.GetFirstOrCreate().Should().Be(insertCapture.Value);
	}

	protected override Task TestMethod()
	{
		return repository.GetFirstOrCreate();
	}

	private void SetUpInsertCapture()
	{
		insertCapture = new EasyCapture<TestingMongoObject>();

		A.CallTo(() => collection.InsertOneAsync(insertCapture, default, default)).Returns(Task.CompletedTask);
	}

	private void SetUpToNotReturnAnItem()
	{
		A.CallTo(() => collection.FindAsync<TestingMongoObject>(expressionCapture, default, default))
			.Returns(new TestingAsyncCursor<TestingMongoObject>(new List<TestingMongoObject>()));
	}

	private void SetUpToReturnItem()
	{
		A.CallTo(() => collection.FindAsync<TestingMongoObject>(expressionCapture, default, default))
			.Returns(new TestingAsyncCursor<TestingMongoObject>(new List<TestingMongoObject> { firstItem }));
	}
}
