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
	event ToolkitHubClientConnected ClientConnected;

	event ToolkitHubClientDisconnected ClientDisconnected;

	void ClientResponseDataBufferMessage(string sessionId, ToolkitMessage toolkitMessage, byte[] dataBuffer);

	void ClientResponseMessage(string sessionId, ToolkitMessage toolkitMessage);

	List<string> GetConnections();

	void OnClientConnected(ToolkitUser toolkitUser, string connectionId);

	void OnClientDisconnected(ToolkitUser toolkitUser, string connectionId);

	Task<ToolkitMessage> SendDataBufferToClient(
		string connectionId,
		ToolkitMessage message,
		byte[] dataBuffer,
		TimeSpan? timeout = null
	);

	Task SendToAllClients(ToolkitMessage message);

	Task<ToolkitMessage> SendToClient(string connectionId, ToolkitMessage message, TimeSpan? timeout = null);

	Task SendToClientNoResponse(string connectionId, ToolkitMessage message);
}

public class ToolkitHubServer(IHubContext<ToolkitHub> hubContext, IGenerator generator, IToolkitLogger logger)
	: IToolkitHubServer
{
	private readonly ConcurrentDictionary<string, string> connections = new();
	private readonly ConcurrentDictionary<string, ToolkitMessage> responses = new();
	private readonly ConcurrentDictionary<string, int> timedOutResponses = new();
	private readonly ConcurrentDictionary<string, ToolkitMessage> waitingForResponses = new();

	public event ToolkitHubClientConnected ClientConnected;

	public event ToolkitHubClientDisconnected ClientDisconnected;

	public void ClientResponseDataBufferMessage(string sessionId, ToolkitMessage toolkitMessage, byte[] dataBuffer)
	{
		if (timedOutResponses.TryRemove(sessionId, out _))
		{
			return;
		}

		if (!waitingForResponses.TryRemove(sessionId, out _))
		{
			return;
		}

		logger.Debug(
			$"Got a client response for data buffer!!!!!!! | SessionId {sessionId} | {new JsonOperations().Serialize(toolkitMessage)}"
		);

		responses.TryAdd(sessionId, toolkitMessage);
	}

	public void ClientResponseMessage(string sessionId, ToolkitMessage toolkitMessage)
	{
		if (timedOutResponses.TryRemove(sessionId, out _))
		{
			return;
		}

		if (!waitingForResponses.TryRemove(sessionId, out _))
		{
			return;
		}

		logger.Debug(
			$"Got a client response!!!!!!! | SessionId {sessionId} | {new JsonOperations().Serialize(toolkitMessage)}"
		);

		responses.TryAdd(sessionId, toolkitMessage);
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

		waitingForResponses.TryAdd(sessionId, message);

		await hubContext
			.Clients.Client(connectionId)
			.SendAsync(
				ToolkitHubMethodNames.ServerDataBufferMessage,
				message.MessageType,
				sessionId,
				message.Data,
				dataBuffer
			);

		return await WaitForClientResponse(message, timeout, sessionId);
	}

	public Task SendToAllClients(ToolkitMessage message)
	{
		throw new NotImplementedException();
	}

	public async Task<ToolkitMessage> SendToClient(
		string connectionId,
		ToolkitMessage message,
		TimeSpan? timeout = null
	)
	{
		var sessionId = generator.NewId();

		waitingForResponses.TryAdd(sessionId, message);

		await SendMessageToClient(connectionId, message, sessionId);

		return await WaitForClientResponse(message, timeout, sessionId);
	}

	public async Task SendToClientNoResponse(string connectionId, ToolkitMessage message)
	{
		await SendMessageToClient(connectionId, message, generator.NewId());
	}

	private Task InvokeClientConnected(ToolkitUser user, string connectionId)
	{
		return ClientConnected?.Invoke(user, connectionId);
	}

	private Task InvokeClientDisconnected(ToolkitUser user, string connectionId)
	{
		return ClientDisconnected?.Invoke(user, connectionId);
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

	private async Task<ToolkitMessage> WaitForClientResponse(
		ToolkitMessage message,
		TimeSpan? timeout,
		string sessionId
	)
	{
		timeout ??= 30.Seconds();

		var startTime = DateTime.UtcNow;

		while (true)
		{
			if (responses.TryRemove(sessionId, out var response))
			{
				return response;
			}

			if (DateTime.UtcNow - startTime > timeout)
			{
				timedOutResponses.TryAdd(sessionId, message.MessageType);

				throw new TimeoutException();
			}

			await Task.Delay(35);
		}
	}
}
