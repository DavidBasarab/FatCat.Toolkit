#nullable enable
using FatCat.Toolkit.Web.Api.SignalR;
using Microsoft.AspNetCore.SignalR;

namespace FatCat.Toolkit.WebServer.SignalR;

public interface IToolkitHubConnectedClients
{
	public Task SendMessage(string clientId, int messageType, string data, string? sessionId = null);
}

public class ToolkitHubConnectedClients(IHubContext<ToolkitHub> hubContext, IGenerator generator)
	: IToolkitHubConnectedClients
{
	public async Task SendMessage(string clientId, int messageType, string data, string? sessionId = null)
	{
		var client = hubContext.Clients.Client(clientId);

		await client.SendCoreAsync(
									ToolkitHubMethodNames.ServerResponseMessage,
									[messageType, data, sessionId ?? generator.NewId()]
								);
	}
}
