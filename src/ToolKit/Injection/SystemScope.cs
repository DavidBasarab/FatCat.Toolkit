using System.IO.Abstractions;
using System.Reflection;
using Autofac;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.Extensions;

#pragma warning disable CS8767

namespace FatCat.Toolkit.Injection;

public interface ISystemScope
{
	List<Assembly> SystemAssemblies { get; }

	TItem Resolve<TItem>()
		where TItem : class;

	object Resolve(Type type);

	bool TryResolve(Type type, out object instance);

	bool TryResolve<TItem>(out TItem instance)
		where TItem : class;
}

public class SystemScope : ISystemScope
{
	private static readonly List<Assembly> defaultAssemblies = new ReflectionTools().GetDomainAssemblies();

	private static readonly Lazy<SystemScope> instance = new(() => new SystemScope());

	public static SystemScope Container
	{
		get => instance.Value;
	}

	public static List<Assembly> ContainerAssemblies { get; set; } = defaultAssemblies;

	public static void Initialize(ContainerBuilder builder, ScopeOptions options = ScopeOptions.None)
	{
		Initialize(builder, defaultAssemblies.ToList(), options);
	}

	public static void Initialize(
		ContainerBuilder builder,
		List<Assembly> assemblies,
		ScopeOptions options = ScopeOptions.None
	)
	{
		EnsureAssembly(assemblies, typeof(IFileSystem).Assembly);
		EnsureAssembly(assemblies, typeof(SystemScope).Assembly);

		foreach (var assembly in assemblies)
		{
			ConsoleLog.Write($"    Using assembly {assembly.FullName}");
		}

		Container.BuildContainer(builder, assemblies);

		if (options.IsFlagSet(ScopeOptions.SetLifetimeScope))
		{
			ConsoleLog.WriteMagenta("Setting lifetime scope");

			Container.LifetimeScope = builder.Build();
		}
	}

	public ILifetimeScope LifetimeScope { get; set; }

	public List<Assembly> SystemAssemblies
	{
		get => ContainerAssemblies;
	}

	private SystemScope() { }

	public TItem Resolve<TItem>()
		where TItem : class
	{
		return LifetimeScope.Resolve<TItem>();
	}

	public object Resolve(Type type)
	{
		return LifetimeScope.Resolve(type);
	}

	public bool TryResolve(Type type, out object instance)
	{
		if (LifetimeScope == null)
		{
			instance = null;
			return false;
		}

		return LifetimeScope.TryResolve(type, out instance);
	}

	public bool TryResolve<TItem>(out TItem instance)
		where TItem : class
	{
		if (LifetimeScope != null)
		{
			return LifetimeScope.TryResolve(out instance);
		}

		instance = null;

		return false;
	}

	private void BuildContainer(ContainerBuilder builder, List<Assembly> assemblies)
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

	private static void EnsureAssembly(List<Assembly> assemblies, Assembly assemblyToEnsure)
	{
		if (!assemblies.Contains(assemblyToEnsure))
		{
			assemblies.Insert(0, assemblyToEnsure);
		}
	}
}
