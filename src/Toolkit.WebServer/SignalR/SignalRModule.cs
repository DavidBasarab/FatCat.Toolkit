using Autofac;
using FatCat.Toolkit.Injection;
using FatCat.Toolkit.Web.Api.SignalR;
using Microsoft.Extensions.DependencyInjection;
using FatCat.Toolkit;

namespace FatCat.Toolkit.WebServer.SignalR;

public class SignalRModule : Module, IToolkitModule
{
	protected override void Load(ContainerBuilder builder)
	{
		builder.RegisterType<ToolkitHubClientFactory>().As<IToolkitHubClientFactory>().SingleInstance();

		builder.RegisterType<ToolkitHubServer>().As<IToolkitHubServer>().SingleInstance();
	}

	public void Register(IServiceCollection services)
	{
		services.AddSingleton<IGenerator, Generator>();

		services.AddSingleton<IToolkitHubClientFactory, ToolkitHubClientFactory>();

		services.AddSingleton<IToolkitHubServer, ToolkitHubServer>();
	}
}
