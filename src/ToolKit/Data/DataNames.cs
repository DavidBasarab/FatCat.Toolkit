using Humanizer;

namespace FatCat.Toolkit.Data;

public interface IDataNames
{
	string GetCollectionName<T>() where T : DataObject;

	string GetCollectionNameFromType(Type type);
}

public class DataNames : IDataNames
{
	public string GetCollectionName<T>() where T : DataObject => GetCollectionNameFromType(typeof(T));

	public string GetCollectionNameFromType(Type type) => type.Name.Pluralize();
}