using System.IO.Abstractions;
using System.Reflection;
using Autofac;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.Extensions;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable CS8767

namespace FatCat.Toolkit.Injection;

public interface ISystemScope : IToolkitServiceFactory { }

public class SystemScope : ISystemScope
{
	private static readonly List<Assembly> defaultAssemblies = new ReflectionTools().GetDomainAssemblies();

	private static readonly Lazy<SystemScope> instance = new(() => new SystemScope());

	public static SystemScope Container
	{
		get { return instance.Value; }
	}

	public static List<Assembly> ContainerAssemblies { get; set; } = defaultAssemblies;

	// Autofac path — unchanged
	public static void Initialize(ContainerBuilder builder, ScopeOptions options = ScopeOptions.None)
	{
		Initialize(builder, defaultAssemblies.ToList(), options);
	}

	public static void Initialize(ContainerBuilder builder, List<Assembly> assemblies, ScopeOptions options = ScopeOptions.None)
	{
		EnsureAssembly(assemblies, typeof(IFileSystem).Assembly);
		EnsureAssembly(assemblies, typeof(SystemScope).Assembly);

		foreach (var assembly in assemblies)
		{
			ConsoleLog.Write($"    Using assembly {assembly.FullName}");
		}

		Container.BuildAutofacContainer(builder, assemblies);

		if (options.IsFlagSet(ScopeOptions.SetLifetimeScope))
		{
			ConsoleLog.WriteMagenta("Setting lifetime scope");

			SetServiceProvider(new AutofacServiceProviderAdapter(builder.Build()));
		}
	}

	// MS DI path — new
	public static void Initialize(IServiceCollection services, List<Assembly> assemblies)
	{
		EnsureAssembly(assemblies, typeof(IFileSystem).Assembly);
		EnsureAssembly(assemblies, typeof(SystemScope).Assembly);

		ContainerAssemblies = assemblies;

		foreach (var assembly in assemblies)
		{
			ConsoleLog.Write($"    Using assembly {assembly.FullName}");
		}

		foreach (var module in DiscoverModules(assemblies))
		{
			module.Register(services);
		}

		services.Scan(scan =>
			scan.FromAssemblies(assemblies)
				.AddClasses(c => c.Where(t => t.GetConstructors().Any() && !typeof(IToolkitModule).IsAssignableFrom(t)))
				.UsingRegistrationStrategy(Scrutor.RegistrationStrategy.Skip)
				.AsImplementedInterfaces()
				.WithScopedLifetime()
		);

		services.AddSingleton<IToolkitServiceFactory>(Container);
		services.AddSingleton<ISystemScope>(Container);
	}

	public static void SetServiceProvider(IServiceProvider serviceProvider)
	{
		Container.provider = serviceProvider;
	}

	public ILifetimeScope LifetimeScope
	{
		get
		{
			if (provider is AutofacServiceProviderAdapter adapter)
			{
				return adapter.LifetimeScope;
			}

			return null;
		}
		set { SetServiceProvider(new AutofacServiceProviderAdapter(value)); }
	}

	public List<Assembly> SystemAssemblies
	{
		get { return ContainerAssemblies; }
	}

	private IServiceProvider provider;

	private SystemScope() { }

	public TItem Resolve<TItem>()
		where TItem : class
	{
		return provider.GetRequiredService<TItem>();
	}

	public object Resolve(Type type)
	{
		return provider.GetRequiredService(type);
	}

	public bool TryResolve(Type type, out object instance)
	{
		if (provider == null)
		{
			instance = null;

			return false;
		}

		instance = provider.GetService(type);

		return instance != null;
	}

	public bool TryResolve<TItem>(out TItem instance)
		where TItem : class
	{
		if (provider == null)
		{
			instance = null;

			return false;
		}

		instance = provider.GetService<TItem>();

		return instance != null;
	}

	private void BuildAutofacContainer(ContainerBuilder builder, List<Assembly> assemblies)
	{
		ContainerAssemblies = assemblies;

		builder.RegisterAssemblyModules(ContainerAssemblies.ToArray());

		builder.RegisterInstance(this).As<ISystemScope>();

		builder
			.RegisterAssemblyTypes(ContainerAssemblies.ToArray())
			.AsImplementedInterfaces()
			.HasPublicConstructor()
			.PublicOnly()
			.PreserveExistingDefaults()
			.Except<ISystemScope>()
			.AsSelf();
	}

	private static IEnumerable<IToolkitModule> DiscoverModules(List<Assembly> assemblies)
	{
		return assemblies
			.SelectMany(a => a.GetTypes())
			.Where(t =>
				typeof(IToolkitModule).IsAssignableFrom(t)
				&& t.IsClass
				&& !t.IsAbstract
				&& !typeof(IAutofacOnlyModule).IsAssignableFrom(t)
			)
			.Select(Activator.CreateInstance)
			.Cast<IToolkitModule>();
	}

	private static void EnsureAssembly(List<Assembly> assemblies, Assembly assemblyToEnsure)
	{
		if (!assemblies.Contains(assemblyToEnsure))
		{
			assemblies.Insert(0, assemblyToEnsure);
		}
	}

	private sealed class AutofacServiceProviderAdapter(ILifetimeScope lifetimeScope) : IServiceProvider
	{
		public ILifetimeScope LifetimeScope { get; } = lifetimeScope;

		public object GetService(Type serviceType)
		{
			return LifetimeScope.ResolveOptional(serviceType);
		}
	}
}
