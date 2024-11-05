using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.Threading;
using Humanizer;

namespace FatCat.Toolkit.Communication;

public abstract class FatTcpClient(IFatTcpLogger logger, IThread thread)
{
	private byte[] buffer;
	private int bufferSize;
	private CancellationTokenSource cancelSource;
	private CancellationToken cancelToken;
	private string host;

	private readonly ConcurrentBag<byte[]> messagesToSend = [];
	private ushort port;
	private Stream stream;
	protected TcpClient tcpClient;

	public bool Connected { get; private set; }

	public bool Reconnect { get; set; } = false;

	public TimeSpan ReconnectDelay { get; set; } = 2.Seconds();

	public event TcpMessageReceived TcpMessageReceivedEvent;

	public async Task Connect(
		string host,
		ushort port,
		int bufferSize = 1024,
		CancellationToken cancellationToken = default
	)
	{
		this.host = host;
		this.port = port;
		this.bufferSize = bufferSize;
		cancelToken = cancellationToken;

		await MakeConnection();
	}

	public void Disconnect()
	{
		try
		{
			cancelSource.Cancel();
		}
		catch
		{ // ignored
		}

		ShutdownSocket();
	}

	public void Send(byte[] bytes)
	{
		if (!Connected)
		{
			return;
		}

		messagesToSend.Add(bytes);
	}

	public void Send(string message, Encoding encoding = null)
	{
		encoding ??= Encoding.UTF8;

		var bytes = encoding.GetBytes(message);

		Send(bytes);
	}

	protected abstract Stream GetStream();

	protected virtual void OnOnMessageReceived(byte[] data)
	{
		TcpMessageReceivedEvent?.Invoke(data);
	}

	private async Task SendMessage(byte[] bytes)
	{
		if (!Connected || tcpClient is null || stream is null)
		{
			return;
		}

		try
		{
			logger.WriteDebug($"Sending {bytes.Length} bytes to {host}:{port}");

			await stream.WriteAsync(bytes, cancelToken);

			logger.WriteDebug($"Done sending {bytes.Length} bytes to {host}:{port}");
		}
		catch (SocketException)
		{
			ShutdownSocket();

			await TryReconnect();
		}
		catch (IOException)
		{
			ShutdownSocket();

			await TryReconnect();
		}
	}

	private async Task SendingThread()
	{
		while (!cancelToken.IsCancellationRequested)
		{
			if (messagesToSend.TryTake(out var bytes))
			{
				await SendMessage(bytes);
			}
			else
			{
				await thread.Sleep(1.Milliseconds(), cancelToken);
			}
		}
	}

	private async Task CreateSocket()
	{
		tcpClient = new TcpClient();

		await tcpClient.ConnectAsync(host, port, cancelToken);

		stream = GetStream();

		tcpClient.Client.NoDelay = true;
		tcpClient.Client.ReceiveBufferSize = bufferSize;
		tcpClient.Client.ReceiveTimeout = 0;
		tcpClient.Client.SendBufferSize = bufferSize;
		tcpClient.Client.SendTimeout = 0;

		tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
		tcpClient.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 900);
		tcpClient.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 300);
		tcpClient.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, 5);
	}

	private async Task MakeConnection()
	{
		try
		{
			logger.WriteInformation($"Attempting to connect to <{host}:{port}>");

			await CreateSocket();
			VerifyCancelToken();

			Connected = true;

			logger.WriteInformation($"Connection succeed for <{host}:{port}>");

			thread.Run(SendingThread);
		}
		catch (IOException)
		{
			logger.WriteWarning($"Connection failed to <{host}:{port}>");

			await TryReconnect();
		}
		catch (SocketException)
		{
			logger.WriteError($"Connection failed to <{host}:{port}>");

			await TryReconnect();
		}
	}

	private async Task ReceiveThread()
	{
		buffer = new byte[bufferSize];

		try
		{
			while (!cancelToken.IsCancellationRequested)
			{
				var bytesCount = await stream.ReadAsync(buffer, cancelToken);

				if (bytesCount > 0)
				{
					var bytes = new byte[bytesCount];

					Array.Copy(buffer, bytes, bytesCount);

					OnOnMessageReceived(bytes);
				}
			}
		}
		catch (Exception e)
		{
			ConsoleLog.WriteException(e);
		}
	}

	private void ShutdownSocket()
	{
		try
		{
			Connected = false;

			stream?.Close();
		}
		catch
		{ // ignored
		}

		try
		{
			tcpClient.Dispose();
		}
		catch
		{ // ignored
		}
	}

	private async Task TryReconnect()
	{
		logger.WriteDebug($"Reconnect <{Reconnect}>");

		if (Reconnect)
		{
			logger.WriteDebug($"Going to try to reconnect to <{host}:{port}> in {ReconnectDelay}");

			await Task.Delay(ReconnectDelay, cancelToken);

			await MakeConnection();
		}
	}

	private void VerifyCancelToken()
	{
		if (cancelToken != default)
		{
			return;
		}

		cancelSource = new CancellationTokenSource();
		cancelToken = cancelSource.Token;
	}
}
