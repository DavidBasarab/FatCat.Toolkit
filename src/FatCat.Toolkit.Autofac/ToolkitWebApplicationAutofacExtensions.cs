using Autofac;
using FatCat.Toolkit.Injection;
using FatCat.Toolkit.Injection.Helpers;
using FatCat.Toolkit.WebServer;
using Microsoft.AspNetCore.Builder;

namespace FatCat.Toolkit.Autofac;

public static class ToolkitWebApplicationAutofacExtensions
{
	public static ToolkitWebApplicationSettings UseAutofac(this ToolkitWebApplicationSettings settings)
	{
		settings.ConfigureDiForBuilder = builder =>
		{
			builder.Host.UseServiceProviderFactory(new ToolkitServiceProviderFactory(new AutofacOptions()));

			builder.Host.ConfigureContainer<ContainerBuilder>(
				(_, cb) => SystemScope.Initialize(cb, settings.ContainerAssemblies)
			);
		};

		settings.ConfigureDiForApp = app =>
		{
			SystemScope.Container.LifetimeScope = app.Services.GetAutofacRoot();
		};

		return settings;
	}
}
