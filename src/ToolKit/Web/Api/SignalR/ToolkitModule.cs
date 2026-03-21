using Autofac;
using FatCat.Toolkit.Caching;

namespace FatCat.Toolkit.Web.Api.SignalR;

public class ToolkitModule : Module
{
	protected override void Load(ContainerBuilder builder)
	{
		builder.RegisterGeneric(typeof(FatCatCache<>)).As(typeof(IFatCatCache<>)).SingleInstance();
	}
}
