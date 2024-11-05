using System.Net;
using System.Text.Json.Serialization;
using Autofac;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.Extensions;
using FatCat.Toolkit.Injection;
using FatCat.Toolkit.Logging;
using FatCat.Toolkit.WebServer.Injection.Helpers;
using FatCat.Toolkit.WebServer.SignalR;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using WebApplicationOptions = FatCat.Toolkit.Web.Api.WebApplicationOptions;

namespace FatCat.Toolkit.WebServer;

internal sealed class ApplicationStartUp
{
	public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
	{
		ServicePointManager.ServerCertificateValidationCallback = (_, _, _, _) => true;

		app.Use(CaptureMiddlewareExceptions);

		app.UseFileServer();

		SetUpStaticFiles(app);

		app.UseRouting();

		if (ToolkitWebApplication.IsOptionSet(WebApplicationOptions.Authentication))
		{
			app.UseAuthentication();
			app.UseAuthorization();
		}

		app.UseEndpoints(endpoints => endpoints.MapControllers());

		app.Use(async (context, next) => await next.Invoke());

		SystemScope.Container.LifetimeScope = app.ApplicationServices.GetAutofacRoot();

		SetUpSignalR(app);

		if (ToolkitWebApplication.IsOptionSet(WebApplicationOptions.HttpsRedirection))
		{
			app.UseHttpsRedirection();
		}

		if (ToolkitWebApplication.IsOptionSet(WebApplicationOptions.Cors))
		{
			app.UseCors();
		}

		app.UseAuthorization();
	}

	public void ConfigureContainer(ContainerBuilder builder)
	{
		SystemScope.Initialize(builder, ToolkitWebApplication.Settings.ContainerAssemblies);
	}

	public void ConfigureServices(IServiceCollection services)
	{
		try
		{
			// Add services to the container.
			services.AddControllers();

			services.AddEndpointsApiExplorer();

			services.AddCors(options => options.AddDefaultPolicy(p => p.AllowAnyOrigin()));

			services.AddHttpContextAccessor();

			ConfigureControllers(services);

			AddAuthentication(services);

			services.AddSignalR(options =>
			{
				options.MaximumReceiveMessageSize = int.MaxValue;
				options.EnableDetailedErrors = true;
			});

			services.AddLogging(options =>
			{
				options.ClearProviders();
			});
		}
		catch (Exception ex)
		{
			if (SystemScope.Container.TryResolve<IToolkitLogger>(out var logger))
			{
				logger!.Exception(ex);
			}
		}
	}

	private void AddAuthentication(IServiceCollection services)
	{
		if (ToolkitWebApplication.Settings.Options.IsFlagNotSet(WebApplicationOptions.Authentication))
		{
			return;
		}

		if (ToolkitWebApplication.Settings.ToolkitTokenParameters == null)
		{
			throw new NullReferenceException(nameof(ToolkitWebApplication.Settings.ToolkitTokenParameters));
		}

		var authenticationBuilder = services
			.AddAuthentication(options =>
			{
				options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
				options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
			})
			.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);

		authenticationBuilder.AddJwtBearer(options =>
		{
			options.RequireHttpsMetadata = false;
			options.SaveToken = true;

			var toolkitTokenParameters = ToolkitWebApplication.Settings.ToolkitTokenParameters!;

			options.TokenValidationParameters = toolkitTokenParameters.Get();

			options.TokenHandlers.Add(new FatCatTokenHandler());

			options.Events = ToolkitWebApplication.Settings.JwtBearerEvents();
		});

		services.AddAuthorization(options =>
		{
			options.AddPermissionsPolicies();
		});
	}

	private async Task CaptureMiddlewareExceptions(HttpContext context, Func<Task> next)
	{
		try
		{
			await next().ConfigureAwait(false);
		}
		catch (TaskCanceledException)
		{
			var displayUrl = context.Request.GetDisplayUrl();

			if (SystemScope.Container.TryResolve<IToolkitLogger>(out var logger))
			{
				logger!.Information($"Could not complete call to {displayUrl}");
			}
		}
		catch (Exception e)
		{
			var displayUrl = context.Request.GetDisplayUrl();

			if (SystemScope.Container.TryResolve<IToolkitLogger>(out var logger))
			{
				logger!.Warning($"Error calling {displayUrl}");
				logger!.Exception(e);
			}

			throw;
		}
	}

	private void ConfigureControllers(IServiceCollection services)
	{
		services
			.AddControllers(config =>
			{
				if (ToolkitWebApplication.Settings.Options.IsFlagSet(WebApplicationOptions.Authentication))
				{
					var policy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
						.RequireAuthenticatedUser()
						.Build();

					config.Filters.Add(new AuthorizeFilter(policy));
				}
			})
			.AddJsonOptions(opts =>
			{
				var enumConverter = new JsonStringEnumConverter();
				opts.JsonSerializerOptions.Converters.Add(enumConverter);
			});

		services
			.AddMvc()
			.ConfigureApplicationPartManager(p =>
			{
				foreach (var containerAssembly in ToolkitWebApplication.Settings.ContainerAssemblies)
				{
					ConsoleLog.WriteCyan($"Adding Assembly Part <{containerAssembly.FullName}>");

					p.ApplicationParts.Add(new AssemblyPart(containerAssembly));
				}
			});
	}

	private void SetUpSignalR(IApplicationBuilder app)
	{
		if (ToolkitWebApplication.Settings.Options.IsFlagNotSet(WebApplicationOptions.SignalR))
		{
			return;
		}

		app.UseEndpoints(endpoints =>
		{
			var endpointOption = endpoints.MapHub<ToolkitHub>(ToolkitWebApplication.Settings.SignalRPath);

			if (ToolkitWebApplication.Settings.Options.IsFlagSet(WebApplicationOptions.Authentication))
			{
				endpointOption.RequireAuthorization();
			}
		});
	}

	private void SetUpStaticFiles(IApplicationBuilder app)
	{
		if (ToolkitWebApplication.Settings.Options.IsFlagNotSet(WebApplicationOptions.FileSystem))
		{
			return;
		}

		if (ToolkitWebApplication.Settings.StaticFileLocation == null)
		{
			return;
		}

		var physicalFileProvider = new PhysicalFileProvider(ToolkitWebApplication.Settings.StaticFileLocation);
		var options = new DefaultFilesOptions { FileProvider = physicalFileProvider };

		app.UseDefaultFiles(options);
		app.UseStaticFiles(new StaticFileOptions { FileProvider = physicalFileProvider });
	}

	public class ValidateModelAttribute : ActionFilterAttribute
	{
		public override void OnActionExecuting(ActionExecutingContext actionContext)
		{
			if (actionContext.ModelState.IsValid == false)
			{
				actionContext.Result = new BadRequestObjectResult(actionContext.ModelState);
			}
		}
	}
}
