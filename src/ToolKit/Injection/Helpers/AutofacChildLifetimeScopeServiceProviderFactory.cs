using Autofac;
using Microsoft.Extensions.DependencyInjection;

namespace FatCat.Toolkit.Injection.Helpers;

/// <summary>
///  A factory for creating a <see cref="IServiceProvider" /> that wraps a child <see cref="ILifetimeScope" /> created from
///  an existing parent <see cref="ILifetimeScope" />.
/// </summary>
public class AutofacChildLifetimeScopeServiceProviderFactory
	: IServiceProviderFactory<AutofacChildLifetimeScopeConfigurationAdapter>
{
	private static readonly Action<ContainerBuilder> FallbackConfigurationAction = builder => { };
	private readonly Action<ContainerBuilder> _containerConfigurationAction;
	private readonly ILifetimeScope _rootLifetimeScope;

	/// <summary>
	///  Initializes a new instance of the <see cref="AutofacChildLifetimeScopeServiceProviderFactory" /> class.
	/// </summary>
	/// <param name="rootLifetimeScopeAccessor">A function to retrieve the root <see cref="ILifetimeScope" /> instance.</param>
	/// <param name="configurationAction">
	///  Action on a <see cref="ContainerBuilder" /> that adds component registrations to the
	///  container.
	/// </param>
	public AutofacChildLifetimeScopeServiceProviderFactory(
		Func<ILifetimeScope> rootLifetimeScopeAccessor,
		Action<ContainerBuilder> configurationAction = null
	)
	{
		if (rootLifetimeScopeAccessor == null)
		{
			throw new ArgumentNullException(nameof(rootLifetimeScopeAccessor));
		}

		_rootLifetimeScope = rootLifetimeScopeAccessor();
		_containerConfigurationAction = configurationAction ?? FallbackConfigurationAction;
	}

	/// <summary>
	///  Initializes a new instance of the <see cref="AutofacChildLifetimeScopeServiceProviderFactory" /> class.
	/// </summary>
	/// <param name="rootLifetimeScope">The root <see cref="ILifetimeScope" /> instance.</param>
	/// <param name="configurationAction">
	///  Action on a <see cref="ContainerBuilder" /> that adds component registrations to the
	///  container.
	/// </param>
	public AutofacChildLifetimeScopeServiceProviderFactory(
		ILifetimeScope rootLifetimeScope,
		Action<ContainerBuilder> configurationAction = null
	)
	{
		_rootLifetimeScope = rootLifetimeScope ?? throw new ArgumentNullException(nameof(rootLifetimeScope));
		_containerConfigurationAction = configurationAction ?? FallbackConfigurationAction;
	}

	/// <summary>
	///  Creates a container builder from an <see cref="IServiceCollection" />.
	/// </summary>
	/// <param name="services">The collection of services.</param>
	/// <returns>A container builder that can be used to create an <see cref="IServiceProvider" />.</returns>
	public AutofacChildLifetimeScopeConfigurationAdapter CreateBuilder(IServiceCollection services)
	{
		var actions = new AutofacChildLifetimeScopeConfigurationAdapter();

		actions.Add(builder => builder.Populate(services));
		actions.Add(builder => _containerConfigurationAction(builder));

		return actions;
	}

	/// <summary>
	///  Creates an <see cref="IServiceProvider" /> from the container builder.
	/// </summary>
	/// <param name="configurationAdapter">
	///  The adapter holding configuration applied to <see cref="ContainerBuilder" />
	///  creating the <see cref="IServiceProvider" />.
	/// </param>
	/// <returns>An <see cref="IServiceProvider" />.</returns>
	public IServiceProvider CreateServiceProvider(
		AutofacChildLifetimeScopeConfigurationAdapter configurationAdapter
	)
	{
		if (configurationAdapter == null)
		{
			throw new ArgumentNullException(nameof(configurationAdapter));
		}

		var scope = _rootLifetimeScope.BeginLifetimeScope(scopeBuilder =>
		{
			foreach (var action in configurationAdapter.ConfigurationActions)
			{
				action(scopeBuilder);
			}
		});

		return new AutofacServiceProvider(scope);
	}
}
