using System.Reflection;
using Autofac;
using FatCat.Toolkit.Autofac;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.Data.Mongo;
using FatCat.Toolkit.Injection;
using FatCat.Toolkit.WebServer;
using Microsoft.Extensions.DependencyInjection;
using OneOff.MongoProof;
using OneOffLib;

namespace OneOff;

public static class Program
{
	public static string[] Args { get; set; }

	public static async Task Main(params string[] args)
	{
		Args = args;

		ConsoleLog.LogCallerInformation = true;

		try
		{
			await RunAutofacMongoTest();
			await RunMsDiMongoTest();
			await RunAutofacWebTest();
			await RunMsDiWebTest();
		}
		catch (Exception ex)
		{
			ConsoleLog.WriteException(ex);
		}
	}

	private static List<Assembly> GetAssemblies()
	{
		return
		[
			typeof(OneOffModule).Assembly,
			typeof(Program).Assembly,
			typeof(ConsoleLog).Assembly,
			typeof(ToolkitWebServerModule).Assembly,
		];
	}

	private static async Task RunAutofacMongoTest()
	{
		ConsoleLog.WriteMagenta("=== AUTOFAC MONGO TEST ===");

		SystemScope.Initialize(
			new ContainerBuilder(),
			GetAssemblies(),
			ScopeOptions.SetLifetimeScope
		);

		var mongoWorker = SystemScope.Container.Resolve<MongoProofWorker>();

		await mongoWorker.DoWork();
	}

	private static async Task RunMsDiMongoTest()
	{
		ConsoleLog.WriteMagenta("=== MS DI MONGO TEST ===");

		var assemblies = GetAssemblies();
		var services = new ServiceCollection();

		SystemScope.Initialize(services, assemblies);

		services.AddScoped<IMongoConnectionInformation, LocalMongoConnectionInformation>();

		using var serviceProvider = services.BuildServiceProvider();

		SystemScope.SetServiceProvider(serviceProvider);

		using var scope = serviceProvider.CreateScope();

		var repository = scope.ServiceProvider.GetRequiredService<IMongoRepository<ProofItem>>();
		var mongoWorker = new MongoProofWorker(repository);

		await mongoWorker.DoWork();
	}

	private static async Task RunAutofacWebTest()
	{
		ConsoleLog.WriteMagenta("=== AUTOFAC WEB SERVER TEST ===");

		var worker = new WebServerProofWorker();

		await worker.DoWork("Autofac", "http://localhost:14666", settings => settings.UseAutofac());
	}

	private static async Task RunMsDiWebTest()
	{
		ConsoleLog.WriteMagenta("=== MS DI WEB SERVER TEST ===");

		var worker = new WebServerProofWorker();

		await worker.DoWork("MS DI", "http://localhost:14667");
	}
}
