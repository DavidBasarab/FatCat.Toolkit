using FatCat.Toolkit.Console;
using FatCat.Toolkit.Json;
using FatCat.Toolkit.WebServer;
using Microsoft.AspNetCore.Mvc;

namespace OneOff.Old;

public class TestGetWithQueryStringListEndpoint : Endpoint
{
	[HttpGet("api/Test/Search/Multi")]
	public WebResult DoTestWithQueryStrings([FromQuery] List<MovieItemStatus> statuses)
	{
		ConsoleLog.WriteMagenta("Got Query Request");

		ConsoleLog.WriteMagenta(new JsonOperations().Serialize(statuses, true));

		return Ok($"Got Message | <{DateTime.Now:h:mm:ss tt zz}>");
	}
}
