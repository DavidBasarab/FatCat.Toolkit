using System.Collections.Concurrent;
using FatCat.Toolkit.Json;
using FatCat.Toolkit.Logging;
using FatCat.Toolkit.Web.Api;
using FatCat.Toolkit.Web.Api.SignalR;
using Humanizer;
using Microsoft.AspNetCore.SignalR;

namespace FatCat.Toolkit.WebServer.SignalR;

public interface IToolkitHubServer
{
	public event ToolkitHubClientConnected ClientConnected;

	public event ToolkitHubClientDisconnected ClientDisconnected;

	public void ClientResponseDataBufferMessage(string sessionId, ToolkitMessage toolkitMessage, byte[] dataBuffer);

	public void ClientResponseMessage(string sessionId, ToolkitMessage toolkitMessage);

	public List<string> GetConnections();

	public void OnClientConnected(ToolkitUser toolkitUser, string connectionId);

	public void OnClientDisconnected(ToolkitUser toolkitUser, string connectionId);

	public Task<ToolkitMessage> SendDataBufferToClient(
		string connectionId,
		ToolkitMessage message,
		byte[] dataBuffer,
		TimeSpan? timeout = null
	);

	public Task SendToAllClients(ToolkitMessage message);

	public Task<ToolkitMessage> SendToClient(string connectionId, ToolkitMessage message, TimeSpan? timeout = null);

	public Task SendToClientNoResponse(string connectionId, ToolkitMessage message);
}

public interface IToolkitHubGroups
{
	public Task AddToGroup(string connectionId, string groupName);

	public Task RemoveFromGroup(string connectionId, string groupName);

	public Task SendToGroup(string groupName, ToolkitMessage message);
}

