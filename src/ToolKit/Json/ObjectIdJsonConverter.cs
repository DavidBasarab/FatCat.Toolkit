using System.Text.Json;
using System.Text.Json.Serialization;
using MongoDB.Bson;

namespace FatCat.Toolkit.Json;

public sealed class ObjectIdJsonConverter : JsonConverter<ObjectId>
{
	public override ObjectId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.String)
		{
			return ObjectId.Parse(reader.GetString());
		}

		throw new JsonException("Expected string for ObjectId.");
	}

	public override void Write(Utf8JsonWriter writer, ObjectId value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.ToString());
	}
}
