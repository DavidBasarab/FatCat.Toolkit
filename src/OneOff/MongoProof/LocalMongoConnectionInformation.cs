using FatCat.Toolkit.Data.Mongo;

namespace OneOff.MongoProof;

public class LocalMongoConnectionInformation : IMongoConnectionInformation
{
	public string GetConnectionString()
	{
		return "mongodb://localhost:27017";
	}

	public string GetDatabaseName()
	{
		return "ToolkitProof";
	}
}
