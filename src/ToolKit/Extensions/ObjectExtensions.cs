using System.Text.Json;

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

		var bytes = JsonSerializer.SerializeToUtf8Bytes(objectToCopy);

		return JsonSerializer.Deserialize<T>(bytes);
	}
}
