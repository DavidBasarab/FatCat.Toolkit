using System.Security.Cryptography.X509Certificates;
using FatCat.Toolkit.Logging;
using Thread = FatCat.Toolkit.Threading.Thread;

namespace FatCat.Toolkit.Communication;

public interface ITcpFactory
{
	IFatTcpClient CreateOpenTcpClient();

	IFatTcpServer CreateOpenTcpServer();

	IFatTcpClient CreateSecureTcpClient(X509Certificate certificate);

	IFatTcpServer CreateSecureTcpServer(X509Certificate certificate);
}

public class TcpFactory : ITcpFactory
{
	public IFatTcpClient CreateOpenTcpClient()
	{
		return new OpenFatTcpClient(new ConsoleFatTcpLogger(), new Thread(new ToolkitLogger()));
	}

	public IFatTcpServer CreateOpenTcpServer()
	{
		return new OpenFatTcpServer(new Generator(), new ConsoleFatTcpLogger());
	}

	public IFatTcpClient CreateSecureTcpClient(X509Certificate certificate)
	{
		return new SecureFatTcpClient(certificate, new ConsoleFatTcpLogger(), new Thread(new ToolkitLogger()));
	}

	public IFatTcpServer CreateSecureTcpServer(X509Certificate certificate)
	{
		return new SecureFatTcpServer(certificate, new Generator(), new ConsoleFatTcpLogger());
	}
}
