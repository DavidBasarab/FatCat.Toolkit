using Autofac;
using FatCat.Toolkit.Caching;
using FatCat.Toolkit.Injection;
using Microsoft.Extensions.DependencyInjection;

namespace FatCat.Toolkit.Web.Api.SignalR;

public class ToolkitModule : Module, IToolkitModule
{
	protected override void Load(ContainerBuilder builder)
	{
		builder.RegisterGeneric(typeof(FatCatCache<>)).As(typeof(IFatCatCache<>)).SingleInstance();
	}

	public void Register(IServiceCollection services)
	{
		services.AddSingleton(typeof(IFatCatCache<>), typeof(FatCatCache<>));
	}
}
