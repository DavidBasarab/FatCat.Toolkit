using System.Security.Cryptography.X509Certificates;
using System.Text;
using FakeItEasy;
using FatCat.Fakes;
using FatCat.Toolkit;
using FatCat.Toolkit.Communication;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.Logging;
using FatCat.Toolkit.Threading;
using Humanizer;
using Thread = FatCat.Toolkit.Threading.Thread;

namespace OneOff;

public class TcpWorker(ISimpleTcpSender tcpSender, IGenerator generator, IThread thread) : SpikeWorker
{
	private const int TcpPort = 24329;
	private readonly ISimpleTcpSender tcpSender = tcpSender;
	private IFatTcpClient fatTcpClient;
	private IFatTcpServer fatTcpServer;
	private static int numberOfErrors = 0;

	public static int NumberOfErrors
	{
		get => numberOfErrors;
	}

	public override async Task DoWork()
	{
		ConsoleLog.Write("Going to play with TCP Stuff");

		var cert = new X509Certificate2(@"C:\DevelopmentCert\DevelopmentCert.pfx", "basarab_cert");

		if (Program.Args.Length != 0)
		{
			fatTcpClient = new SecureFatTcpClient(
				cert,
				new ConsoleFatTcpLogger(),
				new Thread(new ToolkitLogger())
			);

			fatTcpClient.Reconnect = true;

			var host = Program.Args.FirstOrDefault();

			await fatTcpClient.Connect(host, TcpPort);

			for (var i = 0; i < 5; i++)
			{
				thread.Run(SendMessages);
			}
		}
		else
		{
			StartServer(cert);
		}
	}

	private async Task SendMessages()
	{
		for (var i = 0; i < 8500000; i++)
		{
			await SendMessage(i);
		}
	}

	private async Task SendMessage(int i)
	{
		try
		{
			var message =
				$"{i:X} This is a	message | {DateTime.Now:T} | ExtraData {Faker.RandomString(length: Faker.RandomInt(10, 200))}";

			var bytes = Encoding.UTF8.GetBytes(message);

			fatTcpClient.Send(bytes);

			var delayTime = Faker.RandomInt(0, 123);

			if (i % 15 != 0)
			{
				await Task.Delay(delayTime.Milliseconds());
			}
		}
		catch (Exception ex)
		{
			ConsoleLog.WriteException(ex);

			Interlocked.Increment(ref numberOfErrors);
		}
	}

	private void StartServer(X509Certificate2 cert)
	{
		fatTcpServer = new SecureFatTcpServer(cert, generator, new ConsoleFatTcpLogger());

		fatTcpServer.TcpMessageReceivedEvent += TcpClientMessage;

		fatTcpServer.Start(TcpPort, Encoding.UTF8);
	}

	private void TcpClientMessage(byte[] data)
	{
		var message = Encoding.UTF8.GetString(data);

		ConsoleLog.WriteCyan($"Message from client := <{message}>");
	}
}
