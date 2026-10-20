using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;

namespace FatCat.Toolkit.WebServer;

public static class ForwardedHeadersExtensions
{
	public static IServiceCollection AddToolkitForwardedHeaders(
		this IServiceCollection services,
		ToolkitWebApplicationSettings settings
	)
	{
		return services.Configure<ForwardedHeadersOptions>(options =>
		{
			ApplyToolkitDefaults(options);

			settings.ConfigureForwardedHeaders?.Invoke(options);
		});
	}

	public static IApplicationBuilder UseToolkitForwardedHeaders(
		this IApplicationBuilder app,
		ToolkitWebApplicationSettings settings
	)
	{
		if (settings.UseForwardedHeaders)
		{
			app.UseForwardedHeaders();
		}

		if (settings.StripUnconsumedForwardedFor)
		{
			app.UseMiddleware<StripUnconsumedForwardedForMiddleware>();
		}

		return app;
	}

	private static void ApplyToolkitDefaults(ForwardedHeadersOptions options)
	{
		options.ForwardedHeaders =
			ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;

		// Azure front-ends/proxies often have dynamic IPs; this tells ASP.NET Core
		// to accept forwarded headers from any proxy. Only do this if your app is
		// NOT directly exposed to the internet except through Azure's proxy/ingress.
		// Consumers that are directly exposed restrict this via ConfigureForwardedHeaders.
		options.KnownIPNetworks.Clear();
		options.KnownProxies.Clear();

		// Helps prevent header chain abuse; usually 1 is correct for App Service/ACA.
		options.ForwardLimit = 1;
	}
}
