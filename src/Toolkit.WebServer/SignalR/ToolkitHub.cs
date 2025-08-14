#nullable enable
using FatCat.Toolkit.Injection;
using FatCat.Toolkit.Logging;
using FatCat.Toolkit.Web.Api.SignalR;
using Microsoft.AspNetCore.SignalR;

namespace FatCat.Toolkit.WebServer.SignalR;

public class ToolkitHub : Hub
{
	private IToolkitHubServer HubServer
	{
		get => SystemScope.Container.Resolve<IToolkitHubServer>();
	}

	private IToolkitLogger Logger
	{
		get => SystemScope.Container.Resolve<IToolkitLogger>();
	}

	public async Task ClientDataBufferMessage(int messageType, string sessionId, string data, byte[] dataBuffer)
	{
		await Task.CompletedTask;

		Logger.Debug(
			$"Got DataBuffer Message | MessageType <{messageType}> | SessionId <{sessionId}> | Data <{data}> | Buffer <{dataBuffer.Length}> | ConnectionId <{Context.ConnectionId}>"
		);

		var toolkitMessage = new ToolkitMessage
		{
			Data = data,
			ConnectionId = Context.ConnectionId,
			User = GetUser(),
			MessageType = messageType,
		};

		var responseMessage = await ToolkitWebApplication.Settings.OnOnClientDataBufferMessage(
			toolkitMessage,
			dataBuffer
		);

		Logger.Debug(
			$"Response for message | <{responseMessage}> | Sending to ConnectionId {Context.ConnectionId}"
		);

		await Clients
			.Client(Context.ConnectionId)
			.SendAsync(ToolkitHubMethodNames.ServerResponseMessage, messageType, sessionId, responseMessage);

		Logger.Debug(
			$"Done sending response for message | <{responseMessage}> | SessionId <{sessionId}> | ConnectionId {Context.ConnectionId}"
		);
	}

	public async Task ClientMessage(int messageType, string sessionId, string data)
	{
		Logger.Debug($"Got Message | MessageType <{messageType}> | SessionId <{sessionId}> | Data <{data}>");

		var toolkitMessage = new ToolkitMessage
		{
			Data = data,
			ConnectionId = Context.ConnectionId,
			User = GetUser(),
			MessageType = messageType,
		};

		var responseMessage = await ToolkitWebApplication.Settings.OnClientHubMessage(toolkitMessage);

		Logger.Debug($"Response for message | <{responseMessage}>");

		await Clients
			.Client(Context.ConnectionId)
			.SendAsync(ToolkitHubMethodNames.ServerResponseMessage, messageType, sessionId, responseMessage);
	}

	public async Task ClientResponseMessage(int messageType, string sessionId, string data)
	{
		await Task.CompletedTask;

		var toolkitMessage = new ToolkitMessage
		{
			Data = data,
			ConnectionId = Context.ConnectionId,
			User = GetUser(),
			MessageType = messageType,
		};

		HubServer.ClientResponseMessage(sessionId, toolkitMessage);
	}

	public override Task OnConnectedAsync()
	{
		try
		{
			var username = Context!.User?.Identity?.Name;

			Logger.Debug($"Connection made | ConnectionId <{Context!.ConnectionId}> | Username <{username}>");

			var toolkitUser = ToolkitUser.Create(Context.User);

			HubServer.OnClientConnected(toolkitUser, Context.ConnectionId);

			ToolkitWebApplication.Settings.OnClientConnected(toolkitUser, Context.ConnectionId);
		}
		catch (Exception ex)
		{
			Logger.Exception(ex);
		}

		return base.OnConnectedAsync();
	}

	public override Task OnDisconnectedAsync(Exception? exception)
	{
		try
		{
			var toolkitUser = ToolkitUser.Create(Context.User);

			HubServer.OnClientDisconnected(toolkitUser, Context.ConnectionId);

			ToolkitWebApplication.Settings.OnClientDisconnected(toolkitUser, Context.ConnectionId);
		}
		catch (Exception ex)
		{
			Logger.Exception(ex);
		}

		return base.OnDisconnectedAsync(exception);
	}

	private ToolkitUser GetUser()
	{
		return Context.User == null ? null : ToolkitUser.Create(Context.User);
	}
}
