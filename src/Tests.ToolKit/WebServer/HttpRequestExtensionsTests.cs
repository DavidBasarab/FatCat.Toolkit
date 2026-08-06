using FatCat.Toolkit.WebServer;
using Microsoft.AspNetCore.Http;

namespace Tests.FatCat.Toolkit.WebServer;

public class HttpRequestExtensionsTests
{
	private readonly string path = $"/api/{Faker.RandomString()}";
	private readonly HttpRequest request = new DefaultHttpContext().Request;
	private readonly string secretValue = Faker.Create<string>();

	[Fact]
	public void ReturnThePathWhenThereIsNoQueryString()
	{
		request.Path = path;

		request.DisplayPath().Should().Be(path);
	}

	[Fact]
	public void ReturnOnlyThePathWhenAQueryStringIsPresent()
	{
		request.Path = path;
		request.QueryString = new QueryString($"?first={secretValue}");

		request.DisplayPath().Should().Be(path);
	}

	[Fact]
	public void NeverIncludeAQueryStringValueInThePath()
	{
		request.Path = path;
		request.QueryString = new QueryString($"?first={secretValue}");

		request.DisplayPath().Should().Not.Contain(secretValue);
	}

	[Fact]
	public void NeverIncludeTheHostInThePath()
	{
		var host = $"{Faker.RandomString()}.example.com";

		request.Host = new HostString(host);
		request.Path = path;

		request.DisplayPath().Should().Not.Contain(host);
	}

	[Fact]
	public void IncludeThePathBaseWhenOneIsSet()
	{
		var pathBase = $"/{Faker.RandomString()}";

		request.PathBase = pathBase;
		request.Path = path;

		request.DisplayPath().Should().Be($"{pathBase}{path}");
	}

	[Fact]
	public void ReturnAnEmptyStringWhenNoPathIsSet()
	{
		request.DisplayPath().Should().BeEmpty();
	}
}
