using System.Reflection;

namespace FatCat.Toolkit.Injection;

public interface IToolkitServiceFactory
{
	List<Assembly> SystemAssemblies { get; }

	TItem Resolve<TItem>()
		where TItem : class;

	object Resolve(Type type);

	bool TryResolve(Type type, out object instance);

	bool TryResolve<TItem>(out TItem instance)
		where TItem : class;
}
