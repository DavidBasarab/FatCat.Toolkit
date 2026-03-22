using Autofac;
using FatCat.Toolkit;
using FatCat.Toolkit.Autofac;

namespace OneOffLib;

public class OneOffModule : AutofacModule
{
	protected override void Load(ContainerBuilder builder)
	{
		builder.RegisterType<TestApplicationTools>().As<IApplicationTools>();
	}
}
