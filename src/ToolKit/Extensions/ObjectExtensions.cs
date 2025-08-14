using FatCat.Toolkit.Json;

namespace FatCat.Toolkit.Extensions;

public static class ObjectExtensions
{
	public static T DeepCopy<T>(this T objectToCopy)
		where T : class
	{
		if (objectToCopy == null)
		{
			return null;
		}

		var jsonOperations = new JsonOperations();

		var json = jsonOperations.Serialize(objectToCopy);

		return jsonOperations.Deserialize<T>(json);
	}
}
