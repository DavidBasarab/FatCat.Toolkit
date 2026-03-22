using System.Text;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.Web.Api;
using FatCat.Toolkit.WebServer;

namespace OneOff.MongoProof;

public class WebServerProofWorker
{
	private readonly TaskCompletionSource serverReady = new();

	private string serverUrl;
	private Action<ToolkitWebApplicationSettings> settingsConfigurator;

	public async Task DoWork(string label, string url, Action<ToolkitWebApplicationSettings> configure = null)
	{
		serverUrl = url;
		settingsConfigurator = configure;

		ConsoleLog.WriteCyan($"=== Web Server Proof [{label}] ===");

		_ = Task.Run(StartServer).ContinueWith(t =>
		{
			if (t.IsFaulted)
			{
				serverReady.TrySetException(t.Exception!);
			}
		});

		await serverReady.Task.WaitAsync(TimeSpan.FromSeconds(10));

		using var client = new HttpClient();

		var getResponse = await client.GetStringAsync($"{serverUrl}/api/proof");

		ConsoleLog.WriteBlue($"GET /api/proof → {getResponse}");

		var postContent = new StringContent("\"hello-proof\"", Encoding.UTF8, "application/json");
		var postResult = await client.PostAsync($"{serverUrl}/api/proof", postContent);
		var postBody = await postResult.Content.ReadAsStringAsync();

		ConsoleLog.WriteBlue($"POST /api/proof → {postBody}");

		if (getResponse.Contains("PROOF_GET_OK") && postBody.Contains("PROOF_POST_OK"))
		{
			ConsoleLog.WriteMagenta($"=== PASS [{label}]: Web server GET and POST succeeded ===");
		}
		else
		{
			ConsoleLog.WriteRed($"=== FAIL [{label}]: Unexpected responses — GET:{getResponse} POST:{postBody} ===");
		}
	}

	private void StartServer()
	{
		var settings = new ToolkitWebApplicationSettings
		{
			AllowAllOrigins = true,
			ContainerAssemblies = [typeof(Program).Assembly, typeof(ConsoleLog).Assembly],
			Options = WebApplicationOptions.Cors,
			Args = ["--urls", serverUrl],
			OnWebApplicationStarted = () => serverReady.TrySetResult(),
		};

		settingsConfigurator?.Invoke(settings);

		ToolkitWebApplication.Run(settings);
	}
}
