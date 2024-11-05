using System.Net.Sockets;

namespace FatCat.Toolkit.Communication;

public abstract class ClientConnection(
	IFatTcpServer server,
	TcpClient client,
	string clientId,
	int bufferSize,
	IFatTcpLogger logger,
	CancellationToken cancellationToken
)
{
	protected readonly int bufferSize = bufferSize;
	protected readonly CancellationToken cancellationToken = cancellationToken;
	protected readonly TcpClient client = client;
	protected readonly string clientId = clientId;
	protected readonly IFatTcpServer server = server;

	private bool IsNotCanceled
	{
		get => !cancellationToken.IsCancellationRequested;
	}

	public void Start()
	{
		Task.Factory.StartNew(ReceivingThread, TaskCreationOptions.LongRunning);
	}

	protected abstract Task<Stream> GetStream();

	protected virtual async Task ReceivingThread()
	{
		var stream = await GetStream();
		var buffer = new byte[bufferSize];

		try
		{
			logger.WriteInformation($"Client Connected from {client.Client.RemoteEndPoint}");

			while (IsNotCanceled)
			{
				var bytesCount = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

				if (bytesCount == 0)
				{
					continue;
				}

				var bytesReceived = new byte[bytesCount];

				Array.Copy(buffer, bytesReceived, bytesCount);

				logger.WriteDebug($"Received {bytesCount} bytes from {clientId}");

				server.OnMessageReceived(bytesReceived);
			}
		}
		catch (IOException) { }
		catch (Exception ex)
		{
			logger.WriteException(ex);
		}
	}
}
