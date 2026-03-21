using System.Text;
using Microsoft.AspNetCore.Http;

namespace FatCat.Toolkit.WebServer;

public static class HttpRequestExtensions
{
	public static async Task<string> ReadContent(this HttpRequest request)
	{
		using var reader = new StreamReader(request.Body, Encoding.UTF8);

		return await reader.ReadToEndAsync();
	}
}
