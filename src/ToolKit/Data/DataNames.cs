using Humanizer;

namespace FatCat.Toolkit.Data;

public interface IDataNames
{
	public string GetCollectionName<T>()
		where T : DataObject;

	public string GetCollectionNameFromType(Type type);
}

public class DataNames : IDataNames
{
	public string GetCollectionName<T>()
		where T : DataObject
	{
		return GetCollectionNameFromType(typeof(T));
	}

	public string GetCollectionNameFromType(Type type)
	{
		return type.Name.Pluralize();
	}
}
