using System.Reflection;
using FatCat.Fakes;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.Json;
using FatCat.Toolkit.Web.Api;
using FatCat.Toolkit.Web.Api.SignalR;
using FatCat.Toolkit.WebServer;
using Humanizer;
using Microsoft.IdentityModel.Tokens;

namespace SampleDocker;

public static class Program
{
	public static void Main(params string[] args)
	{
		StartWebServer(args);
	}

	private static Task OnClientConnected(ToolkitUser user, string connectionId)
	{
		ConsoleLog.WriteDarkCyan($"A client has connected: <{user.Name}> | <{connectionId}>");

		return Task.CompletedTask;
	}

	private static Task OnClientDisconnected(ToolkitUser user, string connectionId)
	{
		ConsoleLog.WriteDarkYellow($"A client has disconnected: <{user.Name}> | <{connectionId}>");

		return Task.CompletedTask;
	}

	private static void Started()
	{
		try
		{
			ConsoleLog.WriteGreen("Hey the web application has started!!!!!");

			// var testingEndpoint = SystemScope.Container.Resolve<GetStorageItemsEndpoint>();
			//
			// testingEndpoint.GetStorageItems().Wait();
		}
		catch (Exception e)
		{
			ConsoleLog.WriteException(e);
		}
	}

	private static void StartWebServer(string[] args)
	{
		var applicationSettings = new ToolkitWebApplicationSettings
		{
			Options =
				WebApplicationOptions.CommonOptions
				| WebApplicationOptions.SignalR
				| WebApplicationOptions.Authentication,
			ToolkitTokenParameters = new SpikeToolkitParameters(),
			ContainerAssemblies = new List<Assembly>
			{
				Assembly.GetExecutingAssembly(),
				typeof(ToolkitWebApplication).Assembly,
			},
			OnWebApplicationStarted = Started,
			Args = args,
			BasePath = "david",
		};

		applicationSettings.ClientDataBufferMessage += async (message, buffer) =>
		{
			ConsoleLog.WriteMagenta($"Got data buffer message: {new JsonOperations().Serialize(message)}");
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

		applicationSettings.ClientConnected += OnClientConnected;
		applicationSettings.ClientDisconnected += OnClientDisconnected;

		ToolkitWebApplication.Run(applicationSettings);
	}
}

public class SpikeToolkitParameters : IToolkitTokenParameters
{
	public TokenValidationParameters Get()
	{
		ConsoleLog.WriteCyan("Getting token parameters");

		return new TokenValidationParameters
		{
			IssuerSigningKey = new RsaSecurityKey(SecureData.Rsa),
			ValidAudience = "https://foghaze.com/Brume",
			ValidIssuer = "FogHaze",
			ClockSkew = 10.Seconds(),
		};
	}
}
