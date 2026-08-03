using Autofac;
using FatCat.Toolkit.Injection;
using FatCat.Toolkit.Logging;
using FatCat.Toolkit.Web.Api.SignalR;
using FatCat.Toolkit.WebServer;
using FatCat.Toolkit.WebServer.SignalR;
using Microsoft.AspNetCore.SignalR;

namespace Tests.FatCat.Toolkit.WebServer.SignalR;

public class ToolkitHubTests
{
	private readonly ToolkitHub hub;
	private readonly ToolkitWebApplicationSettings settings;

	public ToolkitHubTests()
	{
		var hubServer = A.Fake<IToolkitHubServer>();
		var logger = A.Fake<IToolkitLogger>();

		var builder = new ContainerBuilder();

		builder.RegisterInstance(hubServer).As<IToolkitHubServer>();
		builder.RegisterInstance(logger).As<IToolkitLogger>();

		SystemScope.Container.LifetimeScope = builder.Build();

		settings = new ToolkitWebApplicationSettings();

		SetToolkitSettings(settings);

		hub = new ToolkitHub { Context = A.Fake<HubCallerContext>() };
	}

	[Fact]
	public void AwaitClientConnectedHookBeforeReturning()
	{
		var hookGate = new TaskCompletionSource();

		settings.ClientConnected += async (user, connectionId) =>
		{
			await hookGate.Task;
		};

		var connectedTask = hub.OnConnectedAsync();

		connectedTask.IsCompleted.Should().BeFalse();

		hookGate.SetResult();
	}

	[Fact]
	public void AwaitClientDisconnectedHookBeforeReturning()
	{
		var hookGate = new TaskCompletionSource();

		settings.ClientDisconnected += async (user, connectionId) =>
		{
			await hookGate.Task;
		};

		var disconnectedTask = hub.OnDisconnectedAsync(null);

		disconnectedTask.IsCompleted.Should().BeFalse();

		hookGate.SetResult();
	}

	[Fact]
	public async Task CompleteOnConnectedWithNoClientConnectedSubscriber()
	{
		var connectedTask = hub.OnConnectedAsync();

		await connectedTask;

		connectedTask.IsCompleted.Should().BeTrue();
	}

	[Fact]
	public async Task CompleteOnDisconnectedWithNoClientDisconnectedSubscriber()
	{
		var disconnectedTask = hub.OnDisconnectedAsync(null);

		await disconnectedTask;

		disconnectedTask.IsCompleted.Should().BeTrue();
	}

	private void SetToolkitSettings(ToolkitWebApplicationSettings settingsToApply)
	{
		var settingsProperty = typeof(ToolkitWebApplication).GetProperty(nameof(ToolkitWebApplication.Settings));

		settingsProperty.GetSetMethod(true).Invoke(null, new object[] { settingsToApply });
	}
}
