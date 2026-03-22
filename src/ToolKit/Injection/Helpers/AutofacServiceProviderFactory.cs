using Autofac;
using Microsoft.Extensions.DependencyInjection;

namespace FatCat.Toolkit.Injection.Helpers;

/// <summary>
///  A factory for creating a <see cref="ContainerBuilder" /> and an <see cref="IServiceProvider" />.
/// </summary>
public class AutofacServiceProviderFactory(Action<ContainerBuilder> configurationAction = null)
	: IServiceProviderFactory<ContainerBuilder>
{
	private readonly Action<ContainerBuilder> configurationAction = configurationAction ?? (_ => { });

	/// <summary>
	///  Creates a container builder from an <see cref="IServiceCollection" />.
	/// </summary>
	/// <param name="services">The collection of services.</param>
	/// <returns>A container builder that can be used to create an <see cref="IServiceProvider" />.</returns>
	public ContainerBuilder CreateBuilder(IServiceCollection services)
	{
		var builder = new ContainerBuilder();

		builder.Populate(services);

		configurationAction(builder);

		return builder;
	}

	/// <summary>
	///  Creates an <see cref="IServiceProvider" /> from the container builder.
	/// </summary>
	/// <param name="containerBuilder">The container builder.</param>
	/// <returns>An <see cref="IServiceProvider" />.</returns>
	public IServiceProvider CreateServiceProvider(ContainerBuilder containerBuilder)
	{
		if (containerBuilder == null)
		{
			throw new ArgumentNullException(nameof(containerBuilder));
		}

		var container = containerBuilder.Build();

		if (container.Resolve<ISystemScope>() is SystemScope systemScope)
		{
			systemScope.LifetimeScope ??= container;
		}

		return new AutofacServiceProvider(container);
	}
}
