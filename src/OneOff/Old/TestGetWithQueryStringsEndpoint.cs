using FatCat.Toolkit;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.Json;
using FatCat.Toolkit.WebServer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OneOff.Old;

public class QueryRequest : EqualObject
{
	public int Count { get; set; }

	public string FirstName { get; set; }

	public string LastName { get; set; }
}

[AllowAnonymous]
public class TestGetWithQueryStringsEndpoint : Endpoint
{
	[HttpGet("api/Test/Search")]
	public WebResult DoTestWithQueryStrings([FromQuery] QueryRequest request)
	{
		ConsoleLog.WriteMagenta("Got Query Request");
		ConsoleLog.WriteMagenta(new JsonOperations().Serialize(request, true));

		return Ok(
			$"Got {request.Count} of {request.FirstName} {request.LastName} | <{DateTime.Now:h:mm:ss tt zz}>"
		);
	}
}
