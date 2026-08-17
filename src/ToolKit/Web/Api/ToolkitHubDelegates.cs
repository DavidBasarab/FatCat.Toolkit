#nullable enable
using FatCat.Toolkit.Web.Api.SignalR;

namespace FatCat.Toolkit.Web.Api;

public delegate Task<string?> ToolkitHubMessage(ToolkitMessage message);

public delegate Task<string?> ToolkitHubDataBufferMessage(ToolkitMessage message, byte[] dataBuffer);

public delegate Task ToolkitHubClientConnected(ToolkitUser user, string connectionId);

public delegate Task ToolkitHubClientDisconnected(ToolkitUser user, string connectionId);

public delegate Task ToolkitHubReconnecting(Exception? exception);

public delegate Task ToolkitHubReconnected(string? connectionId);

public delegate Task ToolkitHubConnectionLost();
