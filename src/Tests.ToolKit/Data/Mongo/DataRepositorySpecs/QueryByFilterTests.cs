using FatCat.Toolkit.Data.Mongo;
using FatCat.Toolkit.Testing;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Tests.FatCat.Toolkit.Data.Mongo.DataRepositorySpecs;

public class QueryByFilterTests : EnsureCollectionTests
{
	private readonly EasyCapture<ExpressionFilterDefinition<TestingMongoObject>> countFilterCapture;
	private readonly EasyCapture<ExpressionFilterDefinition<TestingMongoObject>> findFilterCapture;
	private readonly int filterNumber;
	private readonly int limit;
	private readonly EasyCapture<FindOptions<TestingMongoObject, TestingMongoObject>> optionsCapture;
	private readonly List<TestingMongoObject> pageItems;
	private readonly int skip;

	private long totalCount;

	public QueryByFilterTests()
	{
		filterNumber = Faker.RandomInt();
		skip = Faker.RandomInt();
		limit = Faker.RandomInt();
		totalCount = Faker.RandomInt();

		pageItems = Faker.Create<List<TestingMongoObject>>(3);

		countFilterCapture = new EasyCapture<ExpressionFilterDefinition<TestingMongoObject>>();
		findFilterCapture = new EasyCapture<ExpressionFilterDefinition<TestingMongoObject>>();
		optionsCapture = new EasyCapture<FindOptions<TestingMongoObject, TestingMongoObject>>();

		A.CallTo(() => collection.CountDocumentsAsync(countFilterCapture, default, default)).ReturnsLazily(() => totalCount);

		A.CallTo(() => collection.FindAsync<TestingMongoObject>(findFilterCapture, optionsCapture, default))
			.ReturnsLazily<IAsyncCursor<TestingMongoObject>>(() => new TestingAsyncCursor<TestingMongoObject>(pageItems));
	}

	[Fact]
	public async Task CountTheWholeFilterForTheTotal()
	{
		var page = await Query();

		A.CallTo(() => collection.CountDocumentsAsync(A<FilterDefinition<TestingMongoObject>>._, default, default))
			.MustHaveHappenedOnceExactly();

		countFilterCapture.Value.Expression.Compile()(MatchingItem()).Should().BeTrue();

		page.TotalCount.Should().Be(totalCount);
	}

	[Fact]
	public async Task FindTheFilteredSlice()
	{
		var page = await Query();

		A.CallTo(() => collection.FindAsync<TestingMongoObject>(A<FilterDefinition<TestingMongoObject>>._, A<FindOptions<TestingMongoObject, TestingMongoObject>>._, default))
			.MustHaveHappenedOnceExactly();

		findFilterCapture.Value.Expression.Compile()(MatchingItem()).Should().BeTrue();

		optionsCapture.Value.Skip.Should().Be(skip);

		optionsCapture.Value.Limit.Should().Be(limit);

		page.Items.Should().BeEquivalentTo(pageItems);
	}

	[Fact]
	public async Task SortDescendingWhenAsked()
	{
		await repository.QueryByFilter(i => i.Number == filterNumber, i => i.Number, true, skip, limit);

		RenderedSort().Should().Be(new BsonDocument("Number", -1));
	}

	[Fact]
	public async Task SortAscendingWhenAsked()
	{
		await repository.QueryByFilter(i => i.Number == filterNumber, i => i.Number, false, skip, limit);

		RenderedSort().Should().Be(new BsonDocument("Number", 1));
	}

	[Fact]
	public async Task SortDescendingOnADateTimeFieldWhenAsked()
	{
		await repository.QueryByFilter(i => i.Number == filterNumber, i => i.SomeDate, true, skip, limit);

		RenderedSort().Should().Be(new BsonDocument("SomeDate", -1));
	}

	[Fact]
	public async Task SortAscendingOnADateTimeFieldWhenAsked()
	{
		await repository.QueryByFilter(i => i.Number == filterNumber, i => i.SomeDate, false, skip, limit);

		RenderedSort().Should().Be(new BsonDocument("SomeDate", 1));
	}

	[Fact]
	public async Task ReturnAnEmptyPageWhenNothingMatches()
	{
		totalCount = 0;
		pageItems.Clear();

		var page = await Query();

		page.Items.Should().BeEmpty();

		page.TotalCount.Should().BeZero();
	}

	[Fact]
	public void RequireAConnectionToQuery()
	{
		repository.Collection = null;

		Func<Task> queryAction = TestMethod;

		queryAction.Should().ThrowAsync<ConnectionToMongoIsRequired>();

		A.CallTo(() => collection.FindAsync<TestingMongoObject>(A<FilterDefinition<TestingMongoObject>>._, A<FindOptions<TestingMongoObject, TestingMongoObject>>._, default))
			.MustNotHaveHappened();
	}

	protected override Task TestMethod()
	{
		return repository.QueryByFilter(i => i.Number == filterNumber, i => i.Number, true, skip, limit);
	}

	private Task<PagedResults<TestingMongoObject>> Query()
	{
		return repository.QueryByFilter(i => i.Number == filterNumber, i => i.Number, true, skip, limit);
	}

	private BsonDocument RenderedSort()
	{
		var serializerRegistry = BsonSerializer.SerializerRegistry;
		var documentSerializer = serializerRegistry.GetSerializer<TestingMongoObject>();

		return optionsCapture.Value.Sort.Render(new RenderArgs<TestingMongoObject>(documentSerializer, serializerRegistry));
	}

	private TestingMongoObject MatchingItem()
	{
		var matchingItem = Faker.Create<TestingMongoObject>();

		matchingItem.Number = filterNumber;

		return matchingItem;
	}
}
