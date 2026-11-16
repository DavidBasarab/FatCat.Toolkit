using System.Net;
using System.Net.Http.Headers;
using System.Text;
using FatCat.Toolkit.Extensions;
using FatCat.Toolkit.Json;
using FatCat.Toolkit.Logging;
using Humanizer;

namespace FatCat.Toolkit.Web;

public interface IWebCaller
{
	public string Accept { get; set; }

	public Uri BaseUri { get; }

	public TimeSpan Timeout { get; set; }

	public void AddHeader(string name, string value);

	public Task<FatWebResponse> Delete(string url);

	public Task<FatWebResponse> Delete(string url, TimeSpan timeout);

	public Task<FatWebResponse> Delete(string url, TimeSpan timeout, CancellationToken cancellationToken);

	public Task<FatWebResponse> Get(string url);

	public Task<FatWebResponse> Get(string url, TimeSpan timeout);

	public Task<FatWebResponse> Get(string url, TimeSpan timeout, CancellationToken cancellationToken);

	public Task<FatWebResponse> Post<T>(string url, T data);

	public Task<FatWebResponse> Post<T>(string url, List<T> data);

	public Task<FatWebResponse> Post(string url);

	public Task<FatWebResponse> Post(string url, string data);

	public Task<FatWebResponse> Post(string url, string data, string contentType);

	public Task<FatWebResponse> Post<T>(string url, T data, TimeSpan timeout);

	public Task<FatWebResponse> Post<T>(string url, List<T> data, TimeSpan timeout);

	public Task<FatWebResponse> Post(string url, TimeSpan timeout);

	public Task<FatWebResponse> Post(string url, string data, TimeSpan timeout, string contentType);

	public Task<FatWebResponse> Post<T>(string url, T data, TimeSpan timeout, CancellationToken cancellationToken);

	public Task<FatWebResponse> Post<T>(string url, List<T> data, TimeSpan timeout, CancellationToken cancellationToken);

	public Task<FatWebResponse> Post(string url, TimeSpan timeout, CancellationToken cancellationToken);

	public Task<FatWebResponse> Post(
		string url,
		string data,
		TimeSpan timeout,
		string contentType,
		CancellationToken cancellationToken
	);

	public Task<FatWebResponse> Put<T>(string url, T data);

	public Task<FatWebResponse> Put<T>(string url, List<T> data);

	public Task<FatWebResponse> Put(string url);

	public Task<FatWebResponse> Put(string url, string data);

	public Task<FatWebResponse> Put(string url, string data, string contentType);

	public Task<FatWebResponse> Put<T>(string url, T data, TimeSpan timeout);

	public Task<FatWebResponse> Put<T>(string url, List<T> data, TimeSpan timeout);

	public Task<FatWebResponse> Put(string url, TimeSpan timeout);

	public Task<FatWebResponse> Put(string url, string data, TimeSpan timeout, string contentType);

	public Task<FatWebResponse> Put<T>(string url, T data, TimeSpan timeout, CancellationToken cancellationToken);

	public Task<FatWebResponse> Put<T>(string url, List<T> data, TimeSpan timeout, CancellationToken cancellationToken);

	public Task<FatWebResponse> Put(string url, TimeSpan timeout, CancellationToken cancellationToken);

	public Task<FatWebResponse> Put(
		string url,
		string data,
		TimeSpan timeout,
		string contentType,
		CancellationToken cancellationToken
	);

	public void SetClient(HttpClient client);

	public void UseBasicAuthorization(string username, string password);

	public void UserBearerToken(string token);
}

public class WebCaller(Uri uri, IJsonOperations jsonOperations, IToolkitLogger logger) : IWebCaller
{
	private readonly Dictionary<string, string> headers = [];

	private string basicPassword;
	private string basicUsername;
	private string bearerToken;

	private HttpClient localClient;

	public string Accept { get; set; }

	public Uri BaseUri { get; } = uri;

	public TimeSpan Timeout { get; set; } = 30.Seconds();

	public void AddHeader(string name, string value)
	{
		headers[name] = value;
	}

	public void ClearAuthorization()
	{
		bearerToken = null;
		basicUsername = null;
		basicPassword = null;
	}

	public async Task<FatWebResponse> Delete(string url)
	{
		return await Delete(url, Timeout);
	}

	public async Task<FatWebResponse> Delete(string url, TimeSpan timeout)
	{
		return await Delete(url, timeout, CancellationToken.None);
	}

	public async Task<FatWebResponse> Delete(string url, TimeSpan timeout, CancellationToken cancellationToken)
	{
		return await SendWebRequest(HttpMethod.Delete, url, timeout, cancellationToken);
	}

