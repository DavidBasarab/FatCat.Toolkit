using System.Diagnostics;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.Logging;
using FatCat.Toolkit.Web;
using Humanizer;

namespace OneOff;

public class ClientWorker(IWebCallerFactory webCallerFactory)
{
	public async Task DoWork()
	{
		await Task.CompletedTask;

		ToolkitLogger.Enabled = true;

		ConsoleLog.Write("Going to call the testing endpoint");

		var watch = Stopwatch.StartNew();

		try
		{
			var webCaller = webCallerFactory.GetWebCaller(new Uri("http://localhost:5000"));

			var response = await webCaller.Post("api/long-running", 3.Minutes());

			if (response.IsSuccessful)
			{
				ConsoleLog.WriteGreen($"Response from long running endpoint: {response.Content}");
			}
			else
			{
				ConsoleLog.WriteRed($"Failed to call long running endpoint: {response.StatusCode}");
			}
		}
		catch (Exception ex)
		{
			ConsoleLog.WriteException(ex);
		}

		watch.Stop();

		ConsoleLog.WriteCyan($"Finished calling long running endpoint in | {watch.Elapsed}");
	}
}
