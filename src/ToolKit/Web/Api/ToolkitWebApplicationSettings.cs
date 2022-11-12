#nullable enable
using System.Reflection;
using FatCat.Toolkit.Web.Api.SignalR;

namespace FatCat.Toolkit.Web.Api;

public class ToolkitWebApplicationSettings : EqualObject
{
	public string? CertificationLocation { get; set; }

	public string? CertificationPassword { get; set; }

	public List<Assembly> ContainerAssemblies { get; set; } = new();

	public List<Uri> CorsUri { get; set; } = new();

	public Action? OnWebApplicationStarted { get; set; }

	public WebApplicationOptions Options { get; set; }

	public ushort Port { get; set; } = 443;

	public string SignalRPath { get; set; } = "/api/events";

	public string? StaticFileLocation { get; set; }

	public event ToolkitHubDataBufferMessage? ClientDataBufferMessage;

	public event ToolkitHubMessage? ClientMessage;

	public Task<string?> OnOnClientDataBufferMessage(ToolkitMessage message, byte[] dataBuffer) => ClientDataBufferMessage?.Invoke(message, dataBuffer)!;

	public Task<string?> OnClientHubMessage(ToolkitMessage message) => ClientMessage?.Invoke(message)!;
}