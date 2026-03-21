#nullable enable
namespace FatCat.Toolkit.Web.Api.SignalR;

public class ConnectionResult(bool connected, IToolkitHubClientConnection? connection = null)
{
	public bool Connected { get; set; } = connected;

	public IToolkitHubClientConnection? Connection { get; set; } = connection;
}
