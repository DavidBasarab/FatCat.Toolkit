using FatCat.Toolkit.Threading;

namespace FatCat.Toolkit.Communication;

public class OpenFatTcpClient(IFatTcpLogger logger, IThread thread) : FatTcpClient(logger, thread), IFatTcpClient
{
	protected override Stream GetStream()
	{
		return tcpClient.GetStream();
	}
}
