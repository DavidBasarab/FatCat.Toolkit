using FatCat.Toolkit.WebServer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace OneOff;

public class RateLimitedEndpoint : Endpoint
{
	[HttpGet("request/limited")]
	[EnableRateLimiting("oneoff-fixed")]
	public WebResult ReturnLimitedRequest()
	{
		return Ok("this-request-was-allowed");
	}
}
