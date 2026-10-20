using FatCat.Toolkit.WebServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace Tests.FatCat.Toolkit.WebServer;

public class StripUnconsumedForwardedForMiddlewareTests
{
	private readonly DefaultHttpContext context = new();
	private readonly ForwardedHeadersOptions forwardedHeadersOptions = new();
	private readonly string originalFor = Faker.RandomString();
	private readonly StripUnconsumedForwardedForMiddleware sut;
	private bool nextWasCalled;

	public StripUnconsumedForwardedForMiddlewareTests()
	{
		context.Request.Headers[ForwardedHeadersDefaults.XForwardedForHeaderName] = Faker.RandomString();
		context.Request.Headers[ForwardedHeadersDefaults.XOriginalForHeaderName] = originalFor;

		sut = new StripUnconsumedForwardedForMiddleware(CallNext, Options.Create(forwardedHeadersOptions));
	}

	[Fact]
	public async Task RemoveForwardedFor()
	{
		await sut.Invoke(context);

		context.Request.Headers.ContainsKey(ForwardedHeadersDefaults.XForwardedForHeaderName).Should().BeFalse();
	}

	[Fact]
	public async Task RemoveTheConfiguredForwardedForHeader()
	{
		var customHeaderName = Faker.RandomString();

		forwardedHeadersOptions.ForwardedForHeaderName = customHeaderName;
		context.Request.Headers[customHeaderName] = Faker.RandomString();

		await sut.Invoke(context);

		context.Request.Headers.ContainsKey(customHeaderName).Should().BeFalse();
	}

	[Fact]
	public async Task KeepTheOriginalForwardedFor()
	{
		await sut.Invoke(context);

		context.Request.Headers[ForwardedHeadersDefaults.XOriginalForHeaderName].ToString().Should().Be(originalFor);
	}

	[Fact]
	public async Task CallTheNextMiddleware()
	{
		await sut.Invoke(context);

		nextWasCalled.Should().BeTrue();
	}

	private Task CallNext(HttpContext nextContext)
	{
		nextWasCalled = true;

		return Task.CompletedTask;
	}
}
