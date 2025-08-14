using FatCat.Toolkit.Data.Mongo;
using MongoDB.Driver;

namespace Tests.FatCat.Toolkit.Data.Mongo.DataRepositorySpecs;

public abstract class DataRepositoryTests
{
	protected readonly TestingMongoObject item;
	protected readonly List<TestingMongoObject> itemList;
	protected readonly MongoRepository<TestingMongoObject> repository;
	protected IMongoCollection<TestingMongoObject> collection = null!;
	protected string databaseName = null!;
	protected IMongoDataConnection mongoDataConnection = null!;
	protected IMongoNames mongoNames = null!;

	protected DataRepositoryTests()
	{
		SetUpDataConnection();
		SetUpDataNames();

		item = Faker.Create<TestingMongoObject>();
		itemList = Faker.Create<List<TestingMongoObject>>(4);

		repository = new MongoRepository<TestingMongoObject>(mongoDataConnection, mongoNames)
		{
			Collection = collection,
		};
	}

	private void SetUpDataConnection()
	{
		mongoDataConnection = A.Fake<IMongoDataConnection>();

		collection = A.Fake<IMongoCollection<TestingMongoObject>>();

		A.CallTo(() => mongoDataConnection.GetCollection<TestingMongoObject>(A<string>._, A<string>._))
			.Returns(collection);
	}

	private void SetUpDataNames()
	{
		mongoNames = A.Fake<IMongoNames>();

		databaseName = Faker.RandomString();

		A.CallTo(() => mongoNames.GetDatabaseName<TestingMongoObject>()).Returns(databaseName);
	}
}
