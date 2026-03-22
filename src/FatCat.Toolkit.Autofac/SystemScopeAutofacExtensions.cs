using System.Reflection;
using Autofac;
using FatCat.Toolkit.Injection;

namespace FatCat.Toolkit.Autofac;

public static class SystemScopeAutofacExtensions
{
	public static void InitializeWithAutofac(
		this SystemScope scope,
		ContainerBuilder builder,
		List<Assembly> assemblies,
		ScopeOptions options = ScopeOptions.None
	)
	{
		SystemScope.Initialize(builder, assemblies, options);
	}

	public static void SetLifetimeScope(this SystemScope scope, ILifetimeScope lifetimeScope)
	{
		SystemScope.Container.LifetimeScope = lifetimeScope;
	}
}
