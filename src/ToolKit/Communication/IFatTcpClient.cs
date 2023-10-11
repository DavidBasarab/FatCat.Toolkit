namespace FatCat.Toolkit.Communication;

public interface IFatTcpClient
{
	bool Connected { get; }

	bool Reconnect { get; set; }

	TimeSpan ReconnectDelay { get; set; }

	event TcpMessageReceived TcpMessageReceivedEvent;

	Task Connect(string host, ushort port, int bufferSize = 1024, CancellationToken cancellationToken = default);

	void Disconnect();

	Task Send(byte[] bytes);
}
