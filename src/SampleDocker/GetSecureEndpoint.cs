﻿using FatCat.Toolkit.Console;
using FatCat.Toolkit.Web;
using FatCat.Toolkit.Web.Api.SignalR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Endpoint = FatCat.Toolkit.Web.Endpoint;

namespace SampleDocker;

public class GetSecureEndpoint : Endpoint
{
	private readonly IHttpContextAccessor httpContextAccessor;

	public GetSecureEndpoint(IHttpContextAccessor httpContextAccessor) => this.httpContextAccessor = httpContextAccessor;

	[HttpGet("api/Sample/Secure")]
	public WebResult DoGet()
	{
		var toolkitUser = ToolkitUser.Create(httpContextAccessor.HttpContext.User);

		ConsoleLog.WriteCyan($"{JsonConvert.SerializeObject(toolkitUser, Formatting.Indented)}");

		return Ok("Got sample secure endpoint");
	}
}