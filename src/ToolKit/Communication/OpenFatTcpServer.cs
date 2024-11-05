using System.Net.Sockets;

namespace FatCat.Toolkit.Communication;

public class OpenFatTcpServer(IGenerator generator, IFatTcpLogger logger)
	: FatTcpServer(generator, logger),
		IFatTcpServer
{
	protected override ClientConnection GetClientConnection(TcpClient client, string clientId)
	{
		return new OpenClientConnection(this, client, clientId, bufferSize, logger, cancelToken);
	}
}
