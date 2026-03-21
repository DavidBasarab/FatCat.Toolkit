using FatCat.Toolkit.Json;
using FatCat.Toolkit.Logging;
using FatCat.Toolkit.Web;

namespace Tests.FatCat.Toolkit.Web.Api.WebCallerSpecs;

public class CallsWithMorePathInUri
{
	[Fact]
	public void CanGetFullUrl()
	{
		var webCaller = CreateWebCaller("https://httpbin.org/anything");

		var endingPath = "StarTrek";

		var expectedFullUrl = GetExpectedUrl(endingPath);

		webCaller.GetFullUrl(endingPath).Should().Be(expectedFullUrl);
	}

	[Theory]
	[InlineData("https://httpbin.org/anything")]
	[InlineData("https://httpbin.org/anything/")]
	public async Task CanMakeAGetToALongerPath(string baseUrl)
	{
		var webCaller = CreateWebCaller(baseUrl);

		var endingPath = "GoBengals";

		var result = await webCaller.Get(endingPath);

		result.IsSuccessful.Should().BeTrue($"Did not make call to {webCaller.GetFullUrl(endingPath)}");

		var response = result.To<HttpBinResponse>();

		var expectedFullUrl = GetExpectedUrl(endingPath);

		response.Url.Should().Be(expectedFullUrl);
	}

	private WebCaller CreateWebCaller(string baseUrl)
	{
		return new WebCaller(new Uri(baseUrl), new JsonOperations(), A.Fake<IToolkitLogger>());
	}

	private static string GetExpectedUrl(string endingPath)
	{
		return $"https://httpbin.org/anything/{endingPath}";
	}
}
