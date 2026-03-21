using Autofac;
using FatCat.Toolkit.Data.Mongo;

namespace OneOff.MongoProof;

public class LocalMongoModule : Module
{
	protected override void Load(ContainerBuilder builder)
	{
		builder.RegisterType<LocalMongoConnectionInformation>().As<IMongoConnectionInformation>();
	}
}
