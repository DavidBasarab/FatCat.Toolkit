using Autofac;
using FatCat.Toolkit.Data.Mongo;

namespace SampleDocker;

public class SampleDockerModule : Module
{
	protected override void Load(ContainerBuilder builder)
	{
		builder.RegisterType<SampleConnectionInformation>().As<IMongoConnectionInformation>();
	}
}
