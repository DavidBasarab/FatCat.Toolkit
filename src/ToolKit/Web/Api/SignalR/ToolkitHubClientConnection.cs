using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.Logging;
using Humanizer;
using Microsoft.AspNetCore.SignalR.Client;

namespace FatCat.Toolkit.Web.Api.SignalR;

public interface IToolkitHubClientConnection : IAsyncDisposable
{
	public event ToolkitHubDataBufferMessage ServerDataBufferMessage;

	public event ToolkitHubMessage ServerMessage;

	public Task Connect(string hubUrl, Action onConnectionLost = null);

	public Task Disconnect();

	public Task<ToolkitMessage> Send(ToolkitMessage message, TimeSpan? timeout = null);

	public Task<ToolkitMessage> SendDataBuffer(ToolkitMessage message, byte[] dataBuffer, TimeSpan? timeout = null);

	public Task SendDataBufferNoResponse(ToolkitMessage message, byte[] dataBuffer);

	public Task SendNoResponse(ToolkitMessage message);

	public Task<bool> TryToConnect(string hubUrl, Action onConnectionLost = null);
}

public class ToolkitHubClientConnection(IGenerator generator, IToolkitLogger logger) : IToolkitHubClientConnection
{
	private readonly ConcurrentDictionary<string, ToolkitMessage> responses = new();
	private readonly ConcurrentDictionary<string, int> timedOutResponses = new();
	private readonly ConcurrentDictionary<string, ToolkitMessage> waitingForResponses = new();
	private HubConnection connection;

	public event ToolkitHubDataBufferMessage ServerDataBufferMessage;

	public event ToolkitHubMessage ServerMessage;

	public async Task Connect(string hubUrl, Action onConnectionLost = null)
	{
		connection = new HubConnectionBuilder().WithUrl(hubUrl, options => { }).Build();

		connection.Closed += a =>
		{
			onConnectionLost?.Invoke();

			return Task.CompletedTask;
		};

		await connection.StartAsync();

		RegisterForServerMessages();
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
		await connection.DisposeAsync();
	}

	public async Task<ToolkitMessage> Send(ToolkitMessage message, TimeSpan? timeout = null)
	{
		timeout ??= 30.Seconds();

		var sessionId = generator.NewId();

		waitingForResponses.TryAdd(sessionId, message);

		await SendSessionMessage(message.MessageType, message.Data ?? string.Empty, sessionId);

		return await WaitForResponse(message, timeout, sessionId);
	}

	public async Task<ToolkitMessage> SendDataBuffer(ToolkitMessage message, byte[] dataBuffer, TimeSpan? timeout = null)
	{
		timeout ??= 30.Seconds();

		var sessionId = generator.NewId();

		waitingForResponses.TryAdd(sessionId, message);

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

		return await WaitForResponse(message, timeout, sessionId);
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

	public async Task<bool> TryToConnect(string hubUrl, Action onConnectionLost = null)
	{
		try
		{
			await Connect(hubUrl, onConnectionLost);

			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	private Task<string> InvokeDataBufferMessage(ToolkitMessage message, byte[] dataBuffer)
	{
		return ServerDataBufferMessage?.Invoke(message, dataBuffer)!;
	}

	private Task<string> InvokeServerMessage(ToolkitMessage message)
	{
		return ServerMessage?.Invoke(message)!;
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

		if (!waitingForResponses.TryRemove(sessionId, out _))
		{
			logger.Debug($"SessionId <{sessionId}> is not in WaitingForResponses");

			return;
		}

		logger.Debug($"Adding {sessionId} to Responses");

		responses.TryAdd(sessionId, new ToolkitMessage { MessageType = messageType, Data = data });
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

	private async Task<ToolkitMessage> WaitForResponse(
		ToolkitMessage message,
		[DisallowNull] TimeSpan? timeout,
		string sessionId
	)
	{
		var startTime = DateTime.UtcNow;

		while (true)
		{
			if (responses.TryRemove(sessionId, out var response))
			{
				logger.Debug(
					$"Got response for | MessageType <{message.MessageType}> | SessionId <{sessionId}> | ResponseData := {response.Data}"
				);

				return response;
			}

			if (DateTime.UtcNow - startTime > timeout)
			{
				logger.Debug($"!!!! Timing out for | MessageType <{message.MessageType}> | SessionId <{sessionId}>");

				timedOutResponses.TryAdd(sessionId, message.MessageType);

				throw new TimeoutException();
			}

			await Task.Delay(35);
		}
	}
}
