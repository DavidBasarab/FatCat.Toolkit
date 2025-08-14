using System.Text;
using FatCat.Toolkit.Data.Mongo;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace FatCat.Toolkit.Json;

public interface IJsonOperations
{
	T Deserialize<T>(string json);

	T Deserialize<T>(string json, JsonSerializer serializer);

	T Deserialize<T>(string json, IContractResolver contractResolver);

	T Deserialize<T>(string json, TypeNameHandling typeNameHandling);

	T Deserialize<T>(string json, params JsonConverter[] converters);

	JsonSerializer GetDefaultSerializer();

	string Serialize(object source);

	string Serialize(object source, JsonSerializer serializer);

	string Serialize(object source, Formatting formatting);

	string Serialize(object source, Formatting formatting, TypeNameHandling typeNameHandling);

	string Serialize(object source, Formatting formatting, bool includeNullProperties);
}

public class JsonOperations : IJsonOperations
{
	public T Deserialize<T>(string json)
	{
		return DoDeserialize<T>(json, GetDefaultSerializer());
	}

	public T Deserialize<T>(string json, JsonSerializer serializer)
	{
		return DoDeserialize<T>(json, serializer);
	}

	public T Deserialize<T>(string json, IContractResolver contractResolver)
	{
		var serializer = GetDefaultSerializer();

		serializer.ContractResolver = contractResolver;

		return DoDeserialize<T>(json, serializer);
	}

	public T Deserialize<T>(string json, TypeNameHandling typeNameHandling)
	{
		var serializer = GetDefaultSerializer();

		serializer.TypeNameHandling = typeNameHandling;

		return DoDeserialize<T>(json, serializer);
	}

	public T Deserialize<T>(string json, params JsonConverter[] converters)
	{
		var serializer = new JsonSerializer
		{
			Converters = { new StringEnumConverter() },
			TypeNameHandling = TypeNameHandling.Auto,
			NullValueHandling = NullValueHandling.Ignore,
		};

		foreach (var jsonConverter in converters)
		{
			serializer.Converters.Add(jsonConverter);
		}

		return DoDeserialize<T>(json, serializer);
	}

	public JsonSerializer GetDefaultSerializer()
	{
		return new JsonSerializer
		{
			Converters = { new StringEnumConverter(), new ObjectIdConverter() },
			TypeNameHandling = TypeNameHandling.None,
			NullValueHandling = NullValueHandling.Ignore,
		};
	}

	public string Serialize(object source)
	{
		return DoSerialize(source, GetDefaultSerializer());
	}

	public string Serialize(object source, JsonSerializer serializer)
	{
		return DoSerialize(source, serializer);
	}

	public string Serialize(object source, Formatting formatting)
	{
		var serializer = GetDefaultSerializer();

		serializer.Formatting = formatting;

		return DoSerialize(source, serializer);
	}

	public string Serialize(object source, Formatting formatting, TypeNameHandling typeNameHandling)
	{
		var serializer = GetDefaultSerializer();

		serializer.Formatting = formatting;
		serializer.TypeNameHandling = typeNameHandling;

		return DoSerialize(source, serializer);
	}

	public string Serialize(object source, Formatting formatting, bool includeNullProperties)
	{
		var serializer = GetDefaultSerializer();

		serializer.Formatting = formatting;

		serializer.NullValueHandling = includeNullProperties
			? NullValueHandling.Include
			: NullValueHandling.Ignore;

		return DoSerialize(source, serializer);
	}

	private static T DoDeserialize<T>(string json, JsonSerializer serializer)
	{
		using var stringReader = new StringReader(json);
		using var jsonReader = new JsonTextReader(stringReader);

		return serializer.Deserialize<T>(jsonReader);
	}

	private static string DoSerialize(object source, JsonSerializer serializer)
	{
		using var stream = new MemoryStream();
		using var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true);
		using var jsonWriter = new JsonTextWriter(writer);

		serializer.Serialize(jsonWriter, source);
		jsonWriter.Flush();
		writer.Flush();

		stream.Position = 0;
		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}
}