	public Task<FatWebResponse> Get(string url)
	{
		return Get(url, Timeout);
	}

	public async Task<FatWebResponse> Get(string url, TimeSpan timeout)
	{
		return await Get(url, timeout, CancellationToken.None);
	}

	public async Task<FatWebResponse> Get(string url, TimeSpan timeout, CancellationToken cancellationToken)
	{
		return await SendWebRequest(HttpMethod.Get, url, timeout, cancellationToken);
	}

	public Uri GetFullUrl(string url)
	{
		var baseUrl = BaseUri.ToString();

		if (baseUrl.EndsWith('/'))
		{
			baseUrl = baseUrl.Remove(baseUrl.Length - 1, 1);
		}

		return new Uri($"{baseUrl}/{url}");
	}

	public Task<FatWebResponse> Post<T>(string url, T data)
	{
		return Post(url, data, Timeout);
	}

	public Task<FatWebResponse> Post<T>(string url, List<T> data)
	{
		return Post(url, data, Timeout);
	}

	public Task<FatWebResponse> Post(string url)
	{
		return Post(url, Timeout);
	}

	public Task<FatWebResponse> Post(string url, string data)
	{
		return Post(url, data, Timeout);
	}

	public async Task<FatWebResponse> Post(string url, string data, string contentType)
	{
		return await Post(url, data, Timeout, contentType);
	}

	public async Task<FatWebResponse> Post<T>(string url, T data, TimeSpan timeout)
	{
		return await Post(url, data, timeout, CancellationToken.None);
	}

	public async Task<FatWebResponse> Post<T>(string url, List<T> data, TimeSpan timeout)
	{
		return await Post(url, data, timeout, CancellationToken.None);
	}

	public async Task<FatWebResponse> Post(string url, TimeSpan timeout)
	{
		return await Post(url, timeout, CancellationToken.None);
	}

	public async Task<FatWebResponse> Post(string url, string data, TimeSpan timeout, string contentType)
	{
		return await Post(url, data, timeout, contentType, CancellationToken.None);
	}

	public async Task<FatWebResponse> Post(string url, string data, TimeSpan timeout)
	{
		return await SendWebRequest(HttpMethod.Post, url, timeout, CancellationToken.None, data);
	}

	public async Task<FatWebResponse> Post<T>(string url, T data, TimeSpan timeout, CancellationToken cancellationToken)
	{
		var json = jsonOperations.Serialize(data);

		return await SendWebRequest(HttpMethod.Post, url, timeout, cancellationToken, json, "application/json");
	}

	public async Task<FatWebResponse> Post<T>(string url, List<T> data, TimeSpan timeout, CancellationToken cancellationToken)
	{
		var json = jsonOperations.Serialize(data);

		return await SendWebRequest(HttpMethod.Post, url, timeout, cancellationToken, json, "application/json");
	}

	public async Task<FatWebResponse> Post(string url, TimeSpan timeout, CancellationToken cancellationToken)
	{
		return await SendWebRequest(HttpMethod.Post, url, timeout, cancellationToken);
	}

	public async Task<FatWebResponse> Post(
		string url,
		string data,
		TimeSpan timeout,
		string contentType,
		CancellationToken cancellationToken
	)
	{
		return await SendWebRequest(HttpMethod.Post, url, timeout, cancellationToken, data, contentType);
	}

	public Task<FatWebResponse> Put<T>(string url, T data)
	{
		var json = jsonOperations.Serialize(data);

		return SendWebRequest(HttpMethod.Put, url, Timeout, CancellationToken.None, json);
	}

	public Task<FatWebResponse> Put<T>(string url, List<T> data)
	{
		var json = jsonOperations.Serialize(data);

		return SendWebRequest(HttpMethod.Put, url, Timeout, CancellationToken.None, json);
	}

	public Task<FatWebResponse> Put(string url)
	{
		return SendWebRequest(HttpMethod.Put, url, Timeout, CancellationToken.None);
	}

	public Task<FatWebResponse> Put(string url, string data)
	{
		return SendWebRequest(HttpMethod.Put, url, Timeout, CancellationToken.None, data);
	}

	public Task<FatWebResponse> Put(string url, string data, string contentType)
	{
		return SendWebRequest(HttpMethod.Put, url, Timeout, CancellationToken.None, data, contentType);
	}

	public Task<FatWebResponse> Put<T>(string url, T data, TimeSpan timeout)
	{
		return Put(url, data, timeout, CancellationToken.None);
	}

	public Task<FatWebResponse> Put<T>(string url, List<T> data, TimeSpan timeout)
	{
		return Put(url, data, timeout, CancellationToken.None);
	}

	public Task<FatWebResponse> Put(string url, TimeSpan timeout)
	{
		return Put(url, timeout, CancellationToken.None);
	}

