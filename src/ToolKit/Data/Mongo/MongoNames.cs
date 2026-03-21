namespace FatCat.Toolkit.Data.Mongo;

public interface IMongoNames : IDataNames
{
	public string GetDatabaseName<T>()
		where T : MongoObject;

	public string GetDatabaseNameFromType(Type type);
}

public class MongoNames(IDataNames dataNames) : IMongoNames
{
	public string GetCollectionName<T>()
		where T : DataObject
	{
		return dataNames.GetCollectionName<T>();
	}

	public string GetCollectionNameFromType(Type type)
	{
		return dataNames.GetCollectionNameFromType(type);
	}

	public string GetDatabaseName<T>()
		where T : MongoObject
	{
		return GetDatabaseNameFromType(typeof(T));
	}

	public string GetDatabaseNameFromType(Type type)
	{
		var typeNamespace = type.Namespace;

		var namespaceParts = typeNamespace!.Split('.');

		return $"{namespaceParts[0]}{(namespaceParts.Length > 1 ? namespaceParts[1] : string.Empty)}";
	}
}
