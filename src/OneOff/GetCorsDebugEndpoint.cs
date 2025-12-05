using FatCat.Toolkit.Json;
using FatCat.Toolkit.WebServer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace OneOff;

[AllowAnonymous]
public class GetCorsDebugEndpoint(IOptions<CorsOptions> corsOptions, IJsonOperations jsonOperations) : Endpoint
{
	[HttpGet("api/debug/cors-test")]
	public WebResult GetCorsDebug()
	{
		var requestOrigin = Request.Headers["Origin"].ToString();
		var policy = corsOptions.Value.GetPolicy("AllOrigins");

		if (policy == null)
		{
			var notFoundData = new { message = "CORS policy not found" };

			return Ok(jsonOperations.Serialize(notFoundData));
		}

		var corsInfo = new
		{
			message = "CORS Debug Info",
			requestOrigin,
			allowedOrigins = policy.Origins,
			allowAnyOrigin = policy.Origins.Count == 0 && policy.IsOriginAllowed("*"),
			supportsCredentials = policy.SupportsCredentials,
			allowedMethods = policy.Methods,
			allowedHeaders = policy.Headers,
		};

		return Ok(jsonOperations.Serialize(corsInfo));
	}
}
