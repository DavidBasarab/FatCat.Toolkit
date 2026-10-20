using Microsoft.AspNetCore.HttpOverrides;

namespace Tests.FatCat.Toolkit.WebServer.ForwardedHeadersExtensionsSpecs;

public class AddToolkitForwardedHeadersTests : ForwardedHeadersExtensionsTests
{
	[Fact]
	public void ForwardTheForProtoAndHostHeadersByDefault()
	{
		ResolveOptions()
			.ForwardedHeaders.Should()
			.Be(ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost);
	}

	[Fact]
	public void LimitForwardingToOneHopByDefault()
	{
		ResolveOptions().ForwardLimit.Should().Be(1);
	}

	[Fact]
	public void TrustAnyProxyByDefault()
	{
		ResolveOptions().KnownProxies.Count.Should().Be(0);
	}

	[Fact]
	public void TrustAnyNetworkByDefault()
	{
		ResolveOptions().KnownIPNetworks.Count.Should().Be(0);
	}

	[Fact]
	public void HandTheDefaultsToTheConsumerConfiguration()
	{
		int? forwardLimitSeenByConsumer = null;

		settings.ConfigureForwardedHeaders = options =>
		{
			forwardLimitSeenByConsumer = options.ForwardLimit;
		};

		ResolveOptions();

		forwardLimitSeenByConsumer.Should().Be(1);
	}

	[Fact]
	public void ApplyTheConsumerConfigurationAfterTheDefaults()
	{
		var consumerForwardLimit = Faker.RandomInt(2, 10);

		settings.ConfigureForwardedHeaders = options =>
		{
			options.ForwardLimit = consumerForwardLimit;
		};

		ResolveOptions().ForwardLimit.Should().Be(consumerForwardLimit);
	}
}
