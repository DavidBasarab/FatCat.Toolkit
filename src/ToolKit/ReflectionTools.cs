#nullable enable
using System.Reflection;
using Fasterflect;

namespace FatCat.Toolkit;

public interface IReflectionTools
{
	public TAttribute? FindAttributeOnType<TAttribute>(Type type)
		where TAttribute : Attribute;

	public List<Type> FindTypesImplementing<TTypeImplementing>(List<Assembly> assemblies);

	public List<Assembly> GetDomainAssemblies();
}

public class ReflectionTools : IReflectionTools
{
	public TAttribute? FindAttributeOnType<TAttribute>(Type type)
		where TAttribute : Attribute
	{
		return type.GetCustomAttribute<TAttribute>();
	}

	public List<Type> FindTypesImplementing<TTypeImplementing>(List<Assembly> assemblies)
	{
		var foundTypes = new List<Type>();

		foreach (var assembly in assemblies)
		{
			var typesImplementing = assembly.TypesImplementing<TTypeImplementing>();

			foundTypes.AddRange(typesImplementing);
		}

		return foundTypes;
	}

	public List<Assembly> GetDomainAssemblies()
	{
		return AppDomain.CurrentDomain.GetAssemblies().ToList();
	}
}
