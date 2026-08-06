using FatCat.Toolkit.Web.Api.SignalR;
using FatCat.Toolkit.WebServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.FatCat.Toolkit.WebServer.SignalR;

public class ToolkitWebApplicationSettingsTests
{
	private readonly string connectionId;
	private readonly byte[] dataBuffer;
	private readonly ToolkitMessage message;
	private readonly ToolkitWebApplicationSettings sut;
	private readonly ToolkitUser user;

	public ToolkitWebApplicationSettingsTests()
	{
		sut = new ToolkitWebApplicationSettings();
		message = Faker.Create<ToolkitMessage>();
		user = Faker.Create<ToolkitUser>();
		connectionId = Faker.Create<string>();
		dataBuffer = Faker.Create<byte[]>();
	}

	[Fact]
	public void SignalRRequireAuthorizationDefaultsToTrue()
	{
		sut.SignalRRequireAuthorization.Should().BeTrue();
	}

	[Fact]
	public void ConfigureServicesDefaultsToNull()
	{
		sut.ConfigureServices.Should().BeNull();
	}

	[Fact]
	public void ConfigureServicesInvokesWithTheGivenServiceCollection()
	{
		IServiceCollection invokedWithCollection = null;

		sut.ConfigureServices = incomingCollection =>
		{
			invokedWithCollection = incomingCollection;
		};

		var serviceCollection = new ServiceCollection();

		sut.ConfigureServices.Invoke(serviceCollection);

		invokedWithCollection.Should().BeSameAs(serviceCollection);
	}

	[Fact]
	public void ConfigureRoutedMiddlewareDefaultsToNull()
	{
		sut.ConfigureRoutedMiddleware.Should().BeNull();
	}

	[Fact]
	public void ConfigureRoutedMiddlewareInvokesWithTheGivenApplicationBuilder()
	{
		IApplicationBuilder invokedWithBuilder = null;

		sut.ConfigureRoutedMiddleware = incomingBuilder =>
		{
			invokedWithBuilder = incomingBuilder;
		};

		var applicationBuilder = A.Fake<IApplicationBuilder>();

		sut.ConfigureRoutedMiddleware.Invoke(applicationBuilder);

		invokedWithBuilder.Should().BeSameAs(applicationBuilder);
	}

	[Fact]
	public void SettingConfigureMiddlewareLeavesConfigureRoutedMiddlewareNull()
	{
		sut.ConfigureMiddleware = applicationBuilder => { };

		sut.ConfigureRoutedMiddleware.Should().BeNull();
	}

	[Fact]
	public void SettingConfigureRoutedMiddlewareLeavesConfigureMiddlewareNull()
	{
		sut.ConfigureRoutedMiddleware = applicationBuilder => { };

		sut.ConfigureMiddleware.Should().BeNull();
	}

	[Fact]
	public async Task OnClientHubMessageReturnsNullWhenNoSubscriber()
	{
		var result = await sut.OnClientHubMessage(message);

		result.Should().BeNull();
	}

	[Fact]
	public async Task OnClientHubMessageFlowsSubscriberResult()
	{
		var expected = Faker.Create<string>();

		sut.ClientMessage += incoming =>
		{
			return Task.FromResult(expected);
		};

		var result = await sut.OnClientHubMessage(message);

		result.Should().Be(expected);
	}

	[Fact]
	public async Task OnClientDataBufferMessageReturnsNullWhenNoSubscriber()
	{
		var result = await sut.OnOnClientDataBufferMessage(message, dataBuffer);

		result.Should().BeNull();
	}

	[Fact]
	public async Task OnClientDataBufferMessageFlowsSubscriberResult()
	{
		var expected = Faker.Create<string>();

		sut.ClientDataBufferMessage += (incoming, buffer) =>
		{
			return Task.FromResult(expected);
		};

		var result = await sut.OnOnClientDataBufferMessage(message, dataBuffer);

		result.Should().Be(expected);
	}

	[Fact]
	public void OnClientConnectedReturnsCompletedTaskWhenNoSubscriber()
	{
		var result = sut.OnClientConnected(user, connectionId);

		result.IsCompletedSuccessfully.Should().BeTrue();
	}

	[Fact]
	public async Task OnClientConnectedInvokesSubscriber()
	{
		var wasCalledWithConnectionId = string.Empty;

		sut.ClientConnected += (incomingUser, incomingConnectionId) =>
		{
			wasCalledWithConnectionId = incomingConnectionId;

			return Task.CompletedTask;
		};

		await sut.OnClientConnected(user, connectionId);

		wasCalledWithConnectionId.Should().Be(connectionId);
	}

	[Fact]
	public void OnClientDisconnectedReturnsCompletedTaskWhenNoSubscriber()
	{
		var result = sut.OnClientDisconnected(user, connectionId);

		result.IsCompletedSuccessfully.Should().BeTrue();
	}

	[Fact]
	public async Task OnClientDisconnectedInvokesSubscriber()
	{
		var wasCalledWithConnectionId = string.Empty;

		sut.ClientDisconnected += (incomingUser, incomingConnectionId) =>
		{
			wasCalledWithConnectionId = incomingConnectionId;

			return Task.CompletedTask;
		};

		await sut.OnClientDisconnected(user, connectionId);

		wasCalledWithConnectionId.Should().Be(connectionId);
	}
}
