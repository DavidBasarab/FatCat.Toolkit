using Autofac;
using FatCat.Toolkit;

namespace OneOffLib;

public class OneOffModule : Module
{
	protected override void Load(ContainerBuilder builder)
	{
		builder.RegisterType<TestApplicationTools>().As<IApplicationTools>();
	}
}
