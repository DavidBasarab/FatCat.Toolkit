using System.Text.Json;
using System.Text.Json.Serialization;

namespace FatCat.Toolkit.Json;

public interface IJsonOperations
{
	T Deserialize<T>(string json, JsonSerializerOptions options);

	T Deserialize<T>(string json);

	T DeserializeFromStream<T>(Stream input, JsonSerializerOptions options = null, bool leaveOpen = false);

	Task<T> DeserializeFromStreamAsync<T>(
		Stream input,
		JsonSerializerOptions options = null,
		bool leaveOpen = false,
		CancellationToken cancellationToken = default
	);

	JsonSerializerOptions GetDefaultOptions(bool indented = false);

	string Serialize(object source);

	string Serialize(object source, JsonSerializerOptions options);

	string Serialize(object source, bool indented);

	void SerializeToStream(
		object source,
		Stream output,
		JsonSerializerOptions options = null,
		bool leaveOpen = false
	);

	Task SerializeToStreamAsync(
		object source,
		Stream output,
		JsonSerializerOptions options = null,
		bool leaveOpen = false,
		CancellationToken cancellationToken = default
	);

	bool TryDeserialize<T>(string json, out T value, JsonSerializerOptions options = null);
}

public sealed class JsonOperations : IJsonOperations
{
	public T Deserialize<T>(string json, JsonSerializerOptions options)
	{
		return JsonSerializer.Deserialize<T>(json, options ?? GetDefaultOptions());
	}

	public T Deserialize<T>(string json)
	{
		return Deserialize<T>(json, null);
	}

	public T DeserializeFromStream<T>(Stream input, JsonSerializerOptions options = null, bool leaveOpen = false)
	{
		// Sync read – buffer into memory
		using (var ms = new MemoryStream())
		{
			input.CopyTo(ms);

			if (!leaveOpen)
			{
				input.Dispose();
			}

			return JsonSerializer.Deserialize<T>(ms.ToArray(), options ?? GetDefaultOptions());
		}
	}

	public async Task<T> DeserializeFromStreamAsync<T>(
		Stream input,
		JsonSerializerOptions options = null,
		bool leaveOpen = false,
		CancellationToken cancellationToken = default
	)
	{
		var result = await JsonSerializer.DeserializeAsync<T>(
			input,
			options ?? GetDefaultOptions(),
			cancellationToken
		);

		if (!leaveOpen)
		{
			await input.DisposeAsync();
		}

		return result;
	}

	public JsonSerializerOptions GetDefaultOptions(bool indented = false)
	{
		var opts = new JsonSerializerOptions
		{
			WriteIndented = indented,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			PropertyNameCaseInsensitive = true,
		};

		opts.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
		opts.Converters.Add(new ObjectIdJsonConverter());

		return opts;
	}

	public string Serialize(object source, JsonSerializerOptions options)
	{
		return JsonSerializer.Serialize(source, options ?? GetDefaultOptions());
	}

	public string Serialize(object source)
	{
		return Serialize(source, GetDefaultOptions());
	}

	public string Serialize(object source, bool indented)
	{
		return Serialize(source, GetDefaultOptions(indented));
	}

	public void SerializeToStream(
		object source,
		Stream output,
		JsonSerializerOptions options = null,
		bool leaveOpen = false
	)
	{
		using (
			var writer = new Utf8JsonWriter(
				output,
				new JsonWriterOptions { Indented = (options ?? GetDefaultOptions()).WriteIndented }
			)
		)
		{
			JsonSerializer.Serialize(writer, source, options ?? GetDefaultOptions());
			writer.Flush();
		}

		if (!leaveOpen)
		{
			output.Flush();
		}
	}

	public async Task SerializeToStreamAsync(
		object source,
		Stream output,
		JsonSerializerOptions options = null,
		bool leaveOpen = false,
		CancellationToken cancellationToken = default
	)
	{
		await JsonSerializer.SerializeAsync(output, source, options ?? GetDefaultOptions(), cancellationToken);

		if (!leaveOpen)
		{
			await output.FlushAsync(cancellationToken);
		}
	}

	public bool TryDeserialize<T>(string json, out T value, JsonSerializerOptions options = null)
	{
		try
		{
			value = Deserialize<T>(json, options);
			return true;
		}
		catch
		{
			value = default;
			return false;
		}
	}
}
