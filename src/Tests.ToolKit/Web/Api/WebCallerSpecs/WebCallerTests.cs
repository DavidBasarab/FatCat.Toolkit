using System.Net;
using System.Text;
using FatCat.Toolkit.Json;
using FatCat.Toolkit.Logging;
using FatCat.Toolkit.Web;
using Humanizer;

namespace Tests.FatCat.Toolkit.Web.Api.WebCallerSpecs;

public abstract class WebCallerTests
{
	private const string BasicPassword = "This is a password";

	private const string BasicUserName = "This is a UserName";
	private const string BearerToken = "12345890";

	protected readonly CancellationTokenSource cancellationSource = new();
	protected readonly FakeHttpBin httpBin = new();

	protected HttpBinResponse response;

	protected WebCaller webCaller;

	protected abstract string BasicPath { get; }

	protected WebCallerTests()
	{
		webCaller = CreateWebCaller("https://httpbin.org");
	}

	[Fact]
	public async Task CarryTheCallerTokenWhenTheCallerCancels()
	{
		var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(CallThenCancelOnceInFlight);

		VerifyCarriesTheCallerToken(exception);
	}

	[Fact]
	public async Task CarryTheCallerTokenWhenTheTokenIsAlreadyCancelled()
	{
		await cancellationSource.CancelAsync();

		var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(CallWithTheCallerToken);

		VerifyCarriesTheCallerToken(exception);
	}

	[Fact]
	public async Task NotSendTheRequestWhenTheTokenIsAlreadyCancelled()
	{
		await cancellationSource.CancelAsync();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(CallWithTheCallerToken);

		httpBin.RequestCount.Should().Be(0);
	}

	[Fact]
	public async Task ReturnTimeoutWhenTheTimeoutPassesWithAnUncancelledToken()
	{
		var result = await MakeCallToWeb("delay/3", 200.Milliseconds(), cancellationSource.Token);

		result.StatusCode.Should().Be(HttpStatusCode.RequestTimeout);
	}

	[Fact]
	public async Task SucceedWithAnUncancelledToken()
	{
		var result = await CallWithTheCallerToken();

		result.IsSuccessful.Should().BeTrue();
	}

	[Fact]
	public async Task ThrowWhenTheCallerCancelsMidFlight()
	{
		Func<Task> callAction = CallThenCancelOnceInFlight;

		await Assert.ThrowsAnyAsync<OperationCanceledException>(callAction);
	}

	[Fact]
	public async Task BasicCall()
	{
		await MakeCall(BasicPath);

		response.QueryParameters.Count.Should().Be(0);
	}

	[Fact]
	public async Task CallWithArrayInQueryParameters()
	{
		await MakeCall($"{BasicPath}?first=12&second=13&status=1&status=2&status=3");

		response.QueryParameters.Count.Should().Be(3);

		VerifyBasicQueryStrings();
		VerifyStatusListQueryParameters();
	}

	[Fact]
	public async Task CallWithQueryParameters()
	{
		await MakeCall($"{BasicPath}?first=12&second=13");

		response.QueryParameters.Count.Should().Be(2);

		VerifyBasicQueryStrings();
	}

	[Fact]
	public async Task CanClearBasicAuthorization()
	{
		UserBasicAuthorization();

		webCaller.ClearAuthorization();

		await MakeCall(BasicPath);

		response.AuthorizationHeader.Should().BeNullOrEmpty();
	}

	[Fact]
	public async Task CanClearBearerToken()
	{
		UserBearerToken();

		webCaller.ClearAuthorization();

		await MakeCall(BasicPath);

		response.AuthorizationHeader.Should().BeNullOrEmpty();
	}

	[Fact]
	public async Task CanMakeCallWithoutSlashInFrontOfPath()
	{
		var path = BasicPath;

		if (path.StartsWith("/"))
		{
			path = path.Substring(1);
		}

		await MakeCall(path);
	}

	[Fact]
	public async Task CanSendACustomHeader()
	{
		webCaller.AddHeader("x-custom-header", "the-value");

		await MakeCall(BasicPath);

		response.Headers.GetValueOrDefault("X-Custom-Header").Should().Be("the-value");
	}

