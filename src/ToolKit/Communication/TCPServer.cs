#nullable enable
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using FatCat.Toolkit.Console;

namespace FatCat.Toolkit.Communication;

public delegate void TcpMessageReceived(byte[] data);

public interface ITcpServer : IDisposable
{
	event TcpMessageReceived OnMessageReceived;

	void Start(ushort port, int receiveBufferSize = 1024);

	void Start(ushort port, Encoding encoding, int receiveBufferSize = 1024);

	void Stop();
}

public class TcpServer : ITcpServer
{
	private readonly IGenerator generator;
	private int bufferSize;
	private CancellationTokenSource cancelSource;
	private CancellationToken cancelToken;
	private Encoding encoding;
	private TcpListener listener;
	private ushort port;
	private Socket server;

	internal ConcurrentDictionary<string, ClientConnection> Connections { get; set; } = new();

	public TcpServer(IGenerator generator)
	{
		this.generator = generator;
	}

	public event TcpMessageReceived OnMessageReceived;

	public void Dispose() { }

	public virtual void OnOnMessageReceived(byte[] data)
	{
		OnMessageReceived?.Invoke(data);
	}

	public void Start(ushort port, int receiveBufferSize = 1024)
	{
		Start(port, Encoding.Unicode, receiveBufferSize);
	}

	public void Start(ushort port, Encoding encoding, int receiveBufferSize = 1024)
	{
		cancelSource = new CancellationTokenSource();
		cancelToken = cancelSource.Token;

		this.port = port;
		bufferSize = receiveBufferSize;
		this.encoding = encoding;

		Task.Factory.StartNew(ServerThread, TaskCreationOptions.LongRunning);
	}

	public void Stop()
	{
		Dispose();
	}

	private async Task ServerThread()
	{
		await Task.CompletedTask;

		listener = new TcpListener(IPAddress.Any, port) { Server = { NoDelay = true } };
		server = listener.Server;

		SetUpKeepAlive();

		listener.Start();

		ConsoleLog.WriteMagenta($"Server listening on {port}");

		while (!cancelToken.IsCancellationRequested)
		{
			var client = await listener.AcceptTcpClientAsync(cancelToken);

			var clientId = generator.NewId();

			var clientConnection = new ClientConnection(this, client, clientId, bufferSize, cancelToken);

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
