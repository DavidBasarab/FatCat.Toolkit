#nullable enable
using MongoDB.Driver;

namespace FatCat.Toolkit.Data.Mongo;

public interface IMongoDataConnection
{
	IMongoCollection<T> GetCollection<T>(string? connectionString = null, string? databaseName = null)
		where T : MongoObject;
}

public class MongoDataConnection : IMongoDataConnection
{
	private readonly IMongoConnection mongoConnection;
	private readonly IMongoNames mongoNames;

	public MongoDataConnection(IMongoNames mongoNames, IMongoConnection mongoConnection)
	{
		this.mongoNames = mongoNames;
		this.mongoConnection = mongoConnection;
	}

	public IMongoCollection<T> GetCollection<T>(string? connectionString = null, string? databaseName = null)
		where T : MongoObject
	{
		var database = GetDatabase<T>(connectionString, databaseName);

		return database.GetCollection<T>(mongoNames.GetCollectionName<T>());
	}

	private IMongoDatabase GetDatabase<T>(string? connectionString, string? databaseName)
		where T : MongoObject
	{
		var finalDatabaseName = databaseName ?? mongoNames.GetDatabaseName<T>();

		return mongoConnection.GetDatabase(finalDatabaseName, connectionString);
	}
}
