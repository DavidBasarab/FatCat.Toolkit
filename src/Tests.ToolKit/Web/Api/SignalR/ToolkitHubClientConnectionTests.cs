using System.Linq;
using System.Net;
using System.Reflection;
using FatCat.Toolkit;
using FatCat.Toolkit.Logging;
using FatCat.Toolkit.Web.Api.SignalR;
using Humanizer;
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

		A.CallTo(() =>
				connection.On(
					ToolkitHubMethodNames.ServerResponseMessage,
					A<Type[]>._,
					A<Func<object[], object, Task>>._,
					A<object>._
				)
			)
			.MustHaveHappened()
			.Then(
				A.CallTo(() =>
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
				A.CallTo(() =>
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
	public async Task UseTheSuppliedRetryDelaysForAutomaticReconnect()
	{
		var services = new ServiceCollection();

		A.CallTo(() => builder.Services).Returns(services);

		TimeSpan[] retryDelays = [3.Seconds(), 5.Seconds()];

		await sut.Connect(hubUrl, automaticReconnect: true, retryDelays: retryDelays);

		GetRetryDelay(services, 0).Should().Be(3.Seconds());
	}

	[Fact]
	public async Task UseSignalRDefaultRetryDelaysWhenNoRetryDelaysAreSupplied()
	{
		var services = new ServiceCollection();

		A.CallTo(() => builder.Services).Returns(services);

		await sut.Connect(hubUrl, automaticReconnect: true);

		GetRetryDelay(services, 1).Should().Be(2.Seconds());
	}

	[Fact]
	public async Task NotConfigureAutomaticReconnectWhenRetryDelaysAreSuppliedWithoutOptingIn()
	{
		var services = new ServiceCollection();

		A.CallTo(() => builder.Services).Returns(services);

		TimeSpan[] retryDelays = [3.Seconds(), 5.Seconds()];

		await sut.Connect(hubUrl, retryDelays: retryDelays);

		services.Any(descriptor => descriptor.ServiceType == typeof(IRetryPolicy)).Should().Not.BeTrue();
	}

	[Fact]
	public async Task UseTheSuppliedRetryDelaysWhenTryingToConnect()
	{
		var services = new ServiceCollection();

		A.CallTo(() => builder.Services).Returns(services);

		TimeSpan[] retryDelays = [7.Seconds(), 11.Seconds()];

		await sut.TryToConnect(hubUrl, automaticReconnect: true, retryDelays: retryDelays);

		GetRetryDelay(services, 0).Should().Be(7.Seconds());
	}

	[Fact]
	public async Task RaiseConnectionLostWhenTheConnectionCloses()
	{
		var connectionLostWasRaised = false;

		sut.ConnectionLost += () =>
		{
			connectionLostWasRaised = true;

			return Task.CompletedTask;
		};

		await sut.Connect(hubUrl);

		await RaiseConnectionClosed();

		connectionLostWasRaised.Should().BeTrue();
	}

	[Fact]
	public async Task InvokeTheConnectionLostActionWhenTheConnectionCloses()
	{
		var connectionLostActionWasInvoked = false;

		Action onConnectionLost = () =>
		{
			connectionLostActionWasInvoked = true;
		};

		await sut.Connect(hubUrl, onConnectionLost);

		await RaiseConnectionClosed();

		connectionLostActionWasInvoked.Should().BeTrue();
	}

	[Fact]
	public async Task InvokeTheConnectionLostActionBeforeRaisingConnectionLost()
	{
		var callCount = 0;
		var actionCallNumber = 0;
		var eventCallNumber = 0;

		Action onConnectionLost = () =>
		{
			actionCallNumber = ++callCount;
		};

		sut.ConnectionLost += () =>
		{
			eventCallNumber = ++callCount;

			return Task.CompletedTask;
		};

		await sut.Connect(hubUrl, onConnectionLost);

		await RaiseConnectionClosed();

		actionCallNumber.Should().BeLessThan(eventCallNumber);
	}

	[Fact]
	public async Task WaitOnTheConnectionLostSubscriberWhenTheConnectionCloses()
	{
		var subscriberCompletion = new TaskCompletionSource();

		sut.ConnectionLost += () =>
		{
			return subscriberCompletion.Task;
		};

		await sut.Connect(hubUrl);

		var closedTask = RaiseConnectionClosed();

		var completedBeforeTheSubscriberDid = closedTask.IsCompleted;

		subscriberCompletion.SetResult();

		await closedTask;

		completedBeforeTheSubscriberDid.Should().BeFalse();
	}

	[Fact]
	public async Task CloseWithoutThrowingWhenNothingIsSubscribedToConnectionLost()
	{
		await sut.Connect(hubUrl);

		var exception = await Record.ExceptionAsync(() => RaiseConnectionClosed());

		exception.Should().BeNull();
	}

	[Fact]
	public async Task DisposeWithoutThrowingWhenTheConnectionWasNeverMade()
	{
		var exception = await Record.ExceptionAsync(() => sut.DisposeAsync().AsTask());

		exception.Should().BeNull();
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

	[Fact]
	public async Task SendReturnsTheResponseMessageWhenTheServerResponds()
	{
		var sessionId = Faker.Create<string>();
		A.CallTo(() => generator.NewId()).Returns(sessionId);

		await sut.Connect(hubUrl);

		var responseData = Faker.Create<string>();
		var responseMessageType = Faker.Create<int>();

		var sendTask = sut.Send(Faker.Create<ToolkitMessage>(), TimeSpan.FromSeconds(5));

		DeliverServerResponse(responseMessageType, sessionId, responseData);

		var result = await sendTask;

		result.Data.Should().Be(responseData);
	}

	[Fact]
	public async Task SendThrowsTimeoutExceptionWhenNoResponseArrives()
	{
		var sessionId = Faker.Create<string>();
		A.CallTo(() => generator.NewId()).Returns(sessionId);

		await sut.Connect(hubUrl);

		var exception = await Record.ExceptionAsync(() =>
			sut.Send(Faker.Create<ToolkitMessage>(), TimeSpan.FromMilliseconds(50))
		);

		exception.Should().BeOfType<TimeoutException>();
	}

	[Fact]
	public async Task SendSwallowsAResponseThatArrivesAfterTimeout()
	{
		var sessionId = Faker.Create<string>();
		A.CallTo(() => generator.NewId()).Returns(sessionId);

		await sut.Connect(hubUrl);

		await Record.ExceptionAsync(() => sut.Send(Faker.Create<ToolkitMessage>(), TimeSpan.FromMilliseconds(50)));

		var lateException = Record.Exception(() =>
			DeliverServerResponse(Faker.Create<int>(), sessionId, Faker.Create<string>())
		);

		lateException.Should().BeNull();
	}

	private Task RaiseConnectionClosed()
	{
		var closedField = typeof(HubConnection).GetField(
			nameof(HubConnection.Closed),
			BindingFlags.Instance | BindingFlags.NonPublic
		);

		var closedHandler = (Func<Exception, Task>)closedField.GetValue(connection);

		return closedHandler.Invoke(null);
	}

	private static TimeSpan GetRetryDelay(ServiceCollection services, int previousRetryCount)
	{
		var retryPolicy = (IRetryPolicy)
			services.First(descriptor => descriptor.ServiceType == typeof(IRetryPolicy)).ImplementationInstance;

		return retryPolicy.NextRetryDelay(new RetryContext { PreviousRetryCount = previousRetryCount }).Value;
	}

	private void DeliverServerResponse(int messageType, string sessionId, string data)
	{
		var method = typeof(ToolkitHubClientConnection).GetMethod(
			"OnServerResponseMessageReceived",
			BindingFlags.Instance | BindingFlags.NonPublic
		);

		method.Invoke(sut, new object[] { messageType, sessionId, data });
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
