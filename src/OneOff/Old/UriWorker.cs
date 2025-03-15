using FakeItEasy;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.Json;
using FatCat.Toolkit.Logging;
using FatCat.Toolkit.Web;

namespace OneOff.Old;

public class UriWorker : SpikeWorker
{
	private const string BaseUrl = "https://httpbin.org/anything";
	private readonly IConsoleUtilities consoleUtilities;

	public UriWorker(IConsoleUtilities consoleUtilities)
	{
		this.consoleUtilities = consoleUtilities;
	}

	public override async Task DoWork()
	{
		await Task.CompletedTask;

		var webCaller = new WebCaller(new Uri(BaseUrl), new JsonOperations(), A.Fake<IToolkitLogger>());

		var endingPath = "StarTrek";

		var expectedFullUrl = $"{BaseUrl}/{endingPath}";

		ConsoleLog.WriteCyan($"FullUrl => <{webCaller.GetFullUrl(endingPath)}> | Expected => <{expectedFullUrl}>");

		consoleUtilities.Exit();
	}
}
