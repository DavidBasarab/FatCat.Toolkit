using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ProxySpike.Workers.ProxyPlaying;

public class ProxyStartUp
{
	public IConfiguration Configuration { get; }

	public ProxyStartUp(IConfiguration configuration)
	{
		// Default configuration comes from AppSettings.json file in project/output
		Configuration = configuration;
	}

	// This method gets called by the runtime. Use this method to configure the HTTP request
	// pipeline that handles requests
	public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
	{
		if (env.IsDevelopment())
		{
			app.UseDeveloperExceptionPage();
		}

		// Enable endpoint routing, required for the reverse proxy
		app.UseRouting();

		// Register the reverse proxy routes
		app.UseEndpoints(endpoints =>
		{
			endpoints.MapReverseProxy();
		});
	}

	// This method gets called by the runtime. Use this method to add capabilities to
	// the web application via services in the DI container.
	public void ConfigureServices(IServiceCollection services)
	{
		// Add the reverse proxy capability to the server
		// var proxyBuilder = services.AddReverseProxy();

		// Initialize the reverse proxy from the "ReverseProxy" section of configuration
		// proxyBuilder.LoadFromConfig(Configuration.GetSection("ReverseProxy"));

		services
			.AddReverseProxy()
			.LoadFromMemory(ProxyRouteConfig.GetRoutes(), ProxyClusterConfig.GetClusters());
	}
}
