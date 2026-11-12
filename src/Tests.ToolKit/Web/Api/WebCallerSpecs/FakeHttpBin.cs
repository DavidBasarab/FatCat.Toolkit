using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;

namespace Tests.FatCat.Toolkit.Web.Api.WebCallerSpecs;

public class FakeHttpBin : HttpMessageHandler
{
	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken
	)
	{
		var segments = request.RequestUri.AbsolutePath.Trim('/').Split('/');
		var endpoint = segments[0];

		return endpoint switch
		{
			"status" => new HttpResponseMessage((HttpStatusCode)int.Parse(segments[1])),
			"delay" => await WaitForCancellation(cancellationToken),
			"anything" => await Echo(request),
			_ => await EchoWhenMethodMatches(request, endpoint),
		};
	}

	private static async Task<HttpResponseMessage> Echo(HttpRequestMessage request)
	{
		var body = await ReadBody(request);

		var echo = new JsonObject
		{
			["args"] = ToArguments(request.RequestUri),
			["data"] = body,
			["headers"] = ToHeaders(request),
			["json"] = ParseJsonOrNull(body),
			["url"] = request.RequestUri.ToString(),
		};

		return new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(echo.ToJsonString(), Encoding.UTF8, "application/json"),
		};
	}

	private static async Task<HttpResponseMessage> EchoWhenMethodMatches(HttpRequestMessage request, string endpoint)
	{
		if (!string.Equals(request.Method.Method, endpoint, StringComparison.OrdinalIgnoreCase))
		{
			return new HttpResponseMessage(HttpStatusCode.MethodNotAllowed);
		}

		return await Echo(request);
	}

	private static JsonNode ParseJsonOrNull(string body)
	{
		try
		{
			return JsonNode.Parse(body);
		}
		catch (JsonException)
		{
			return null;
		}
	}

	private static async Task<string> ReadBody(HttpRequestMessage request)
	{
		if (request.Content is null)
		{
			return string.Empty;
		}

		return await request.Content.ReadAsStringAsync();
	}

	private static JsonNode ToArgumentValue(string[] values)
	{
		if (values.Length == 1)
		{
			return JsonValue.Create(values[0]);
		}

		return new JsonArray(values.Select(value => (JsonNode)JsonValue.Create(value)).ToArray());
	}

	private static JsonObject ToArguments(Uri uri)
	{
		var query = HttpUtility.ParseQueryString(uri.Query);

		return new JsonObject(
			query.AllKeys.Select(name => KeyValuePair.Create(name, ToArgumentValue(query.GetValues(name))))
		);
	}

	private static string ToHeaderCase(string headerName)
	{
		var words = headerName
			.Split('-')
			.Select(word => $"{char.ToUpperInvariant(word[0])}{word[1..].ToLowerInvariant()}");

		return string.Join('-', words);
	}

	private static JsonObject ToHeaders(HttpRequestMessage request)
	{
		IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = request.Content is null
			? request.Headers
			: request.Headers.Concat(request.Content.Headers);

		return new JsonObject(
			headers.Select(header =>
				KeyValuePair.Create(ToHeaderCase(header.Key), (JsonNode)JsonValue.Create(string.Join(", ", header.Value)))
			)
		);
	}

	private static async Task<HttpResponseMessage> WaitForCancellation(CancellationToken cancellationToken)
	{
		var cancelled = new TaskCompletionSource<HttpResponseMessage>();

		using var _ = cancellationToken.Register(() => cancelled.TrySetCanceled(cancellationToken));

		return await cancelled.Task;
	}
}
