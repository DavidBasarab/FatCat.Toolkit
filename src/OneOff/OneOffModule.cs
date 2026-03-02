using Autofac;
using FatCat.Toolkit.Logging;

namespace OneOff;

public class OneOffModule : Module
{
	protected override void Load(ContainerBuilder builder)
	{
		builder.RegisterType<OneOffLogger>().As<IToolkitLogger>().SingleInstance();
	}
}
