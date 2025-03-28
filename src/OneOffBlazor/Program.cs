using Autofac;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OneOffBlazor;

public class Program
{
	public static async Task Main(string[] args)
	{
		var builder = WebAssemblyHostBuilder.CreateDefault(args);

		// builder.ConfigureContainer(new AutofacServiceProviderFactory(ConfigureContainer));

		builder.RootComponents.Add<App>("#app");
		builder.RootComponents.Add<HeadOutlet>("head::after");

		builder.Services.AddScoped(
			sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) }
		);

		await builder.Build().RunAsync();
	}
}
