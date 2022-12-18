﻿#nullable enable
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using FatCat.Toolkit.Logging;
using Humanizer;
using Microsoft.AspNetCore.SignalR.Client;

namespace FatCat.Toolkit.Web.Api.SignalR;

public interface IToolkitHubClientConnection : IAsyncDisposable
{
	event ToolkitHubDataBufferMessage? ServerDataBufferMessage;

	event ToolkitHubMessage? ServerMessage;

	Task Connect(string hubUrl);

	Task<ToolkitMessage> Send(ToolkitMessage message, TimeSpan? timeout = null);

	Task<ToolkitMessage> SendDataBuffer(ToolkitMessage message, byte[] dataBuffer, TimeSpan? timeout = null);

	Task SendDataBufferNoResponse(ToolkitMessage message, byte[] dataBuffer);

	Task SendNoResponse(ToolkitMessage message);

	Task<bool> TryToConnect(string hubUrl);
}

public class ToolkitHubClientConnection : IToolkitHubClientConnection
{
	private readonly IGenerator generator;
	private readonly IToolkitLogger logger;

	private readonly ConcurrentDictionary<string, ToolkitMessage> responses = new();
	private readonly ConcurrentDictionary<string, int> timedOutResponses = new();
	private readonly ConcurrentDictionary<string, ToolkitMessage> waitingForResponses = new();
	private HubConnection connection = null!;

	public ToolkitHubClientConnection(IGenerator generator,
									IToolkitLogger logger)
	{
		this.generator = generator;
		this.logger = logger;
	}

	public event ToolkitHubDataBufferMessage? ServerDataBufferMessage;

	public event ToolkitHubMessage? ServerMessage;

	public async Task Connect(string hubUrl)
	{
		connection = new HubConnectionBuilder()
					.WithUrl(hubUrl)
					.Build();

		await connection.StartAsync();

		RegisterForServerMessages();
	}

	public ValueTask DisposeAsync() => connection.DisposeAsync();

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

		logger.Debug($"Going to send <{nameof(ToolkitHub.ClientDataBufferMessage)}> | Timeout <{timeout}> | MessageType <{message.MessageType}> | SessionId <{sessionId}> | Data <{message.Data}>");

		await connection.SendAsync(nameof(ToolkitHub.ClientDataBufferMessage), message.MessageType, sessionId, message.Data, dataBuffer);

		return await WaitForResponse(message, timeout, sessionId);
	}

	public async Task SendDataBufferNoResponse(ToolkitMessage message, byte[] dataBuffer)
	{
		var sessionId = generator.NewId();

		await connection.SendAsync(nameof(ToolkitHub.ClientDataBufferMessage), message.MessageType, sessionId, message.Data, dataBuffer);
	}

	public Task SendNoResponse(ToolkitMessage message) => SendSessionMessage(message.MessageType, message.Data ?? string.Empty, generator.NewId());

	public async Task<bool> TryToConnect(string hubUrl)
	{
		try
		{
			await Connect(hubUrl);

			return true;
		}
		catch (Exception) { return false; }
	}

	private Task<string?> InvokeDataBufferMessage(ToolkitMessage message, byte[] dataBuffer) => ServerDataBufferMessage?.Invoke(message, dataBuffer)!;

	private Task<string?> InvokeServerMessage(ToolkitMessage message) => ServerMessage?.Invoke(message)!;

	private async Task OnServerOriginatedDataBufferMessage(int messageType, string sessionId, string data, byte[] bufferData)
	{
		logger.Debug(new string('-', 80));
		logger.Debug($"OnServerOriginatedDataBufferMessage | MessageType <{messageType}> | SessionId <{sessionId}> | Data <{data}> | bufferData <{bufferData.Length}>");
		logger.Debug(new string('-', 80));

		var message = new ToolkitMessage
					{
						Data = data,
						MessageType = messageType
					};

		var response = await InvokeDataBufferMessage(message, bufferData);

		if (response is not null) await connection.SendAsync(nameof(ToolkitHub.ClientResponseMessage), messageType, sessionId, response);
	}

	private async Task OnServerOriginatedMessage(int messageType, string sessionId, string data)
	{
		logger.Debug(new string('-', 80));
		logger.Debug($"OnServerOriginatedMessage | MessageType <{messageType}> | SessionId <{sessionId}> | Data <{data}>");
		logger.Debug(new string('-', 80));

		var message = new ToolkitMessage
					{
						Data = data,
						MessageType = messageType
					};

		var response = await InvokeServerMessage(message);

		if (response is not null) await connection.SendAsync(nameof(ToolkitHub.ClientResponseMessage), messageType, sessionId, response);
	}

	private void OnServerResponseMessageReceived(int messageType, string sessionId, string data)
	{
		if (timedOutResponses.TryRemove(sessionId, out _)) return;

		if (!waitingForResponses.TryRemove(sessionId, out _)) return;

		logger.Debug($"On ServerMessageReceived | MessageType <{messageType}> | SessionId <{sessionId}> | Data <{data}>");

		responses.TryAdd(sessionId, new ToolkitMessage
									{
										MessageType = messageType,
										Data = data
									});
	}

	private void RegisterForServerMessages()
	{
		var responseMethod = OnServerResponseMessageReceived;
		var originatedMessageMethod = OnServerOriginatedMessage;
		var dataBufferMethod = OnServerOriginatedDataBufferMessage;

		connection.On(ToolkitHub.ServerResponseMessage, responseMethod);
		connection.On(ToolkitHub.ServerOriginatedMessage, originatedMessageMethod);
		connection.On(ToolkitHub.ServerDataBufferMessage, dataBufferMethod);
	}

	private Task SendSessionMessage(int messageType, string data, string sessionId) => connection.SendAsync(nameof(ToolkitHub.ClientMessage), messageType, sessionId, data);

	private async Task<ToolkitMessage> WaitForResponse(ToolkitMessage message, [DisallowNull] TimeSpan? timeout, string sessionId)
	{
		var startTime = DateTime.UtcNow;

		while (true)
		{
			if (responses.TryRemove(sessionId, out var response))
			{
				logger.Debug($"Got response for | MessageType <{message.MessageType}> | SessionId <{sessionId}> | ResponseData := {response.Data}");

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