using System.Diagnostics;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.Threading;
using FatCat.Toolkit.WebServer;
using Humanizer;
using Microsoft.AspNetCore.Mvc;

namespace OneOff;

public class LongRunningEndpoint(IThread thread) : Endpoint
{
	[HttpPost("api/long-running")]
	public async Task<WebResult> DoLongRunningWork()
	{
		ConsoleLog.Write("Starting long running work");

		var watch = Stopwatch.StartNew();

		do
		{
			ConsoleLog.WriteCyan($"Doing long running work for {watch.Elapsed.Humanize(3)}");

			await thread.Sleep(10.Seconds());
		} while (watch.Elapsed < 2.Minutes());

		return Ok("Finished long running work");
	}
}
