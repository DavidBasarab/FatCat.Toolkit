using Autofac;
using FatCat.Toolkit.Console;
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

		AddCors(builder);

		var applicationStartUp = new ApplicationStartUp();

		applicationStartUp.ConfigureServices(builder.Services);

		builder.Host.ConfigureContainer<ContainerBuilder>(
			(a, b) => SystemScope.Initialize(b, Settings.ContainerAssemblies)
		);

		var app = builder.Build();

		applicationStartUp.Configure(app, app.Environment, app.Services.GetRequiredService<ILoggerFactory>());

		app.UseCors(CorsPolicyName);

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

	private static void AddCors(WebApplicationBuilder builder)
	{
		if (Settings.AllowAllOrigins)
		{
			AddDefaultCors(builder);
		}
		else
		{
			AddCorsSettings(builder);
		}
	}

	private static void WriteMessage(string message)
	{
		Settings.OnLogEvent?.Invoke(message);
	}

	private static void AddCorsSettings(WebApplicationBuilder builder)
	{
		WriteMessage("Configuring CORS for the following origins:");

		foreach (var server in Settings.CorsSevers)
		{
			WriteMessage($" - {server}");
		}

		builder.Services.AddCors(options =>
		{
			options.AddPolicy(
				CorsPolicyName,
				policy =>
					policy
						.WithOrigins(Settings.CorsSevers.ToArray())
						.AllowAnyHeader()
						.AllowAnyMethod()
						.AllowCredentials()
			);
		});
	}

	private static void AddDefaultCors(WebApplicationBuilder builder)
	{
		builder.Services.AddCors(options =>
		{
			options.AddPolicy(
				CorsPolicyName,
				policy =>
					policy
						.AllowAnyOrigin() // Allows all domains
						.AllowAnyMethod() // Allows all HTTP methods
						.AllowAnyHeader()
			);
		});
	}
}
