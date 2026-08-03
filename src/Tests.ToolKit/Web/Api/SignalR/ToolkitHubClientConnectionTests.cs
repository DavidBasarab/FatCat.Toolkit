using System.Linq;
using System.Net;
using System.Reflection;
using FatCat.Toolkit;
using FatCat.Toolkit.Logging;
using FatCat.Toolkit.Web.Api.SignalR;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Tests.FatCat.Toolkit.Web.Api.SignalR;

public class ToolkitHubClientConnectionTests
{
	private readonly IHubConnectionBuilder builder;
	private readonly IHubConnectionBuilderFactory builderFactory;
	private readonly HubConnection connection;
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
		connection = CreateFakeConnection();
		hubUrl = Faker.Create<string>();

		A.CallTo(() => builderFactory.Create(A<string>._, A<Action<HttpConnectionOptions>>._)).Returns(builder);
		A.CallTo(() => builder.Build()).Returns(connection);
		A.CallTo(() => connection.StartAsync(A<CancellationToken>._)).Returns(Task.CompletedTask);

		sut = new ToolkitHubClientConnection(generator, logger, builderFactory);
	}

	[Fact]
	public async Task BuildTheConnectionFromTheBuilder()
	{
		await sut.Connect(hubUrl);

		A.CallTo(() => builder.Build()).MustHaveHappened();
	}

	[Fact]
	public async Task CreateTheBuilderBeforeBuildingTheConnection()
	{
		await sut.Connect(hubUrl);

		A.CallTo(() => builderFactory.Create(hubUrl, A<Action<HttpConnectionOptions>>._))
			.MustHaveHappened()
			.Then(A.CallTo(() => builder.Build()).MustHaveHappened());
	}

	[Fact]
	public async Task CreateTheBuilderWithTheHubUrl()
	{
		await sut.Connect(hubUrl);

		A.CallTo(() => builderFactory.Create(hubUrl, A<Action<HttpConnectionOptions>>._)).MustHaveHappened();
	}

	[Fact]
	public async Task RegisterServerMessageHandlersBeforeStartingConnection()
	{
		await sut.Connect(hubUrl);

		A.CallTo(
				() =>
					connection.On(
						ToolkitHubMethodNames.ServerResponseMessage,
						A<Type[]>._,
						A<Func<object[], object, Task>>._,
						A<object>._
					)
			)
			.MustHaveHappened()
			.Then(
				A.CallTo(
						() =>
							connection.On(
								ToolkitHubMethodNames.ServerOriginatedMessage,
								A<Type[]>._,
								A<Func<object[], object, Task>>._,
								A<object>._
							)
					)
					.MustHaveHappened()
			)
			.Then(
				A.CallTo(
						() =>
							connection.On(
								ToolkitHubMethodNames.ServerDataBufferMessage,
								A<Type[]>._,
								A<Func<object[], object, Task>>._,
								A<object>._
							)
					)
					.MustHaveHappened()
			)
			.Then(A.CallTo(() => connection.StartAsync(A<CancellationToken>._)).MustHaveHappened());
	}

	[Fact]
	public async Task InvokeCallerConfigureOptionsWithTheRealOptionsInstance()
	{
		var realOptions = new HttpConnectionOptions();
		HttpConnectionOptions optionsSeenByCaller = null;

		Action<HttpConnectionOptions> callerConfigure = options =>
		{
			optionsSeenByCaller = options;
		};

		Action<HttpConnectionOptions> configurePassedToFactory = null;

		A.CallTo(() => builderFactory.Create(A<string>._, A<Action<HttpConnectionOptions>>._))
			.Invokes((string url, Action<HttpConnectionOptions> configure) => configurePassedToFactory = configure)
			.Returns(builder);

		await sut.Connect(hubUrl, configureOptions: callerConfigure);

		configurePassedToFactory.Invoke(realOptions);

		optionsSeenByCaller.Should().Be(realOptions);
	}

	[Fact]
	public async Task ConfigureBuilderForAutomaticReconnectWhenOptedIn()
	{
		var services = new ServiceCollection();

		A.CallTo(() => builder.Services).Returns(services);

		await sut.Connect(hubUrl, automaticReconnect: true);

		services.Any(descriptor => descriptor.ServiceType == typeof(IRetryPolicy)).Should().BeTrue();
	}

	[Fact]
	public async Task NotConfigureBuilderForAutomaticReconnectWhenNotOptedIn()
	{
		var services = new ServiceCollection();

		A.CallTo(() => builder.Services).Returns(services);

		await sut.Connect(hubUrl);

		services.Any(descriptor => descriptor.ServiceType == typeof(IRetryPolicy)).Should().Not.BeTrue();
	}

	[Fact]
	public async Task InvokeServerMessageReturnsNullWhenNoSubscriber()
	{
		var result = await InvokeServerMessage(Faker.Create<ToolkitMessage>());

		result.Should().BeNull();
	}

	[Fact]
	public async Task InvokeServerMessageFlowsSubscriberResult()
	{
		var expected = Faker.Create<string>();

		sut.ServerMessage += incoming =>
		{
			return Task.FromResult(expected);
		};

		var result = await InvokeServerMessage(Faker.Create<ToolkitMessage>());

		result.Should().Be(expected);
	}

	[Fact]
	public async Task InvokeDataBufferMessageReturnsNullWhenNoSubscriber()
	{
		var result = await InvokeDataBufferMessage(Faker.Create<ToolkitMessage>(), Faker.Create<byte[]>());

		result.Should().BeNull();
	}

	[Fact]
	public async Task InvokeDataBufferMessageFlowsSubscriberResult()
	{
		var expected = Faker.Create<string>();

		sut.ServerDataBufferMessage += (incoming, buffer) =>
		{
			return Task.FromResult(expected);
		};

		var result = await InvokeDataBufferMessage(Faker.Create<ToolkitMessage>(), Faker.Create<byte[]>());

		result.Should().Be(expected);
	}

	private Task<string> InvokeServerMessage(ToolkitMessage message)
	{
		var method = typeof(ToolkitHubClientConnection).GetMethod(
			nameof(InvokeServerMessage),
			BindingFlags.Instance | BindingFlags.NonPublic
		);

		return (Task<string>)method.Invoke(sut, new object[] { message });
	}

	private Task<string> InvokeDataBufferMessage(ToolkitMessage message, byte[] dataBuffer)
	{
		var method = typeof(ToolkitHubClientConnection).GetMethod(
			nameof(InvokeDataBufferMessage),
			BindingFlags.Instance | BindingFlags.NonPublic
		);

		return (Task<string>)method.Invoke(sut, new object[] { message, dataBuffer });
	}

	private static HubConnection CreateFakeConnection()
	{
		return A.Fake<HubConnection>(options =>
			options.WithArgumentsForConstructor(
				new object[]
				{
					A.Fake<IConnectionFactory>(),
					A.Fake<IHubProtocol>(),
					new DnsEndPoint("localhost", 0),
					new ServiceCollection().BuildServiceProvider(),
					NullLoggerFactory.Instance,
				}
			)
		);
	}
}
