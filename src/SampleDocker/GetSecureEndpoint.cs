using System.Text.Json;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.Json;
using FatCat.Toolkit.Web.Api.SignalR;
using FatCat.Toolkit.WebServer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Endpoint = FatCat.Toolkit.WebServer.Endpoint;

namespace SampleDocker;

public class GetSecureEndpoint(IHttpContextAccessor httpContextAccessor) : Endpoint
{
	[HttpGet("api/Sample/Secure")]
	public WebResult DoGet()
	{
		ConsoleLog.WriteCyan($"ContextAccessor Type <{httpContextAccessor.GetType().FullName}>");
		ConsoleLog.WriteCyan($"Normal User <{User.Identity.Name}>");
		ConsoleLog.WriteMagenta($"ContextAccessor User <{httpContextAccessor.HttpContext.User.Identity.Name}>");

		var toolkitUser = ToolkitUser.Create(httpContextAccessor.HttpContext.User);

		ConsoleLog.WriteCyan(
			$"{new JsonOperations().Serialize(toolkitUser, new JsonSerializerOptions
																			{
																				WriteIndented = true })}"
		);

		return Ok("Got sample secure endpoint");
	}
}
