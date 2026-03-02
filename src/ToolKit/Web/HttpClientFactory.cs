using FatCat.Toolkit.Console;

namespace FatCat.Toolkit.Web;

public static class HttpClientFactory
{
	private static HttpClient client;
	private static HttpClientHandler clientHandler;

	public static TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(15);

	public static HttpClient Get()
	{
		client ??= new HttpClient() { Timeout = DefaultTimeout };

		return client;
	}

	public static void UseHttpClientHandler(HttpClientHandler handler = null)
	{
		clientHandler = handler ?? new HttpClientHandler();

		if (handler is null)
		{
			clientHandler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
		}

		client = new HttpClient(clientHandler) { Timeout = DefaultTimeout };
	}
}
