namespace FatCat.Toolkit.Data.Mongo;

public interface IMongoConnectionInformation
{
	public string GetConnectionString();

	public string GetDatabaseName();
}
