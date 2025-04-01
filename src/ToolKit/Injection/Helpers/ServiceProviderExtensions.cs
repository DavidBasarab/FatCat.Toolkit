using System.Globalization;
using Autofac;
using Autofac.Extensions.DependencyInjection;

namespace FatCat.Toolkit.Injection.Helpers;

/// <summary>
///  Extension methods for use with the <see cref="AutofacServiceProvider" />.
/// </summary>
public static class ServiceProviderExtensions
{
	/// <summary>
	///  Tries to cast the instance of <see cref="ILifetimeScope" /> from <see cref="AutofacServiceProvider" /> when possible.
	/// </summary>
	/// <param name="serviceProvider">The instance of <see cref="IServiceProvider" />.</param>
	/// <exception cref="InvalidOperationException">
	///  Throws an <see cref="InvalidOperationException" /> when instance of
	///  <see cref="IServiceProvider" /> can't be assigned to <see cref="AutofacServiceProvider" />.
	/// </exception>
	/// <returns>Returns the instance of <see cref="ILifetimeScope" /> exposed by <see cref="AutofacServiceProvider" />.</returns>
	public static ILifetimeScope GetAutofacRoot(this IServiceProvider serviceProvider)
	{
		if (!(serviceProvider is AutofacServiceProvider autofacServiceProvider))
		{
			throw new InvalidOperationException(
				String.Format(
					CultureInfo.CurrentCulture,
					ServiceProviderExtensionsResources.WrongProviderType,
					serviceProvider?.GetType()
				)
			);
		}

		return autofacServiceProvider.LifetimeScope;
	}
}
