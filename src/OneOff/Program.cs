using Autofac;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.Injection;
using FatCat.Toolkit.Logging;
using FatCat.Toolkit.Messaging;
using FatCat.Toolkit.WebServer;
using OneOff.Old;
using OneOffLib;
using Thread = FatCat.Toolkit.Threading.Thread;

namespace OneOff;

public static class Program
{
	public static string[] Args { get; set; }

	public static async Task Main(params string[] args)
	{
		await Task.CompletedTask;

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

			// RunServer(args);

			// var worker = SystemScope.Container.Resolve<RetryWorker>();
			//
			// await worker.DoWork();

			// var consoleUtilities = SystemScope.Container.Resolve<IConsoleUtilities>();
			//
			// consoleUtilities.WaitForExit();

			var messenger = SystemScope.Container.Resolve<IMessenger>();

			ConsoleLog.WriteCyan($"Type of messenger {messenger.GetType().FullName}");
		}
		catch (Exception ex)
		{
			ConsoleLog.WriteException(ex);
		}
	}

	private static void RunServer(string[] args)
	{
		new ServerWorker(new Thread(new ToolkitLogger())).DoWork(args);
	}
}
