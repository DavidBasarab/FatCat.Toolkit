using System.Reflection;
using Autofac;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.Injection;

namespace OneOffToolkitOnly;

public static class Program
{
	private const int WebPort = 14555;

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
				new List<Assembly> { typeof(Program).Assembly, typeof(ConsoleLog).Assembly },
				ScopeOptions.SetLifetimeScope
			);

			// ConnectClient(args);

			var worker = SystemScope.Container.Resolve<TestCallerIssueWorker>();

			await worker.DoWork(args);

			// var consoleUtilities = SystemScope.Container.Resolve<IConsoleUtilities>();
			//
			// consoleUtilities.WaitForExit();
		}
		catch (Exception ex)
		{
			ConsoleLog.WriteException(ex);
		}
	}

	private static void ConnectClient(string[] args)
	{
		var consoleUtilities = SystemScope.Container.Resolve<IConsoleUtilities>();

		var clientWorker = SystemScope.Container.Resolve<ClientWorker>();

		clientWorker.DoWork(args);

		consoleUtilities.WaitForExit();
	}
}
