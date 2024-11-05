using System.Net;

namespace FatCat.Toolkit.Web;

public class SimpleResponse : EqualObject
{
	public HttpContent Content { get; set; }

	public string ContentType { get; set; }

	public Exception Exception { get; set; }

	public Dictionary<string, IEnumerable<string>> Headers { get; set; } = new();

	public HttpStatusCode HttpStatusCode
	{
		get => (HttpStatusCode)StatusCode;
	}

	public bool IsSuccessful
	{
		get => StatusCode >= 200 && StatusCode < 400;
	}

	public int StatusCode { get; set; }

	public string Text { get; set; }

	public FatWebResponse ToResult()
	{
		return new FatWebResponse
		{
			Content = Text,
			ContentType = ContentType,
			StatusCode = HttpStatusCode,
		};
	}
}
