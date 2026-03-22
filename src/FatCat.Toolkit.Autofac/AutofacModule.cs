using Autofac;
using FatCat.Toolkit.Injection;
using Microsoft.Extensions.DependencyInjection;

namespace FatCat.Toolkit.Autofac;

public abstract class AutofacModule : Module, IToolkitModule, IAutofacOnlyModule
{
	void IToolkitModule.Register(IServiceCollection services)
	{
		throw new InvalidOperationException(
			$"{GetType().Name} extends AutofacModule and can only be used with Autofac. "
				+ "Call UseToolkitWithAutofac() in your application setup."
		);
	}
}
