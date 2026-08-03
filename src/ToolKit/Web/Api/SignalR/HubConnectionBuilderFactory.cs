using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;

namespace FatCat.Toolkit.Web.Api.SignalR;

public interface IHubConnectionBuilderFactory
{
	public IHubConnectionBuilder Create(string hubUrl, Action<HttpConnectionOptions> configureOptions);
}

[ExcludeFromCodeCoverage(
	Justification = "Direct wrapper over HubConnectionBuilder — no business logic, faked in consuming tests."
)]
public class HubConnectionBuilderFactory : IHubConnectionBuilderFactory
{
	public IHubConnectionBuilder Create(string hubUrl, Action<HttpConnectionOptions> configureOptions)
	{
		return new HubConnectionBuilder().WithUrl(hubUrl, configureOptions);
	}
}
