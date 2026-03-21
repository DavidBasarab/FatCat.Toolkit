using System.Text;

namespace FatCat.Toolkit.Communication;

public interface IFatTcpServer : IDisposable
{
	public event TcpMessageReceived TcpMessageReceivedEvent;

	public void OnMessageReceived(byte[] bytesReceived);

	public void Start(ushort port, int receiveBufferSize = 1024);

	public void Start(ushort port, Encoding encoding, int receiveBufferSize = 1024);

	public void Stop();
}
