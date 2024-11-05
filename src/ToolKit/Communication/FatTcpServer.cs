using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FatCat.Toolkit.Communication;

public abstract class FatTcpServer(IGenerator generator, IFatTcpLogger logger)
{
	protected readonly IFatTcpLogger logger = logger;
	protected int bufferSize;
	private CancellationTokenSource cancelSource;
	protected CancellationToken cancelToken;
	private Encoding encoding;
	private TcpListener listener;
	private ushort port;
	private Socket server;

	private ConcurrentDictionary<string, ClientConnection> Connections { get; } = new();

	public event TcpMessageReceived TcpMessageReceivedEvent;

	public void Dispose() { }

	public virtual void OnMessageReceived(byte[] data)
	{
		TcpMessageReceivedEvent?.Invoke(data);
	}

	public void Start(ushort serverPort, int receiveBufferSize = 1024)
	{
		Start(serverPort, Encoding.Unicode, receiveBufferSize);
	}

	public void Start(ushort serverPort, Encoding serverEncoding, int receiveBufferSize = 1024)
	{
		cancelSource = new CancellationTokenSource();
		cancelToken = cancelSource.Token;

		port = serverPort;
		bufferSize = receiveBufferSize;
		encoding = serverEncoding;

		Task.Factory.StartNew(ServerThread, TaskCreationOptions.LongRunning);
	}

	public void Stop()
	{
		Dispose();
	}

	protected abstract ClientConnection GetClientConnection(TcpClient client, string clientId);

	private async Task ServerThread()
	{
		await Task.CompletedTask;

		listener = new TcpListener(IPAddress.Any, port) { Server = { NoDelay = true } };
		server = listener.Server;

		SetUpKeepAlive();

		listener.Start();

		logger.WriteDebug($"Server listening on {port}");

		while (!cancelToken.IsCancellationRequested)
		{
			var client = await listener.AcceptTcpClientAsync(cancelToken);

			var clientId = generator.NewId();

			var clientConnection = GetClientConnection(client, clientId);

			Connections.TryAdd(clientId, clientConnection);

			clientConnection.Start();
		}
	}

	private void SetUpKeepAlive()
	{
		server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
		server.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 900);
		server.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 300);
		server.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, 15);
	}
}