public class ToolkitHubServer(IHubContext<ToolkitHub> hubContext, IGenerator generator, IToolkitLogger logger)
	: IToolkitHubServer,
		IToolkitHubGroups
{
	private readonly ConcurrentDictionary<string, string> connections = new();
	private readonly ConcurrentDictionary<string, int> timedOutResponses = new();
	private readonly ConcurrentDictionary<string, TaskCompletionSource<ToolkitMessage>> waitingForResponses = new();

	public event ToolkitHubClientConnected ClientConnected;

	public event ToolkitHubClientDisconnected ClientDisconnected;

	public void ClientResponseDataBufferMessage(string sessionId, ToolkitMessage toolkitMessage, byte[] dataBuffer)
	{
		if (timedOutResponses.TryRemove(sessionId, out _))
		{
			return;
		}

		if (!waitingForResponses.TryRemove(sessionId, out var completionSource))
		{
			return;
		}

		logger.Debug(
			$"Got a client response for data buffer!!!!!!! | SessionId {sessionId} | {new JsonOperations().Serialize(toolkitMessage)}"
		);

		completionSource.TrySetResult(toolkitMessage);
	}

	public void ClientResponseMessage(string sessionId, ToolkitMessage toolkitMessage)
	{
		if (timedOutResponses.TryRemove(sessionId, out _))
		{
			return;
		}

		if (!waitingForResponses.TryRemove(sessionId, out var completionSource))
		{
			return;
		}

		logger.Debug(
			$"Got a client response!!!!!!! | SessionId {sessionId} | {new JsonOperations().Serialize(toolkitMessage)}"
		);

		completionSource.TrySetResult(toolkitMessage);
	}

	public List<string> GetConnections()
	{
		return connections.Keys.ToList();
	}

	public void OnClientConnected(ToolkitUser toolkitUser, string connectionId)
	{
		logger.Debug($"Connected | <{connectionId}>");

		connections.TryAdd(connectionId, connectionId);

		InvokeClientConnected(toolkitUser, connectionId);
	}

	public void OnClientDisconnected(ToolkitUser toolkitUser, string connectionId)
	{
		logger.Debug($"Client Disconnected | <{connectionId}>");

		connections.TryRemove(connectionId, out _);

		InvokeClientDisconnected(toolkitUser, connectionId);
	}

	public async Task<ToolkitMessage> SendDataBufferToClient(
		string connectionId,
		ToolkitMessage message,
		byte[] dataBuffer,
		TimeSpan? timeout = null
	)
	{
		var sessionId = generator.NewId();

		var completionSource = CreateResponseCompletionSource(sessionId);

		await hubContext
			.Clients.Client(connectionId)
			.SendAsync(
				ToolkitHubMethodNames.ServerDataBufferMessage,
				message.MessageType,
				sessionId,
				message.Data,
				dataBuffer
			);

		return await WaitForClientResponse(message, timeout, sessionId, completionSource);
	}

	public async Task AddToGroup(string connectionId, string groupName)
	{
		await hubContext.Groups.AddToGroupAsync(connectionId, groupName);
	}

	public async Task RemoveFromGroup(string connectionId, string groupName)
	{
		await hubContext.Groups.RemoveFromGroupAsync(connectionId, groupName);
	}

	public async Task SendToGroup(string groupName, ToolkitMessage message)
	{
		var sessionId = generator.NewId();

		await hubContext
			.Clients.Group(groupName)
			.SendAsync(ToolkitHubMethodNames.ServerOriginatedMessage, message.MessageType, sessionId, message.Data);
	}

	public async Task SendToAllClients(ToolkitMessage message)
	{
		var sessionId = generator.NewId();

		await hubContext
			.Clients.All.SendAsync(
				ToolkitHubMethodNames.ServerOriginatedMessage,
				message.MessageType,
				sessionId,
				message.Data
			);
	}

	public async Task<ToolkitMessage> SendToClient(
		string connectionId,
		ToolkitMessage message,
		TimeSpan? timeout = null
	)
	{
		var sessionId = generator.NewId();

		var completionSource = CreateResponseCompletionSource(sessionId);

		await SendMessageToClient(connectionId, message, sessionId);

		return await WaitForClientResponse(message, timeout, sessionId, completionSource);
	}

	public async Task SendToClientNoResponse(string connectionId, ToolkitMessage message)
	{
		await SendMessageToClient(connectionId, message, generator.NewId());
	}

	private Task InvokeClientConnected(ToolkitUser user, string connectionId)
	{
		return ClientConnected?.Invoke(user, connectionId) ?? Task.CompletedTask;
	}

	private Task InvokeClientDisconnected(ToolkitUser user, string connectionId)
	{
		return ClientDisconnected?.Invoke(user, connectionId) ?? Task.CompletedTask;
	}

	private async Task SendMessageToClient(string connectionId, ToolkitMessage message, string sessionId)
	{
		await hubContext
			.Clients.Client(connectionId)
			.SendAsync(
				ToolkitHubMethodNames.ServerOriginatedMessage,
				message.MessageType,
				sessionId,
				message.Data
			);
	}

	private TaskCompletionSource<ToolkitMessage> CreateResponseCompletionSource(string sessionId)
	{
		var completionSource = new TaskCompletionSource<ToolkitMessage>(TaskCreationOptions.RunContinuationsAsynchronously);

		waitingForResponses.TryAdd(sessionId, completionSource);

		return completionSource;
	}

	private async Task<ToolkitMessage> WaitForClientResponse(
		ToolkitMessage message,
		TimeSpan? timeout,
		string sessionId,
		TaskCompletionSource<ToolkitMessage> completionSource
	)
	{
		timeout ??= 30.Seconds();

		using var cancellationSource = new CancellationTokenSource(timeout.Value);
		using var cancellationRegistration = cancellationSource.Token.Register(() => completionSource.TrySetCanceled());

		try
		{
			return await completionSource.Task;
		}
		catch (OperationCanceledException)
		{
			timedOutResponses.TryAdd(sessionId, message.MessageType);
			waitingForResponses.TryRemove(sessionId, out _);

			throw new TimeoutException();
		}
	}
}
