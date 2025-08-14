using System.Text.Json;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.Json;
using FatCat.Toolkit.WebServer;
using Microsoft.AspNetCore.Mvc;
using ProxySpike.Workers.ServiceModels;

namespace ProxySpike.Workers.Endpoints;

public class TestPostEndpoint : Endpoint
{
	[HttpPost("api/Test")]
	public WebResult DoPostTest([FromBody] SamplePostRequest request)
	{
		var jsonOperations = new JsonOperations();

		ConsoleLog.WriteDarkGreen(
			$"{jsonOperations.Serialize(request, new JsonSerializerOptions { WriteIndented = true })}"
		);

		return Ok($"You hit the TestPost Endpoint | {DateTime.Now:hh:mm:ss:fff tt}");
	}
}
