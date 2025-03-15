using FatCat.Toolkit.Console;
using FatCat.Toolkit.Web;
using Humanizer;

namespace OneOff.Old;

public class WebCallerWorker : SpikeWorker
{
	private readonly IWebCallerFactory webCallerFactory;

	public WebCallerWorker(IWebCallerFactory webCallerFactory)
	{
		this.webCallerFactory = webCallerFactory;
	}

	public override async Task DoWork()
	{
		await Task.Delay(1.Seconds());

		var webCaller = webCallerFactory.GetWebCaller(new Uri("https://localhost:14555/api"));

		var endingPath = "Test/Search?firstname=david&lastname=basarab&count=43";

		var result = await webCaller.Get(endingPath);

		if (result.IsSuccessful)
		{
			ConsoleLog.WriteGreen($"Status: {result.StatusCode} | {result.Content}");
		}
		else
		{
			ConsoleLog.WriteRed($"Status: {result.StatusCode}");
		}
	}
}
