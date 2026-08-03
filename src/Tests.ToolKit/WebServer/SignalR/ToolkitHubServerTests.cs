using FatCat.Toolkit;
using FatCat.Toolkit.Logging;
using FatCat.Toolkit.Web.Api.SignalR;
using FatCat.Toolkit.WebServer.SignalR;
using Microsoft.AspNetCore.SignalR;

namespace Tests.FatCat.Toolkit.WebServer.SignalR;

public class ToolkitHubServerTests
{
	private readonly IHubClients clients;
	private readonly IClientProxy clientProxy;
	private readonly string connectionId;
	private readonly IGenerator generator;
	private readonly IGroupManager groupManager;
	private readonly string groupName;
	private readonly IHubContext<ToolkitHub> hubContext;
	private readonly IToolkitLogger logger;
	private readonly ToolkitMessage message;
	private readonly ToolkitHubServer sut;

	public ToolkitHubServerTests()
	{
		hubContext = A.Fake<IHubContext<ToolkitHub>>();
		clients = A.Fake<IHubClients>();
		clientProxy = A.Fake<IClientProxy>();
		groupManager = A.Fake<IGroupManager>();
		generator = A.Fake<IGenerator>();
		logger = A.Fake<IToolkitLogger>();

		connectionId = Faker.Create<string>();
		groupName = Faker.Create<string>();
		message = Faker.Create<ToolkitMessage>();

		A.CallTo(() => hubContext.Clients).Returns(clients);
		A.CallTo(() => hubContext.Groups).Returns(groupManager);
		A.CallTo(() => clients.Group(A<string>._)).Returns(clientProxy);
		A.CallTo(() => clients.All).Returns(clientProxy);

		sut = new ToolkitHubServer(hubContext, generator, logger);
	}

	[Fact]
	public async Task AddToGroupPassesConnectionIdAndGroupNameToGroupManager()
	{
		await sut.AddToGroup(connectionId, groupName);

		A.CallTo(() => groupManager.AddToGroupAsync(connectionId, groupName, A<CancellationToken>._)).MustHaveHappened();
	}

	[Fact]
	public async Task RemoveFromGroupPassesConnectionIdAndGroupNameToGroupManager()
	{
		await sut.RemoveFromGroup(connectionId, groupName);

		A.CallTo(() => groupManager.RemoveFromGroupAsync(connectionId, groupName, A<CancellationToken>._))
			.MustHaveHappened();
	}

	[Fact]
	public async Task SendToGroupSendsToTheNamedGroup()
	{
		await sut.SendToGroup(groupName, message);

		A.CallTo(() => clients.Group(groupName)).MustHaveHappened();
	}

	[Fact]
	public async Task SendToAllClientsSendsThroughAllClients()
	{
		await sut.SendToAllClients(message);

		A.CallTo(() => clients.All).MustHaveHappened();
	}

	[Fact]
	public async Task SendToClientReturnsTheClientResponseMessage()
	{
		var sessionId = Faker.Create<string>();
		A.CallTo(() => generator.NewId()).Returns(sessionId);

		var responseMessage = Faker.Create<ToolkitMessage>();

		var sendTask = sut.SendToClient(connectionId, message, TimeSpan.FromSeconds(5));

		sut.ClientResponseMessage(sessionId, responseMessage);

		var result = await sendTask;

		result.Should().Be(responseMessage);
	}

	[Fact]
	public async Task SendToClientThrowsTimeoutExceptionWhenNoResponseArrives()
	{
		var sessionId = Faker.Create<string>();
		A.CallTo(() => generator.NewId()).Returns(sessionId);

		var exception = await Record.ExceptionAsync(
			() => sut.SendToClient(connectionId, message, TimeSpan.FromMilliseconds(50))
		);

		exception.Should().BeOfType<TimeoutException>();
	}

	[Fact]
	public async Task SendToClientSwallowsAResponseThatArrivesAfterTimeout()
	{
		var sessionId = Faker.Create<string>();
		A.CallTo(() => generator.NewId()).Returns(sessionId);

		await Record.ExceptionAsync(() => sut.SendToClient(connectionId, message, TimeSpan.FromMilliseconds(50)));

		var lateException = Record.Exception(() => sut.ClientResponseMessage(sessionId, Faker.Create<ToolkitMessage>()));

		lateException.Should().BeNull();
	}
}
