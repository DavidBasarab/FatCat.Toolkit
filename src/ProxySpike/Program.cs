using Autofac;
using CommandLine;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.Injection;
using FatCat.Toolkit.Threading;
using ProxySpike.Options;
using ProxySpike.Workers;
using ProxySpike.Workers.ProxyPlaying;

namespace ProxySpike;

public static class Program
{
	private static IConsoleUtilities consoleUtilities;
	private static IThread thread;

	public static void Main(params string[] args)
	{
		try
		{
			SystemScope.Initialize(new ContainerBuilder(), ScopeOptions.SetLifetimeScope);

			consoleUtilities = SystemScope.Container.Resolve<IConsoleUtilities>();
			thread = SystemScope.Container.Resolve<IThread>();

			ConsoleLog.Write("Starting Proxy Spike Program");

			Parser
				.Default.ParseArguments<ServerOptions, ProxyOptions>(args)
				.WithParsed<ProxyOptions>(options =>
				{
					thread.Run(async () =>
					{
						var worker = SystemScope.Container.Resolve<ProxyWorker>();

						await worker.DoWork(options);
					});
				})
				.WithParsed<ServerOptions>(options =>
				{
					thread.Run(async () =>
					{
						var worker = SystemScope.Container.Resolve<WebServerWorker>();

						await worker.DoWork(options);
					});
				});

			consoleUtilities.WaitForExit();
		}
		catch (Exception ex)
		{
			ConsoleLog.WriteException(ex);
		}
	}
}
