#nullable enable
using FatCat.Toolkit.Json;
using Newtonsoft.Json;

namespace FatCat.Toolkit.Extensions;

public static class ObjectExtensions
{
	public static T? DeepCopy<T>(this T? objectToCopy)
		where T : class
	{
		if (objectToCopy == null)
		{
			return null;
		}

		var json = JsonConvert.SerializeObject(objectToCopy, JsonOperations.DefaultSettings);

		return JsonConvert.DeserializeObject<T>(json, JsonOperations.DefaultSettings);
	}
}
