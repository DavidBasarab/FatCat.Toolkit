#nullable enable
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FatCat.Toolkit.Communication;

public interface ISimpleTcpSender
{
	public Task Send(IPEndPoint endPoint, string message, Encoding? encoding = null);
}

public class SimpleTcpSender : ISimpleTcpSender
{
	public async Task Send(IPEndPoint endPoint, string message, Encoding? encoding = null)
	{
		using var client = new TcpClient();

		await client.ConnectAsync(endPoint);

		await using var networkStream = client.GetStream();

		var encodingToUse = encoding ?? Encoding.UTF8;

		var messageBytes = encodingToUse.GetBytes(message);

		await networkStream.WriteAsync(messageBytes);
		await networkStream.FlushAsync();
		networkStream.Close();

		client.Close();
		client.Dispose();
	}
}
