using Autofac;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.Injection;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OneOffBlazor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var containerBuilder = new ContainerBuilder();

SystemScope.Initialize(
	containerBuilder,
	[typeof(OneOffModule).Assembly, typeof(Program).Assembly, typeof(ConsoleLog).Assembly,],
	ScopeOptions.SetLifetimeScope
);

builder.ConfigureContainer(new ToolkitServiceProviderFactory(new AutofacOptions()));

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
