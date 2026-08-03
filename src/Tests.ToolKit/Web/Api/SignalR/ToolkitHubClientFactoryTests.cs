using FatCat.Toolkit.Injection;
using FatCat.Toolkit.Web.Api.SignalR;
using Microsoft.AspNetCore.Http.Connections.Client;

namespace Tests.FatCat.Toolkit.Web.Api.SignalR;

public class ToolkitHubClientFactoryTests
{
	private readonly IToolkitHubClientConnection connection;
	private readonly string hubUrl;
	private readonly ISystemScope scope;
	private readonly ToolkitHubClientFactory sut;

	public ToolkitHubClientFactoryTests()
	{
		scope = A.Fake<ISystemScope>();
		connection = A.Fake<IToolkitHubClientConnection>();
		hubUrl = Faker.Create<string>();

		A.CallTo(() => scope.Resolve<IToolkitHubClientConnection>()).Returns(connection);
		A.CallTo(() => connection.TryToConnect(A<string>._, A<Action>._, A<Action<HttpConnectionOptions>>._, A<bool>._))
			.Returns(true);

		sut = new ToolkitHubClientFactory(scope);
	}

	[Fact]
	public async Task ConnectToClientThreadsConfigureOptionsToConnection()
	{
		Action<HttpConnectionOptions> configureOptions = options => { };

		await sut.ConnectToClient(hubUrl, configureOptions);

		A.CallTo(() => connection.Connect(hubUrl, A<Action>._, configureOptions, A<bool>._)).MustHaveHappened();
	}

	[Fact]
	public async Task ConnectToClientThreadsAutomaticReconnectToConnection()
	{
		await sut.ConnectToClient(hubUrl, automaticReconnect: true);

		A.CallTo(() => connection.Connect(hubUrl, A<Action>._, A<Action<HttpConnectionOptions>>._, true)).MustHaveHappened();
	}

	[Fact]
	public async Task TryToConnectToClientThreadsConfigureOptionsToConnection()
	{
		Action<HttpConnectionOptions> configureOptions = options => { };

		await sut.TryToConnectToClient(hubUrl, configureOptions: configureOptions);

		A.CallTo(() => connection.TryToConnect(hubUrl, A<Action>._, configureOptions, A<bool>._)).MustHaveHappened();
	}

	[Fact]
	public async Task TryToConnectToClientThreadsAutomaticReconnectToConnection()
	{
		await sut.TryToConnectToClient(hubUrl, automaticReconnect: true);

		A.CallTo(() => connection.TryToConnect(hubUrl, A<Action>._, A<Action<HttpConnectionOptions>>._, true))
			.MustHaveHappened();
	}
}
