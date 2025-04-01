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

	protected HttpBinResponse response;

	protected WebCaller webCaller =
		new(new Uri("https://httpbin.org"), new JsonOperations(), A.Fake<IToolkitLogger>()) { Accept = null };

	protected abstract string BasicPath { get; }

	[Fact]
	public async Task BasicCall()
	{
		await MakeCall(BasicPath);

		response.QueryParameters.Should().BeEmpty();
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
	public async Task DoACallWithAnExtraSlashOnUrl()
	{
		webCaller = new WebCaller(new Uri("https://httpbin.org/"), new JsonOperations(), A.Fake<IToolkitLogger>());

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

	protected void VerifyBasicQueryStrings()
	{
		response.QueryParameters["first"].Should().Be("12");
		response.QueryParameters["second"].Should().Be("13");
	}

	protected void VerifyBearerToken()
	{
		response.AuthorizationHeader.Should().Be($"Bearer {BearerToken}");
	}

	protected void VerifyStatusListQueryParameters()
	{
		var statusList = response.GetQueryParameterAsList("status");

		statusList.Count.Should().Be(3);

		statusList.Should().BeEquivalentTo(new List<string> { "1", "2", "3" });
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
