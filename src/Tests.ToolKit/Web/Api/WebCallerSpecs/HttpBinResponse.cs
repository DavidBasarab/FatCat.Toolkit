using System.Text.Json;
using System.Text.Json.Serialization;
using FatCat.Toolkit;

namespace Tests.FatCat.Toolkit.Web.Api.WebCallerSpecs;

public class HttpBinResponse : EqualObject
{
	public string AcceptHeader
	{
		get => Headers.GetValueOrDefault("Accept");
	}

	public string AuthorizationHeader
	{
		get => Headers.GetValueOrDefault("Authorization");
	}

	public string ContentTypeHeader
	{
		get => Headers.GetValueOrDefault("Content-Type");
	}

	[JsonPropertyName("headers")]
	public Dictionary<string, string> Headers { get; set; }

	[JsonPropertyName("origin")]
	public string Origin { get; set; }

	[JsonPropertyName("args")]
	public Dictionary<string, JsonElement> QueryParameters { get; set; }

	[JsonPropertyName("data")]
	public string RawData { get; set; }

	[JsonPropertyName("url")]
	public string Url { get; set; }

	public List<string> GetQueryParameterAsList(string name)
	{
		if (QueryParameters == null || !QueryParameters.TryGetValue(name, out var element))
		{
			return new List<string>();
		}

		if (element.ValueKind != JsonValueKind.Array)
		{
			return new List<string>();
		}

		return JsonSerializer.Deserialize<List<string>>(element.GetRawText());
	}
}
