using System.Text;

namespace FatCat.Toolkit.Communication;

public interface IFatTcpClient
{
	public bool Connected { get; }

	public bool Reconnect { get; set; }

	public TimeSpan ReconnectDelay { get; set; }

	public event TcpMessageReceived TcpMessageReceivedEvent;

	public Task Connect(string host, ushort port, int bufferSize = 1024, CancellationToken cancellationToken = default);

	public void Disconnect();

	public void Send(byte[] bytes);

	public void Send(string message, Encoding encoding = null);
}
