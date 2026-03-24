namespace FatCat.Toolkit.Data.Mongo;

public class EnvironmentConnectionInformation(IEnvironmentRepository environmentRepository) : IMongoConnectionInformation
{
	public string GetConnectionString()
	{
		return environmentRepository.Get("MongoConnectionString");
	}

	public string GetDatabaseName()
	{
		return environmentRepository.Get("MongoDatabaseName");
	}
}
