using Autofac;
using FatCat.Toolkit.Data.Mongo;
using FatCat.Toolkit.Injection;
using Microsoft.Extensions.DependencyInjection;

namespace FatCat.Toolkit.Data;

public class DataModule : Module, IToolkitModule
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

	public void Register(IServiceCollection services)
	{
		services.AddSingleton<IMongoConnection>(new MongoConnection(SystemScope.ContainerAssemblies));

		services.AddScoped(typeof(IMongoRepository<>), typeof(MongoRepository<>));

		services.AddScoped<IMongoConnectionInformation, EnvironmentConnectionInformation>();
	}
}
