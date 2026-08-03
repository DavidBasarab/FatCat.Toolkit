#nullable enable
using System.Collections.Concurrent;
using FatCat.Toolkit.Injection;
using Microsoft.AspNetCore.Http.Connections.Client;

namespace FatCat.Toolkit.Web.Api.SignalR;

public interface IToolkitHubClientFactory : IAsyncDisposable
{
	public Task<IToolkitHubClientConnection> ConnectToClient(
		string hubUrl,
		Action<HttpConnectionOptions>? configureOptions = null,
		bool automaticReconnect = false
	);

	public void RemoveHubFromConnections(string hubUrl);

	public Task<ConnectionResult> TryToConnectToClient(
		string hubUrl,
		Action? onConnectionLost = null,
		Action<HttpConnectionOptions>? configureOptions = null,
		bool automaticReconnect = false
	);
}

public class ToolkitHubClientFactory(ISystemScope scope) : IToolkitHubClientFactory
{
	private readonly ConcurrentDictionary<string, IToolkitHubClientConnection> connections = new();

	public async Task<IToolkitHubClientConnection> ConnectToClient(
		string hubUrl,
		Action<HttpConnectionOptions>? configureOptions = null,
		bool automaticReconnect = false
	)
	{
		if (connections.TryGetValue(hubUrl, out var connection))
		{
			return connection;
		}

		connection = scope.Resolve<IToolkitHubClientConnection>();

		await connection.Connect(hubUrl, configureOptions: configureOptions, automaticReconnect: automaticReconnect);

		connections.TryAdd(hubUrl, connection);

		return connection;
	}

	public async ValueTask DisposeAsync()
	{
		foreach (var connection in connections.Values)
		{
			await connection.DisposeAsync();
		}
	}

	public void RemoveHubFromConnections(string hubUrl)
	{
		connections.TryRemove(hubUrl, out _);
	}

	public async Task<ConnectionResult> TryToConnectToClient(
		string hubUrl,
		Action? onConnectionLost = null,
		Action<HttpConnectionOptions>? configureOptions = null,
		bool automaticReconnect = false
	)
	{
		if (connections.TryGetValue(hubUrl, out var connection))
		{
			return new ConnectionResult(true, connection);
		}

		connection = scope.Resolve<IToolkitHubClientConnection>();

		var result = await connection.TryToConnect(
			hubUrl,
			() =>
			{
				RemoveHubFromConnections(hubUrl);

				onConnectionLost?.Invoke();
			},
			configureOptions,
			automaticReconnect
		);

		if (!result)
		{
			return new ConnectionResult(false);
		}

		connections.TryAdd(hubUrl, connection);

		return new ConnectionResult(true, connection);
	}
}
