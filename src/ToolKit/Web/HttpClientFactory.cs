namespace FatCat.Toolkit.Web;

public static class HttpClientFactory
{
	private static HttpClient client = new() { Timeout = Timeout.InfiniteTimeSpan };
	private static HttpClientHandler clientHandler;

	public static HttpClient Get()
	{
		return client;
	}

	public static void UseHttpClientHandler(HttpClientHandler handler = null)
	{
		clientHandler = handler ?? new HttpClientHandler();

		if (handler is null)
		{
			clientHandler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
		}

		client = new HttpClient(clientHandler) { Timeout = Timeout.InfiniteTimeSpan };
	}
}
