using System.Reflection;
using FatCat.Fakes;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.Web.Api;
using FatCat.Toolkit.WebServer;
using Newtonsoft.Json;
using ProxySpike.Helpers;
using ProxySpike.Options;

namespace ProxySpike.Workers;

public class WebServerWorker : ISpikeWorker<ServerOptions>
{
	public Task DoWork(ServerOptions options)
	{
		ConsoleLog.WriteCyan($"Web Server Worker on port <{options.WebPort}>");

		var applicationSettings = new ToolkitWebApplicationSettings
		{
			Options = WebApplicationOptions.SignalR,
			TlsCertificate = new CertificationSettings
			{
				Location = @"C:\DevelopmentCert\DevelopmentCert.pfx",
				Password = "basarab_cert"
			},
			ToolkitTokenParameters = new SpikeToolkitParameters(),
			ContainerAssemblies = new List<Assembly> { Assembly.GetExecutingAssembly() },
			OnWebApplicationStarted = Started,
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
				$"MessageId <{message.MessageType}> | Data <{message.Data}> | ConnectionId <{message.ConnectionId}>"
			);

			return "ACK";
		};

		ToolkitWebApplication.Run(applicationSettings);

		return Task.CompletedTask;
	}

	private void Started()
	{
		ConsoleLog.WriteGreen("Hey the web application has started!!!!!");
	}
}
