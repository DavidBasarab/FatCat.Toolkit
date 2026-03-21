namespace Tests.FatCat.Toolkit.Data.Mongo.DataRepositorySpecs;

public class DataRepositoryConnectTests : DataRepositoryTests
{
	private readonly string connectionString;
	private readonly string parameterDatabaseName;

	public DataRepositoryConnectTests()
	{
		connectionString = Faker.RandomString();
		parameterDatabaseName = Faker.RandomString();
	}

	[Fact]
	public void GetCollection()
	{
		repository.Connect(connectionString, parameterDatabaseName);

		A.CallTo(
				() =>
					mongoDataConnection.GetCollection<TestingMongoObject>(connectionString, parameterDatabaseName)
			)
			.MustHaveHappened();
	}

	[Fact]
	public void GetDatabaseName()
	{
		repository.Connect(connectionString);

		A.CallTo(() => mongoNames.GetDatabaseName<TestingMongoObject>()).MustHaveHappened();
	}

	[Fact]
	public void IfDatabaseNameIsNotProvidedThenUseDatabaseFromObject()
	{
		repository.Connect(connectionString);

		repository.DatabaseName.Should().Be(databaseName);
	}

	[Fact]
	public void SetCollectionOnRepository()
	{
		repository.Connect(connectionString);

		repository.Collection.Should().Be(collection);
	}

	[Fact]
	public void SetDatabaseNameOnRepository()
	{
		repository.Connect(connectionString, parameterDatabaseName);

		repository.DatabaseName.Should().Be(parameterDatabaseName);
	}
}
