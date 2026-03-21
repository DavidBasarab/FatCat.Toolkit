using Autofac;
using FatCat.Toolkit.Data.Mongo;
using FatCat.Toolkit.Injection;

namespace FatCat.Toolkit.Data;

public class DataModule : Module
{
	protected override void Load(ContainerBuilder builder)
	{
		builder
			.RegisterInstance(new MongoConnection(SystemScope.ContainerAssemblies.ToList()))
			.As<IMongoConnection>()
			.SingleInstance();

		builder.RegisterGeneric(typeof(MongoRepository<>)).As(typeof(IMongoRepository<>));

		builder.RegisterType<EnvironmentConnectionInformation>().As<IMongoConnectionInformation>();
	}
}
