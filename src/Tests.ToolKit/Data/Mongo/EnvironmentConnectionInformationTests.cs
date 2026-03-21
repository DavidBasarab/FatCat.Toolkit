using FatCat.Toolkit.Data;
using FatCat.Toolkit.Data.Mongo;

namespace Tests.FatCat.Toolkit.Data.Mongo;

public class EnvironmentConnectionInformationTests
{
	private readonly EnvironmentConnectionInformation connectionInformation;
	private string connectionString;
	private string dataBaseName;
	private IEnvironmentRepository environmentRepository;

	public EnvironmentConnectionInformationTests()
	{
		SetUpEnvironmentRepository();

		connectionInformation = new EnvironmentConnectionInformation(environmentRepository);
	}

	[Fact]
	public void OnConnectionStringReadConnectionStringFromEnvironmentRepository()
	{
		connectionInformation.GetConnectionString();

		A.CallTo(() => environmentRepository.Get("MongoConnectionString")).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public void OnDatabaseNameReadFromEnvironmentRepository()
	{
		connectionInformation.GetDatabaseName();

		A.CallTo(() => environmentRepository.Get("MongoDatabaseName")).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public void ReturnConnectionStringFromRepository()
	{
		connectionInformation.GetConnectionString().Should().Be(connectionString);
	}

	[Fact]
	public void ReturnDatabaseNameFromRepository()
	{
		connectionInformation.GetDatabaseName().Should().Be(dataBaseName);
	}

	private void SetUpEnvironmentRepository()
	{
		environmentRepository = A.Fake<IEnvironmentRepository>();

		SetUpGetConnectionString();
		SetUpGetDatabaseName();
	}

	private void SetUpGetConnectionString()
	{
		connectionString = Faker.RandomString();

		A.CallTo(() => environmentRepository.Get("MongoConnectionString")).Returns(connectionString);
	}

	private void SetUpGetDatabaseName()
	{
		dataBaseName = Faker.RandomString();

		A.CallTo(() => environmentRepository.Get("MongoDatabaseName")).Returns(dataBaseName);
	}
}
