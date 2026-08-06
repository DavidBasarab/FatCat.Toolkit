using System.Reflection;
using FatCat.Toolkit.Web.Api;
using FatCat.Toolkit.Web.Api.SignalR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebApplicationOptions = FatCat.Toolkit.Web.Api.WebApplicationOptions;

namespace FatCat.Toolkit.WebServer;

public class ToolkitWebApplicationSettings : EqualObject
{
	public bool AllowAllOrigins { get; set; }

	public string[] Args { get; set; } = [];

	public string BasePath { get; set; }

	public List<Assembly> ContainerAssemblies { get; set; } = new();

	public Action<ILoggingBuilder> ConfigureLogging { get; set; }

	public Action<IApplicationBuilder> ConfigureMiddleware { get; set; }

	public Action<IServiceCollection> ConfigureServices { get; set; }

	public List<string> CorsSevers { get; set; } = [];

	public Func<JwtBearerEvents> JwtBearerEvents { get; set; } = OAuthExtensions.GetTokenBearerEvents;

	public Action<string> OnLogEvent { get; set; }

	public Action OnWebApplicationStarted { get; set; }

	public WebApplicationOptions Options { get; set; } = WebApplicationOptions.Cors | WebApplicationOptions.HttpsRedirection;

	public bool SignalRRequireAuthorization { get; set; } = true;

	public string SignalRPath { get; set; } = "/api/events";

	public string StaticFileLocation { get; set; }

	public CertificationSettings TlsCertificate { get; set; }

	public IToolkitTokenParameters ToolkitTokenParameters { get; set; }

	public event ToolkitHubClientConnected ClientConnected;

	public event ToolkitHubDataBufferMessage ClientDataBufferMessage;

	public event ToolkitHubClientDisconnected ClientDisconnected;

	public event ToolkitHubMessage ClientMessage;

	public Task OnClientConnected(ToolkitUser user, string connectionId)
	{
		return ClientConnected?.Invoke(user, connectionId) ?? Task.CompletedTask;
	}

	public Task OnClientDisconnected(ToolkitUser user, string connectionId)
	{
		return ClientDisconnected?.Invoke(user, connectionId) ?? Task.CompletedTask;
	}

	public Task<string> OnClientHubMessage(ToolkitMessage message)
	{
		return ClientMessage?.Invoke(message) ?? Task.FromResult<string>(null);
	}

	public Task<string> OnOnClientDataBufferMessage(ToolkitMessage message, byte[] dataBuffer)
	{
		return ClientDataBufferMessage?.Invoke(message, dataBuffer) ?? Task.FromResult<string>(null);
	}
}
