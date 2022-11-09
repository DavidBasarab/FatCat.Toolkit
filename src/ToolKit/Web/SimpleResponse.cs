#nullable enable
using System.Net;

namespace FatCat.Toolkit.Web;

public class SimpleResponse : EqualObject
{
	public HttpContent? Content { get; set; }

	public string? ContentType { get; set; }

	public Exception? Exception { get; set; }

	public Dictionary<string, IEnumerable<string>> Headers { get; set; } = new();

	public HttpStatusCode HttpStatusCode => (HttpStatusCode)StatusCode;

	public bool IsSuccessful => StatusCode >= 200 && StatusCode < 400;

	public int StatusCode { get; set; }

	public string? Text { get; set; }

	public WebResult ToResult() => new()
									{
										Content = Text,
										ContentType = ContentType!,
										StatusCode = HttpStatusCode
									};
}