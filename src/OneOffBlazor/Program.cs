using Autofac;
using Autofac.Extensions.DependencyInjection;
using FatCat.Toolkit.Console;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace OneOffBlazor;

public static class Program
{
	public static async Task Main(string[] args)
	{
		ConsoleLog.ConsoleAccess = new BlazorConsoleAccess();

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
		Console.WriteLine("Configuring Autofac container...");
		ConsoleLog.WriteMagenta("Trying to see if this will work");
	}
}
