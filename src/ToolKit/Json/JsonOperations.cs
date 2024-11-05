using FatCat.Toolkit.Data.Mongo;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FatCat.Toolkit.Json;

public interface IJsonOperations
{
	T Deserialize<T>(string json);

	string Serialize(object dataObject);

	JsonSerializerSettings JsonSettings { get; set; }
}

public class JsonOperations : IJsonOperations
{
	public static JsonSerializerSettings DefaultSettings { get; } =
		new()
		{
			TypeNameHandling = TypeNameHandling.All,
			NullValueHandling = NullValueHandling.Ignore,
			Converters = new List<JsonConverter> { new StringEnumConverter(), new ObjectIdConverter() },
		};

	public JsonSerializerSettings JsonSettings { get; set; } = DefaultSettings;

	public T Deserialize<T>(string json)
	{
		return JsonConvert.DeserializeObject<T>(json, JsonSettings);
	}

	public string Serialize(object dataObject)
	{
		return JsonConvert.SerializeObject(dataObject, JsonSettings);
	}
}
