using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace FatCat.Toolkit.Communication;

public class SecureFatTcpServer(X509Certificate certificate, IGenerator generator, IFatTcpLogger logger)
	: FatTcpServer(generator, logger),
		IFatTcpServer
{
	protected override ClientConnection GetClientConnection(TcpClient client, string clientId)
	{
		return new SecureClientConnection(certificate, this, client, clientId, bufferSize, logger, cancelToken);
	}
}
