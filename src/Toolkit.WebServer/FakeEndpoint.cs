using System.Diagnostics.CodeAnalysis;
using FatCat.Toolkit.Console;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace FatCat.Toolkit.WebServer;

public class LoginRequest : EqualObject
{
	public string ClearPassword { get; set; }

	public string Username { get; set; }
}

[ExcludeFromCodeCoverage(Justification = "Used only for testing will need to remove")]
[AllowAnonymous]
public class LoginEndpoint() : Endpoint
{
	[HttpPost("api/Login")]
	public async Task<WebResult> LoginUser([FromBody] LoginRequest loginRequest)
	{
		await Task.CompletedTask;

		ConsoleLog.WriteMagenta("LoginEndpoint.LoginUser");
		ConsoleLog.WriteMagenta($"{JsonConvert.SerializeObject(loginRequest)}");

		return Ok();
	}
}
