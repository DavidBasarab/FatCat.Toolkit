using System.Net;
using FatCat.Toolkit.WebServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Tests.FatCat.Toolkit.WebServer.ForwardedHeadersExtensionsSpecs;

public abstract class ForwardedHeadersExtensionsTests
{
	protected readonly IPAddress callerWrittenAddress = RandomIpAddress();
	protected readonly IPAddress clientAddress = RandomIpAddress();
	protected readonly IPAddress connectionAddress = RandomIpAddress();
	protected readonly DefaultHttpContext context = new();
	protected readonly ToolkitWebApplicationSettings settings = new();

	protected ForwardedHeadersExtensionsTests()
	{
		context.Connection.RemoteIpAddress = connectionAddress;
	}

	protected string ForwardedFor
	{
		get { return context.Request.Headers[ForwardedHeadersDefaults.XForwardedForHeaderName].ToString(); }
	}

	protected string OriginalFor
	{
		get { return context.Request.Headers[ForwardedHeadersDefaults.XOriginalForHeaderName].ToString(); }
	}

	protected ForwardedHeadersOptions ResolveOptions()
	{
		return BuildServiceProvider().GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;
	}

	protected async Task SendRequestForwardedFor(params IPAddress[] hops)
	{
		context.Request.Headers[ForwardedHeadersDefaults.XForwardedForHeaderName] = ToForwardedFor(hops);

		var applicationBuilder = new ApplicationBuilder(BuildServiceProvider());

		applicationBuilder.UseToolkitForwardedHeaders(settings);

		await applicationBuilder.Build().Invoke(context);
	}

	private static IPAddress RandomIpAddress()
	{
		return new IPAddress(Faker.RandomBytes(4));
	}

	private static string ToForwardedFor(params IPAddress[] hops)
	{
		return string.Join(", ", hops.Select(hop => hop.ToString()));
	}

	private ServiceProvider BuildServiceProvider()
	{
		return new ServiceCollection().AddLogging().AddToolkitForwardedHeaders(settings).BuildServiceProvider();
	}
}
