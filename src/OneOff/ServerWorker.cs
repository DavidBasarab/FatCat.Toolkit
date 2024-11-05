using System.Reflection;
using FatCat.Fakes;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.Injection;
using FatCat.Toolkit.Threading;
using FatCat.Toolkit.Web;
using FatCat.Toolkit.Web.Api;
using FatCat.Toolkit.Web.Api.SignalR;
using FatCat.Toolkit.WebServer;
using Newtonsoft.Json;

namespace OneOff;

public class ServerWorker(IThread thread)
{
	private readonly IThread thread = thread;
	private ToolkitWebApplicationSettings applicationSettings;

	public void DoWork(string[] args)
	{
		applicationSettings = new ToolkitWebApplicationSettings
		{
			Options = WebApplicationOptions.SignalR,
			TlsCertificate = new CertificationSettings
			{
				Location = @"C:\DevelopmentCert\DevelopmentCert.pfx",
				Password = "basarab_cert"
			},
			SignalRPath = "events",
			ToolkitTokenParameters = new SpikeToolkitParameters(),
			ContainerAssemblies = [Assembly.GetExecutingAssembly(), typeof(ToolkitWebServerModule).Assembly],
			OnWebApplicationStarted = Started,
			Args = args
		};

		applicationSettings.ClientDataBufferMessage += async (message, buffer) =>
		{
			ConsoleLog.WriteMagenta($"Got data buffer message: {JsonConvert.SerializeObject(message)}");
			ConsoleLog.WriteMagenta($"Data buffer length: {buffer.Length}");

			await Task.CompletedTask;

			var responseMessage = $"BufferResponse {Faker.RandomString()}";

			ConsoleLog.WriteGreen($"Client Response for data buffer: <{responseMessage}>");

			return responseMessage;
		};

		applicationSettings.ClientMessage += async message =>
		{
			await Task.CompletedTask;

			ConsoleLog.WriteDarkCyan(
				$"******** MessageId <{message.MessageType}> | Data <{message.Data}> | ConnectionId <{message.ConnectionId}>"
			);

			return "ACK";
		};

		applicationSettings.ClientConnected += OnClientConnected;
		applicationSettings.ClientDisconnected += OnClientDisconnected;

		ToolkitWebApplication.Run(applicationSettings);
	}

	private static void MakeWebRequest(IWebCaller caller, string url)
	{
		ConsoleLog.WriteDarkCyan($"Making web request to url <{url}>");

		var response = caller.Get(url).Result;

		// var finalResult = new WebResult<TestModel>(response);
		var finalResult = response;

		if (finalResult.IsSuccessful)
		{
			ConsoleLog.WriteGreen(finalResult.Content);
		}
		else
		{
			ConsoleLog.WriteRed($"Web Request status code: <{finalResult.StatusCode}> | <{finalResult.Content}>");
		}
	}

	private Task OnClientConnected(ToolkitUser user, string connectionId)
	{
		ConsoleLog.WriteDarkCyan($"A client has connected: <{user.Name}> | <{connectionId}>");

		return Task.CompletedTask;
	}

	private Task OnClientDisconnected(ToolkitUser user, string connectionId)
	{
		ConsoleLog.WriteDarkYellow($"A client has disconnected: <{user.Name}> | <{connectionId}>");

		return Task.CompletedTask;
	}

	private void Started()
	{
		thread.Run(() =>
		{
			ConsoleLog.WriteGreen("Hey the web application has started!!!!!");

			var factory = SystemScope.Container.Resolve<IWebCallerFactory>();

			var caller = factory.GetWebCaller(new Uri("http://localhost:14555"));

			MakeWebRequest(caller, "api/test");

			var testModel = Faker.Create<TestModel>();

			caller.Post("api/test/post", testModel);

			// var response = caller.Get("api/test/Search/firstname=david&lastname=basarab&count=43").Result;
			// MakeWebRequest(caller, "api/test/Search?firstname=david&lastname=basarab&count=43");
			// MakeWebRequest(caller, "api/test/Search/Multi?statuses=Available&statuses=CheckedOut");
		});
	}
}
