using System.Text.Json.Serialization;

namespace Tests.FatCat.Toolkit.Web.Api.WebCallerSpecs;

public class HttpBinDataJsonResponse<T> : HttpBinResponse
{
	[JsonPropertyName("json")]
	public T Json { get; set; }
}
