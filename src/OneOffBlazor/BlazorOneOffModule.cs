using Autofac;

namespace OneOffBlazor;

public class BlazorOneOffModule : Module
{
	protected override void Load(ContainerBuilder builder)
	{
		builder.RegisterType<OtherTestingService>().As<ITestingService>().SingleInstance();
	}
}
