using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.Logging;
using Humanizer;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;

namespace FatCat.Toolkit.Web.Api.SignalR;

public interface IToolkitHubClientConnection : IAsyncDisposable
{
	public event ToolkitHubDataBufferMessage ServerDataBufferMessage;

	public event ToolkitHubMessage ServerMessage;

	public event ToolkitHubReconnecting Reconnecting;

	public event ToolkitHubReconnected Reconnected;

	public event ToolkitHubConnectionLost ConnectionLost;

	public Task Connect(
		string hubUrl,
		Action onConnectionLost = null,
		Action<HttpConnectionOptions> configureOptions = null,
		bool automaticReconnect = false,
		TimeSpan[] retryDelays = null
	);

	public Task Disconnect();

	public Task<ToolkitMessage> Send(ToolkitMessage message, TimeSpan? timeout = null);

	public Task<ToolkitMessage> SendDataBuffer(ToolkitMessage message, byte[] dataBuffer, TimeSpan? timeout = null);

	public Task SendDataBufferNoResponse(ToolkitMessage message, byte[] dataBuffer);

	public Task SendNoResponse(ToolkitMessage message);

	public Task<bool> TryToConnect(
		string hubUrl,
		Action onConnectionLost = null,
		Action<HttpConnectionOptions> configureOptions = null,
		bool automaticReconnect = false,
		TimeSpan[] retryDelays = null
	);
}

