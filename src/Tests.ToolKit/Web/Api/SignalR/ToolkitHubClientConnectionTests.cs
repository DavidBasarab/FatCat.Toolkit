using FatCat.Toolkit;
using FatCat.Toolkit.Logging;
using FatCat.Toolkit.Web.Api.SignalR;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;

namespace Tests.FatCat.Toolkit.Web.Api.SignalR;

public class ToolkitHubClientConnectionTests
{
	private readonly IHubConnectionBuilder builder;
	private readonly IHubConnectionBuilderFactory builderFactory;
	private readonly IGenerator generator;
	private readonly string hubUrl;
	private readonly IToolkitLogger logger;
	private readonly ToolkitHubClientConnection sut;

	public ToolkitHubClientConnectionTests()
	{
		generator = A.Fake<IGenerator>();
		logger = A.Fake<IToolkitLogger>();
		builderFactory = A.Fake<IHubConnectionBuilderFactory>();
		builder = A.Fake<IHubConnectionBuilder>();
		hubUrl = Faker.Create<string>();

		A.CallTo(() => builderFactory.Create(A<string>._, A<Action<HttpConnectionOptions>>._)).Returns(builder);

		sut = new ToolkitHubClientConnection(generator, logger, builderFactory);
	}

	[Fact]
	public async Task BuildTheConnectionFromTheBuilder()
	{
		await RunConnect();

		A.CallTo(() => builder.Build()).MustHaveHappened();
	}

	[Fact]
	public async Task CreateTheBuilderBeforeBuildingTheConnection()
	{
		await RunConnect();

		A.CallTo(() => builderFactory.Create(hubUrl, A<Action<HttpConnectionOptions>>._))
			.MustHaveHappened()
			.Then(A.CallTo(() => builder.Build()).MustHaveHappened());
	}

	[Fact]
	public async Task CreateTheBuilderWithTheHubUrl()
	{
		await RunConnect();

		A.CallTo(() => builderFactory.Create(hubUrl, A<Action<HttpConnectionOptions>>._)).MustHaveHappened();
	}

	private async Task RunConnect()
	{
		try
		{
			await sut.Connect(hubUrl);
		}
		catch
		{
			// ignored — the faked builder yields no live HubConnection, so Connect cannot reach a server; this phase asserts only the builder seam
		}
	}
}
