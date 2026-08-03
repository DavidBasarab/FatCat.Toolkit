using FatCat.Toolkit.Web.Api.SignalR;
using FatCat.Toolkit.WebServer;

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
