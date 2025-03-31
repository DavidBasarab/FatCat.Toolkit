using Autofac;
using FatCat.Toolkit.Extensions;
using FatCat.Toolkit.Injection;
using FatCat.Toolkit.Threading;
using Humanizer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebApplicationOptions = FatCat.Toolkit.Web.Api.WebApplicationOptions;

namespace FatCat.Toolkit.WebServer;

public static class ToolkitWebApplication
{
	public const string CorsPolicyName = "AllOrigins";

	public static ToolkitWebApplicationSettings Settings { get; private set; } = null!;

	public static bool IsOptionSet(WebApplicationOptions option)
	{
		return Settings.Options.IsFlagSet(option);
	}

	public static void Run(ToolkitWebApplicationSettings settings)
	{
		Settings = settings;

		settings.ContainerAssemblies.Add(typeof(ToolkitWebServerModule).Assembly);

		SystemScope.ContainerAssemblies = settings.ContainerAssemblies;

		var builder = WebApplication.CreateBuilder(settings.Args);

		builder.Host.UseServiceProviderFactory(new ToolkitServiceProviderFactory(new AutofacOptions()));

		builder.Services.AddHttpContextAccessor();

		var applicationStartUp = new ApplicationStartUp();

		applicationStartUp.ConfigureServices(builder.Services);

		builder.Host.ConfigureContainer<ContainerBuilder>(
			(a, b) => SystemScope.Initialize(b, Settings.ContainerAssemblies)
		);

		var app = builder.Build();

		builder.Services.AddCors(policy =>
		{
			policy.AddPolicy(
				CorsPolicyName,
				builder => builder.SetIsOriginAllowedToAllowWildcardSubdomains().AllowAnyOrigin()
			);
		});

		applicationStartUp.Configure(app, app.Environment, app.Services.GetRequiredService<ILoggerFactory>());

		if (Settings.BasePath.IsNotNullOrEmpty())
		{
			app.UsePathBase(Settings.BasePath);
		}

		app.MapControllers();

		var thread = SystemScope.Container.Resolve<IThread>();

		thread.Run(async () =>
		{
			await thread.Sleep(300.Milliseconds());

			Settings.OnWebApplicationStarted?.Invoke();
		});

		app.Run();
	}
}
