using Autofac;
using FatCat.Toolkit.Injection;
using Microsoft.Extensions.DependencyInjection;

namespace FatCat.Toolkit.WebServer;

public class ToolkitWebServerModule : Module, IToolkitModule
{
	protected override void Load(ContainerBuilder builder)
	{
		base.Load(builder);
	}

	public void Register(IServiceCollection services) { }
}
