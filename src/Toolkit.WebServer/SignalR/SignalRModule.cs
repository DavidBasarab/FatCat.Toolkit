using Autofac;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.Web.Api.SignalR;

namespace FatCat.Toolkit.WebServer.SignalR;

public class SignalRModule : Module
{
	protected override void Load(ContainerBuilder builder)
	{
		builder.RegisterType<ToolkitHubClientFactory>().As<IToolkitHubClientFactory>().SingleInstance();

		builder.RegisterType<ToolkitHubServer>().As<IToolkitHubServer>().SingleInstance();
	}
}