public class ToolkitHubClientConnection(
	IGenerator generator,
	IToolkitLogger logger,
	IHubConnectionBuilderFactory hubConnectionBuilderFactory
) : IToolkitHubClientConnection
{
	private readonly ConcurrentDictionary<string, int> timedOutResponses = new();
	private readonly ConcurrentDictionary<string, TaskCompletionSource<ToolkitMessage>> waitingForResponses = new();
	private HubConnection connection;

	public event ToolkitHubDataBufferMessage ServerDataBufferMessage;

	public event ToolkitHubMessage ServerMessage;

	public event ToolkitHubReconnecting Reconnecting;

	public event ToolkitHubReconnected Reconnected;

	public event ToolkitHubConnectionLost ConnectionLost;

	public async Task Connect(
		string hubUrl,
		Action onConnectionLost = null,
		Action<HttpConnectionOptions> configureOptions = null,
		bool automaticReconnect = false,
		TimeSpan[] retryDelays = null
	)
	{
		var builder = hubConnectionBuilderFactory.Create(hubUrl, options => configureOptions?.Invoke(options));

		if (automaticReconnect)
		{
			builder = ConfigureAutomaticReconnect(builder, retryDelays);
		}

		connection = builder.Build();

		connection.Closed += a =>
		{
			onConnectionLost?.Invoke();

			return ConnectionLost?.Invoke() ?? Task.CompletedTask;
		};

		connection.Reconnecting += exception =>
		{
			return Reconnecting?.Invoke(exception) ?? Task.CompletedTask;
		};

		connection.Reconnected += connectionId =>
		{
			return Reconnected?.Invoke(connectionId) ?? Task.CompletedTask;
		};

		RegisterForServerMessages();

		await connection.StartAsync();
	}

	public async Task Disconnect()
	{
		if (connection is not null)
		{
			await connection.StopAsync();
		}
	}

	public async ValueTask DisposeAsync()
	{
		await Disconnect();

		if (connection is not null)
		{
			await connection.DisposeAsync();
		}
	}

	public async Task<ToolkitMessage> Send(ToolkitMessage message, TimeSpan? timeout = null)
	{
		timeout ??= 30.Seconds();

		var sessionId = generator.NewId();

		var completionSource = CreateResponseCompletionSource(sessionId);

		await SendSessionMessage(message.MessageType, message.Data ?? string.Empty, sessionId);

		return await WaitForResponse(message, timeout, sessionId, completionSource);
	}

	public async Task<ToolkitMessage> SendDataBuffer(ToolkitMessage message, byte[] dataBuffer, TimeSpan? timeout = null)
	{
		timeout ??= 30.Seconds();

		var sessionId = generator.NewId();

		var completionSource = CreateResponseCompletionSource(sessionId);

		logger.Debug(
			$"Going to send <{nameof(ToolkitHubMethodNames.ClientDataBufferMessage)}> | Timeout <{timeout}> | MessageType <{message.MessageType}> | SessionId <{sessionId}> | Data <{message.Data}>"
		);

		await connection.SendAsync(
			nameof(ToolkitHubMethodNames.ClientDataBufferMessage),
			message.MessageType,
			sessionId,
			message.Data,
			dataBuffer
		);

		return await WaitForResponse(message, timeout, sessionId, completionSource);
	}

	public async Task SendDataBufferNoResponse(ToolkitMessage message, byte[] dataBuffer)
	{
		var sessionId = generator.NewId();

		await connection.SendAsync(
			nameof(ToolkitHubMethodNames.ClientDataBufferMessage),
			message.MessageType,
			sessionId,
			message.Data,
			dataBuffer
		);
	}

	public Task SendNoResponse(ToolkitMessage message)
	{
		return SendSessionMessage(message.MessageType, message.Data ?? string.Empty, generator.NewId());
	}

	public async Task<bool> TryToConnect(
		string hubUrl,
		Action onConnectionLost = null,
		Action<HttpConnectionOptions> configureOptions = null,
		bool automaticReconnect = false,
		TimeSpan[] retryDelays = null
	)
	{
		try
		{
			await Connect(hubUrl, onConnectionLost, configureOptions, automaticReconnect, retryDelays);

			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	private static IHubConnectionBuilder ConfigureAutomaticReconnect(IHubConnectionBuilder builder, TimeSpan[] retryDelays)
	{
		if (retryDelays is null)
		{
			return builder.WithAutomaticReconnect();
		}

		return builder.WithAutomaticReconnect(retryDelays);
	}

	private Task<string> InvokeDataBufferMessage(ToolkitMessage message, byte[] dataBuffer)
	{
		return ServerDataBufferMessage?.Invoke(message, dataBuffer) ?? Task.FromResult<string>(null);
	}

	private Task<string> InvokeServerMessage(ToolkitMessage message)
	{
		return ServerMessage?.Invoke(message) ?? Task.FromResult<string>(null);
	}

	private Task OnConnectionClosed(Exception arg)
	{
		ConsoleLog.WriteCyan("Connection LOST");

		if (arg is not null)
		{
			ConsoleLog.WriteCyan($"    {arg.Message}  | {arg.GetType().FullName}");
		}

		return Task.CompletedTask;
	}

	private async Task OnServerOriginatedDataBufferMessage(int messageType, string sessionId, string data, byte[] bufferData)
	{
		logger.Debug(new string('-', 80));

		logger.Debug(
			$"OnServerOriginatedDataBufferMessage | MessageType <{messageType}> | SessionId <{sessionId}> | Data <{data}> | bufferData <{bufferData.Length}>"
		);

		logger.Debug(new string('-', 80));

		var message = new ToolkitMessage { Data = data, MessageType = messageType };

		var response = await InvokeDataBufferMessage(message, bufferData);

		if (response is not null)
		{
			await connection.SendAsync(nameof(ToolkitHubMethodNames.ClientResponseMessage), messageType, sessionId, response);
		}
	}

	private async Task OnServerOriginatedMessage(int messageType, string sessionId, string data)
	{
		logger.Debug(new string('-', 80));

		logger.Debug($"OnServerOriginatedMessage | MessageType <{messageType}> | SessionId <{sessionId}> | Data <{data}>");

		logger.Debug(new string('-', 80));

		var message = new ToolkitMessage { Data = data, MessageType = messageType };

		var response = await InvokeServerMessage(message);

		if (response is not null)
		{
			await connection.SendAsync(nameof(ToolkitHubMethodNames.ClientResponseMessage), messageType, sessionId, response);
		}
	}

	private void OnServerResponseMessageReceived(int messageType, string sessionId, string data)
	{
		logger.Debug($"On ServerMessageReceived | MessageType <{messageType}> | SessionId <{sessionId}> | Data <{data}>");

		if (timedOutResponses.TryRemove(sessionId, out _))
		{
			logger.Debug($"SessionId <{sessionId}> has timed out");

			return;
		}

		if (!waitingForResponses.TryRemove(sessionId, out var completionSource))
		{
			logger.Debug($"SessionId <{sessionId}> is not in WaitingForResponses");

			return;
		}

		logger.Debug($"Adding {sessionId} to Responses");

		completionSource.TrySetResult(new ToolkitMessage { MessageType = messageType, Data = data });
	}

	private void RegisterForServerMessages()
	{
		var responseMethod = OnServerResponseMessageReceived;
		var originatedMessageMethod = OnServerOriginatedMessage;
		var dataBufferMethod = OnServerOriginatedDataBufferMessage;

		connection.On(ToolkitHubMethodNames.ServerResponseMessage, responseMethod);
		connection.On(ToolkitHubMethodNames.ServerOriginatedMessage, originatedMessageMethod);
		connection.On(ToolkitHubMethodNames.ServerDataBufferMessage, dataBufferMethod);
	}

	private Task SendSessionMessage(int messageType, string data, string sessionId)
	{
		return connection.SendAsync(nameof(ToolkitHubMethodNames.ClientMessage), messageType, sessionId, data);
	}

	private TaskCompletionSource<ToolkitMessage> CreateResponseCompletionSource(string sessionId)
	{
		var completionSource = new TaskCompletionSource<ToolkitMessage>(TaskCreationOptions.RunContinuationsAsynchronously);

		waitingForResponses.TryAdd(sessionId, completionSource);

		return completionSource;
	}

	private async Task<ToolkitMessage> WaitForResponse(
		ToolkitMessage message,
		[DisallowNull] TimeSpan? timeout,
		string sessionId,
		TaskCompletionSource<ToolkitMessage> completionSource
	)
	{
		using var cancellationSource = new CancellationTokenSource(timeout.Value);
		using var cancellationRegistration = cancellationSource.Token.Register(() => completionSource.TrySetCanceled());

		try
		{
			var response = await completionSource.Task;

			logger.Debug(
				$"Got response for | MessageType <{message.MessageType}> | SessionId <{sessionId}> | ResponseData := {response.Data}"
			);

			return response;
		}
		catch (OperationCanceledException)
		{
			logger.Debug($"!!!! Timing out for | MessageType <{message.MessageType}> | SessionId <{sessionId}>");

			timedOutResponses.TryAdd(sessionId, message.MessageType);
			waitingForResponses.TryRemove(sessionId, out _);

			throw new TimeoutException();
		}
	}
}
