using System.Diagnostics;
using FatCat.Fakes;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.Threading;
using FatCat.Toolkit.Web;
using FatCat.Toolkit.Web.Api.SignalR;
using Humanizer;

namespace OneOffToolkitOnly;

public class ClientWorker(IThread thread, IToolkitHubClientFactory hubFactory, IWebCallerFactory webCallerFactory)
{
	public void DoWork(string[] args)
	{
		thread.Run(async () =>
		{
			HttpClientFactory.UseHttpClientHandler();

			var mainUrl = "http://127.0.0.1:14555";

			if (args.Length > 1)
			{
				mainUrl = args[1];
			}

			ConsoleLog.WriteCyan($"Using Url := <{mainUrl}>");

			var webCaller = webCallerFactory.GetWebCaller(new Uri(mainUrl));

			var tokenResult = await webCaller.Get("api/sample/token");

			if (tokenResult.IsUnsuccessful)
			{
				ConsoleLog.WriteRed(
					$"Could not get TOKEN this is an error!!!!! | StatusCode := <{tokenResult.StatusCode}>"
				);

				return;
			}

			var testToken = tokenResult.Content;

			ConsoleLog.Write($"Using Token := {testToken}");

			webCaller.UserBearerToken(testToken);

			ConsoleLog.WriteCyan($"WebCaller URI <{webCaller.BaseUri}>");

			var response = await webCaller.Get("api/Sample/Secure");

			ConsoleLog.WriteMagenta($"StatusCode := <{response.StatusCode}> | {response.Content}");

			await ConnectToHub(mainUrl, testToken);
		});
	}

	private async Task ConnectToHub(string mainUrl, string testToken)
	{
		// Secure Url
		var hubUrl = $"{mainUrl}/events?access_token={testToken}";

		ConsoleLog.WriteYellow("  Going to connect to hub");

		var result = await hubFactory.TryToConnectToClient(
			hubUrl,
			() =>
			{
				ConsoleLog.WriteDarkRed("Connection lost to server");

				Task.Delay(10.Seconds()).Wait();

				ConnectToHub(mainUrl, testToken).Wait();
			}
		);

		if (!result.Connected)
		{
			ConsoleLog.WriteRed($"Could not connect too <{hubUrl}>");

			return;
		}

		ConsoleLog.WriteGreen("Connected to HUB");

		var hubConnection = result.Connection;

		hubConnection.ServerMessage += OnServerMessage;
		hubConnection.ServerDataBufferMessage += OnDataBufferFromServer;

		await thread.Sleep(1.Seconds());

		ConsoleLog.WriteDarkGreen($"Done connecting to hub at {hubUrl}");

		await thread.Sleep(1.Seconds());

		for (var i = 0; i < 17; i++)
		{
			await SendMessageToHub(hubConnection);

			await thread.Sleep(1.Seconds());

			await SendDataBuffer(hubConnection);

			await thread.Sleep(1.Seconds());
		}
	}

	private static async Task<string> OnDataBufferFromServer(ToolkitMessage message, byte[] dataBuffer)
	{
		await Task.CompletedTask;

		ConsoleLog.WriteYellow($"Client Received DataBuffer Message of length {dataBuffer.Length}");

		var responseMessage = Faker.RandomString("ClientDataBufferResponse_");

		ConsoleLog.WriteMagenta($"Client DataBufferResponse : <{responseMessage}>");

		return responseMessage;
	}

	private static async Task<string> OnServerMessage(ToolkitMessage message)
	{
		await Task.CompletedTask;

		var responseMessage = $"{Faker.RandomString()}";

		ConsoleLog.WriteGreen($"Client Response: <{responseMessage}>");

		return responseMessage;
	}

	private static async Task SendDataBuffer(IToolkitHubClientConnection hubConnection)
	{
		var dataBuffer = Faker.Create<List<byte>>(1024).ToArray();

		ConsoleLog.WriteCyan($"Going to send data message of length {dataBuffer.Length}");

		var watch = Stopwatch.StartNew();

		await hubConnection.SendDataBufferNoResponse(
			new ToolkitMessage { Data = "Junk", MessageType = 123 },
			dataBuffer
		);

		watch.Stop();

		ConsoleLog.WriteYellow($"Send DataBuffer Message Took <{watch.Elapsed}>");

		// ConsoleLog.WriteCyan(
		//  $"Response from server was {JsonConvert.SerializeObject(response, Formatting.Indented)}"
		// );
	}

	private static async Task SendMessageToHub(IToolkitHubClientConnection hubConnection)
	{
		await hubConnection.SendNoResponse(
			new ToolkitMessage
			{
				Data = $"This is from the client - <{Faker.RandomString()}>",
				MessageType = 13,
				ConnectionId = "ConnectionId",
			}
		);
	}
}