	[Fact]
	public async Task CanSendMultipleCustomHeaders()
	{
		webCaller.AddHeader("x-api-key", "the-api-key");
		webCaller.AddHeader("anthropic-version", "2023-06-01");

		await MakeCall(BasicPath);

		response.Headers.GetValueOrDefault("X-Api-Key").Should().Be("the-api-key");
		response.Headers.GetValueOrDefault("Anthropic-Version").Should().Be("2023-06-01");
	}

	[Fact]
	public async Task CanTimeout()
	{
		webCaller.Timeout = 1.Seconds();

		var result = await MakeCallToWeb("delay/3");

		result.IsUnsuccessful.Should().BeTrue();

		result.StatusCode.Should().Be(HttpStatusCode.RequestTimeout);
	}

	[Fact]
	public async Task CanUseBasicAuthorization()
	{
		UserBasicAuthorization();

		await MakeCall(BasicPath);

		VerifyBasicAuthorization();
	}

	[Fact]
	public async Task CanUseBearerToken()
	{
		UserBearerToken();

		await MakeCall(BasicPath);

		VerifyBearerToken();
	}

	[Fact]
	public async Task CustomHeaderCoexistsWithBearerToken()
	{
		UserBearerToken();

		webCaller.AddHeader("x-custom-header", "the-value");

		await MakeCall(BasicPath);

		VerifyBearerToken();

		response.Headers.GetValueOrDefault("X-Custom-Header").Should().Be("the-value");
	}

	[Fact]
	public async Task DoACallWithAnExtraSlashOnUrl()
	{
		webCaller = CreateWebCaller("https://httpbin.org/");

		await MakeCall(BasicPath);
	}

	[Fact]
	public async Task ReturnStatusCodeOfNotFound()
	{
		var result = await MakeCallToWeb("status/404");

		result.IsUnsuccessful.Should().BeTrue();

		result.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	protected abstract Task<FatWebResponse> MakeCallToWeb(string path);

	protected abstract Task<FatWebResponse> MakeCallToWeb(
		string path,
		TimeSpan timeout,
		CancellationToken cancellationToken
	);

	protected void VerifyBasicQueryStrings()
	{
		response.QueryParameters["first"].GetString().Should().Be("12");
		response.QueryParameters["second"].GetString().Should().Be("13");
	}

	protected void VerifyBearerToken()
	{
		response.AuthorizationHeader.Should().Be($"Bearer {BearerToken}");
	}

	protected void VerifyCarriesTheCallerToken(OperationCanceledException exception)
	{
		(exception.CancellationToken == cancellationSource.Token).Should().BeTrue();
	}

	protected void VerifyStatusListQueryParameters()
	{
		var statusList = response.GetQueryParameterAsList("status");

		statusList.Count.Should().Be(3);

		statusList.Should().BeEquivalentTo(new List<string> { "1", "2", "3" });
	}

	private async Task<FatWebResponse> CallThenCancelOnceInFlight()
	{
		var call = MakeCallToWeb("delay/3", 1.Minutes(), cancellationSource.Token);

		await httpBin.DelayStarted;
		await cancellationSource.CancelAsync();

		return await call;
	}

	private Task<FatWebResponse> CallWithTheCallerToken()
	{
		return MakeCallToWeb(BasicPath, 1.Minutes(), cancellationSource.Token);
	}

	private WebCaller CreateWebCaller(string baseUrl)
	{
		var caller = new WebCaller(new Uri(baseUrl), new JsonOperations(), A.Fake<IToolkitLogger>());

		caller.SetClient(new HttpClient(httpBin));

		return caller;
	}

	private async Task MakeCall(string path)
	{
		var result = await MakeCallToWeb(path);

		result.IsSuccessful.Should().BeTrue();

		response = result.To<HttpBinResponse>();
	}

	private void UserBasicAuthorization()
	{
		webCaller.UseBasicAuthorization(BasicUserName, BasicPassword);
	}

	private void UserBearerToken()
	{
		webCaller.UserBearerToken(BearerToken);
	}

	private void VerifyBasicAuthorization()
	{
		var encodedUsernameAndPassword = Convert.ToBase64String(
			Encoding.UTF8.GetBytes($"{BasicUserName}:{BasicPassword}")
		);

		response.AuthorizationHeader.Should().Be($"Basic {encodedUsernameAndPassword}");
	}
}
