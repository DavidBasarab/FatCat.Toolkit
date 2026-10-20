using System.Net;

namespace Tests.FatCat.Toolkit.WebServer.ForwardedHeadersExtensionsSpecs;

public class UseToolkitForwardedHeadersTests : ForwardedHeadersExtensionsTests
{
	[Fact]
	public async Task UseASingleForwardedHopAsTheClientAddress()
	{
		await SendRequestForwardedFor(clientAddress);

		context.Connection.RemoteIpAddress.Should().Be(clientAddress);
	}

	[Fact]
	public async Task UseTheProxyWrittenHopWhenTheCallerAlsoWroteOne()
	{
		await SendRequestForwardedFor(callerWrittenAddress, clientAddress);

		context.Connection.RemoteIpAddress.Should().Be(clientAddress);
	}

	[Fact]
	public async Task LeaveTheCallerWrittenHopInForwardedForByDefault()
	{
		await SendRequestForwardedFor(callerWrittenAddress, clientAddress);

		ForwardedFor.Should().Be(callerWrittenAddress.ToString());
	}

	[Fact]
	public async Task UseTheProxyWrittenHopWhenStrippingUnconsumedForwardedFor()
	{
		settings.StripUnconsumedForwardedFor = true;

		await SendRequestForwardedFor(callerWrittenAddress, clientAddress);

		context.Connection.RemoteIpAddress.Should().Be(clientAddress);
	}

	[Fact]
	public async Task RemoveTheLeftoverForwardedForWhenStrippingUnconsumedForwardedFor()
	{
		settings.StripUnconsumedForwardedFor = true;

		await SendRequestForwardedFor(callerWrittenAddress, clientAddress);

		ForwardedFor.Should().BeEmpty();
	}

	[Fact]
	public async Task KeepTheProxyAddressInOriginalForWhenStrippingUnconsumedForwardedFor()
	{
		settings.StripUnconsumedForwardedFor = true;

		await SendRequestForwardedFor(callerWrittenAddress, clientAddress);

		OriginalFor.Should().Be(new IPEndPoint(connectionAddress, 0).ToString());
	}

	[Fact]
	public async Task KeepTheConnectionAddressWhenForwardedHeadersAreTurnedOff()
	{
		settings.UseForwardedHeaders = false;

		await SendRequestForwardedFor(callerWrittenAddress);

		context.Connection.RemoteIpAddress.Should().Be(connectionAddress);
	}

	[Fact]
	public async Task RemoveForwardedForWhenStrippingWithForwardedHeadersTurnedOff()
	{
		settings.UseForwardedHeaders = false;
		settings.StripUnconsumedForwardedFor = true;

		await SendRequestForwardedFor(callerWrittenAddress);

		ForwardedFor.Should().BeEmpty();
	}

	[Fact]
	public async Task KeepTheConnectionAddressWhenTheSenderIsNotAKnownProxy()
	{
		settings.ConfigureForwardedHeaders = options =>
		{
			options.KnownProxies.Add(callerWrittenAddress);
		};

		await SendRequestForwardedFor(clientAddress);

		context.Connection.RemoteIpAddress.Should().Be(connectionAddress);
	}
}
