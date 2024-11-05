using Autofac;
using FatCat.Toolkit.WebServer.Injection.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace FatCat.Toolkit.WebServer.Injection;

public class AutofacOptions
{
	public bool ValidateOnBuild { get; set; } = false;

	internal Action<ContainerBuilder> ConfigureContainerDelegate { get; set; } = builder => { };

	public AutofacOptions ConfigureContainer(Action<ContainerBuilder> configureContainerDelegate)
	{
		ConfigureContainerDelegate = configureContainerDelegate;
		return this;
	}
}

public class ToolkitServiceProviderFactory(AutofacOptions autofacOptions)
	: IServiceProviderFactory<ContainerBuilder>
{
	public ContainerBuilder CreateBuilder(IServiceCollection services)
	{
		if (autofacOptions.ValidateOnBuild)
		{
			services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
		}

		var containerBuilder = new ContainerBuilder();

		containerBuilder.Populate(services);

		autofacOptions.ConfigureContainerDelegate(containerBuilder);

		return containerBuilder;
	}

	public IServiceProvider CreateServiceProvider(ContainerBuilder containerBuilder)
	{
		ArgumentNullException.ThrowIfNull(containerBuilder);

		var container = containerBuilder.Build();

		return new AutofacServiceProvider(container);
	}
}
