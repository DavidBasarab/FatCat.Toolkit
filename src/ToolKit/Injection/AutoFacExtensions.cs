using System.Collections.Concurrent;
using System.Reflection;
using Autofac;
using Autofac.Builder;
using Autofac.Features.Scanning;

namespace FatCat.Toolkit.Injection;

public static class AutoFacExtensions
{
	private static readonly ConcurrentDictionary<Type, ConstructorInfo[]> defaultPublicConstructorsCache = new();

	public static IRegistrationBuilder<TLimit, TScanningActivatorData, TRegistrationStyle> HasPublicConstructor<
		TLimit,
		TScanningActivatorData,
		TRegistrationStyle
	>(this IRegistrationBuilder<TLimit, TScanningActivatorData, TRegistrationStyle> registration)
		where TScanningActivatorData : ScanningActivatorData
	{
		if (registration == null)
		{
			throw new ArgumentNullException(nameof(registration));
		}

		return registration.Where(TypeHasPublicConstructor);
	}

	/// <summary>
	///  Register a delegate as a component.
	/// </summary>
	/// <typeparam name="T">The type of the instance.</typeparam>
	/// <param name="builder">Container builder.</param>
	/// <param name="delegate">The delegate to register.</param>
	/// <returns>Registration builder allowing the registration to be configured.</returns>
	public static IRegistrationBuilder<T, SimpleActivatorData, SingleRegistrationStyle> Register<T>(
		this ContainerBuilder builder,
		Func<ISystemScope, T> @delegate
	)
		where T : notnull
	{
		return builder.Register(
			(IComponentContext ctx) =>
			{
				var haiScope = ctx.Resolve<ISystemScope>();

				return @delegate(haiScope);
			}
		);
	}

	private static bool TypeHasPublicConstructor(Type type)
	{
		var items = defaultPublicConstructorsCache.GetOrAdd(type, t => t.GetDeclaredPublicConstructors());

		return items.Length > 0;
	}
}
