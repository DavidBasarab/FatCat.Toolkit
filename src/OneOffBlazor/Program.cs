using Autofac;
using Autofac.Extensions.DependencyInjection;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.Injection;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace OneOffBlazor;

public static class Program
{
	public static async Task Main(string[] args)
	{
		ConsoleLog.ConsoleAccess = new NoColorConsoleAccess();

		var builder = WebAssemblyHostBuilder.CreateDefault(args);

		builder.ConfigureContainer(new AutofacServiceProviderFactory(ConfigureContainer));

		builder.RootComponents.Add<App>("#app");
		builder.RootComponents.Add<HeadOutlet>("head::after");

		builder.Services.AddScoped(
									sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) }
								);
		
		

		await builder.Build().RunAsync();
	}

	private static void ConfigureContainer(ContainerBuilder builder)
	{
		ConsoleLog.Write("Initialize System Scope...");

		SystemScope.Initialize(
								builder,
								[
									typeof(OneOffModule).Assembly,
									typeof(Program).Assembly,
									typeof(ConsoleLog).Assembly,
								]
							);

		ConsoleLog.Write("System Scope initialized.");
	}
}