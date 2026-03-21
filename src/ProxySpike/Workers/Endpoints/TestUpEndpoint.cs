using FatCat.Toolkit.Console;
using FatCat.Toolkit.WebServer;
using Microsoft.AspNetCore.Mvc;

namespace ProxySpike.Workers.Endpoints;

public class TestUpEndpoint : Endpoint
{
	[HttpGet("api/Test")]
	public WebResult DoTestUp()
	{
		ConsoleLog.WriteMagenta("Hit the TestUp Endpoint");

		var message = $"You got me {DateTime.Now:hh:mm:ss:fff tt}";

		return Ok(message);
	}
}
