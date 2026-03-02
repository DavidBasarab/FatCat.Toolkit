using FatCat.Toolkit.Console;
using FatCat.Toolkit.Web;

namespace OneOff;

public class ClientWorker(IWebCallerFactory webCallerFactory)
{
	public async Task DoWork()
	{
		await Task.CompletedTask;

		ConsoleLog.Write("Going to call the testing endpoint");

		var webCaller = webCallerFactory.GetWebCaller(new Uri("http://localhost:5000"));

		var response = await webCaller.Post("api/long-running");

		if (response.IsSuccessful)
		{
			ConsoleLog.WriteGreen($"Response from long running endpoint: {response.Content}");
		}
		else
		{
			ConsoleLog.WriteRed($"Failed to call long running endpoint: {response.StatusCode}");
		}
	}
}
