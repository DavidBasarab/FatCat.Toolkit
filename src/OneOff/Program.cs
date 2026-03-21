using Autofac;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.Injection;
using FatCat.Toolkit.WebServer;
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
			SystemScope.Initialize(
				new ContainerBuilder(),
				[
					typeof(OneOffModule).Assembly,
					typeof(Program).Assembly,
					typeof(ConsoleLog).Assembly,
					typeof(ToolkitWebServerModule).Assembly,
				],
				ScopeOptions.SetLifetimeScope
			);

			var worker = SystemScope.Container.Resolve<MongoProofWorker>();

			await worker.DoWork();
		}
		catch (Exception ex)
		{
			ConsoleLog.WriteException(ex);
		}
	}
}