	public Task<FatWebResponse> Put(string url, string data, TimeSpan timeout, string contentType)
	{
		return Put(url, data, timeout, contentType, CancellationToken.None);
	}

	public Task<FatWebResponse> Put<T>(string url, T data, TimeSpan timeout, CancellationToken cancellationToken)
	{
		var json = jsonOperations.Serialize(data);

		return SendWebRequest(HttpMethod.Put, url, timeout, cancellationToken, json);
	}

	public Task<FatWebResponse> Put<T>(string url, List<T> data, TimeSpan timeout, CancellationToken cancellationToken)
	{
		var json = jsonOperations.Serialize(data);

		return SendWebRequest(HttpMethod.Put, url, timeout, cancellationToken, json);
	}

	public Task<FatWebResponse> Put(string url, TimeSpan timeout, CancellationToken cancellationToken)
	{
		return SendWebRequest(HttpMethod.Put, url, timeout, cancellationToken);
	}

	public Task<FatWebResponse> Put(
		string url,
		string data,
		TimeSpan timeout,
		string contentType,
		CancellationToken cancellationToken
	)
	{
		return SendWebRequest(HttpMethod.Put, url, timeout, cancellationToken, data, contentType);
	}

	public void SetClient(HttpClient client)
	{
		localClient = client;
	}

	public void UseBasicAuthorization(string username, string password)
	{
		basicUsername = username;
		basicPassword = password;
	}

	public void UserBearerToken(string token)
	{
		bearerToken = token;
	}

	private void EnsureAccept(HttpRequestMessage request)
	{
		if (Accept is not null)
		{
			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(Accept));
		}
		else
		{
			request.Headers.Accept.Clear();
		}
	}

	private void EnsureHeaders(HttpRequestMessage request)
	{
		if (headers.Count == 0)
		{
			return;
		}

		logger.Debug($"Adding <{headers.Count}> request headers");

		foreach (var header in headers)
		{
			request.Headers.TryAddWithoutValidation(header.Key, header.Value);
		}
	}

	private void EnsureAuthorization(HttpRequestMessage request)
	{
		if (bearerToken is not null)
		{
			logger.Debug($"Adding Bearer Token := <{bearerToken}>");

			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
		}
		else if (basicUsername is not null && basicPassword is not null)
		{
			logger.Debug("Adding Basic Authorization");

			var encodedUsernameAndPassword = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{basicUsername}:{basicPassword}"));

			request.Headers.Authorization = new AuthenticationHeaderValue("Basic", encodedUsernameAndPassword);
		}
		else
		{
			request.Headers.Authorization = null;
		}
	}

	private async Task<FatWebResponse> SendWebRequest(
		HttpMethod httpMethod,
		string url,
		TimeSpan timeout,
		CancellationToken cancellationToken,
		string data = null,
		string contentType = null
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		logger.Debug("Creating http client");

		var httpClient = localClient ?? HttpClientFactory.Get();

		var requestUri = GetFullUrl(url);

		logger.Debug($"Creating request message to Uri := <{requestUri}>");

		var requestMessage = new HttpRequestMessage(httpMethod, requestUri);

		EnsureAccept(requestMessage);
		EnsureAuthorization(requestMessage);
		EnsureHeaders(requestMessage);

		if (data.IsNotNullOrEmpty())
		{
			logger.Debug($"Adding data of length := <{data.Length}> | Content Type := <{contentType}>");
			logger.Debug(data);

			requestMessage.Content = new StringContent(data, Encoding.UTF8, contentType);
		}

		logger.Debug($"Timeout is := <{timeout}>");

		using var timeoutSource = new CancellationTokenSource(timeout);
		using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token, cancellationToken);

		try
		{
			logger.Debug($"Sending request to <{requestUri}>");

			var response = await httpClient.SendAsync(requestMessage, linkedSource.Token);

			logger.Debug("Creating web result from response");

			var result = new FatWebResponse(response);

			logger.Debug($"Request to <{requestUri}> | StatusCode := <{result.StatusCode}>");

			bearerToken = null;

			return result;
		}
		catch (HttpRequestException ex)
		{
			if (ex.Message.Contains("no host known", StringComparison.OrdinalIgnoreCase))
			{
				logger.Debug(ex.Message);

				return new FatWebResponse(HttpStatusCode.NotFound);
			}

			throw;
		}
		catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
		{
			logger.Debug($"Request to {requestUri} was cancelled by the caller");

			throw new OperationCanceledException(exception.Message, exception, cancellationToken);
		}
		catch (TaskCanceledException)
		{
			logger.Debug($"Request to {requestUri} timed out");

			return FatWebResponse.Timeout();
		}
	}
}
