using FatCat.Toolkit.Console;
using FatCat.Toolkit.WebServer;
using Microsoft.AspNetCore.Mvc;

namespace ProxySpike.Workers.Endpoints;

public class TestDeleteEndpoint : Endpoint
{
	[HttpDelete("api/Test/{someId}")]
	public WebResult DoDeleteTest(string someId)
	{
		ConsoleLog.WriteDarkCyan($"Hit test delete endpoint with Id of {someId}");

		return Ok($"You hit test Delete {DateTime.Now:hh:mm:ss:fff tt}");
	}
}
