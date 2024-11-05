#nullable enable
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FatCat.Toolkit.Web;

public interface IJsonConvert
{
	T? DeserializeObjectWithTypeHandling<T>(string json);

	T? DeserializeObjectWithTypeHandling<T>(string json, params Newtonsoft.Json.JsonConverter[] converters);

	string SerializeObject(object value);
}

public class JsonConverter : IJsonConvert
{
	private static readonly JsonSerializerSettings jsonSettingsReturnNulls = JsonSerializerSettingsFactory(
		NullValueHandling.Include
	);

	private static readonly JsonSerializerSettings jsonSettingsDoNotReturnNulls = JsonSerializerSettingsFactory(
		NullValueHandling.Ignore
	);

	public T? DeserializeObjectWithTypeHandling<T>(string json)
	{
		return DeserializeObjectWithTypeHandling<T>(json, Array.Empty<Newtonsoft.Json.JsonConverter>());
	}

	public T? DeserializeObjectWithTypeHandling<T>(string json, params Newtonsoft.Json.JsonConverter[] converters)
	{
		var settingsWithTypeHandling = jsonSettingsReturnNulls;

		foreach (var converter in converters)
		{
			jsonSettingsReturnNulls.Converters.Add(converter);
		}

		settingsWithTypeHandling.TypeNameHandling = TypeNameHandling.Auto;

		return JsonConvert.DeserializeObject<T>(json, settingsWithTypeHandling);
	}

	public string SerializeObject(object value)
	{
		return SerializeObject(value, Formatting.None, false);
	}

	public string SerializeObject(object value, Formatting formatting, bool includeNullProperties)
	{
		return JsonConvert.SerializeObject(
			value,
			formatting,
			includeNullProperties ? jsonSettingsReturnNulls : jsonSettingsDoNotReturnNulls
		);
	}

	private static JsonSerializerSettings JsonSerializerSettingsFactory(NullValueHandling nullValueHandling)
	{
		return new JsonSerializerSettings
		{
			Converters = new List<Newtonsoft.Json.JsonConverter> { new StringEnumConverter() },
			NullValueHandling = nullValueHandling,
		};
	}
}
