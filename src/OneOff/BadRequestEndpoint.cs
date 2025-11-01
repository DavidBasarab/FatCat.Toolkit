using FatCat.Toolkit.WebServer;
using Microsoft.AspNetCore.Mvc;

namespace OneOff;

public class BadRequestEndpoint : Endpoint
{
	[HttpGet("request/bad")]
	public WebResult ReturnBadRequest()
	{
		return WebResult.BadRequest("this-is-a-bad-request");
	}

	[HttpGet("request/good")]
	public WebResult ReturnGoodRequest()
	{
		return Ok("this-is-a-good-request");
	}
}
