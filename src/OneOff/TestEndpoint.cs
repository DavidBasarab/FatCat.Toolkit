﻿using FatCat.Fakes;
using FatCat.Toolkit;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.WebServer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OneOff;

public enum SomeEnum
{
	First,
	Second,
	Third,
	Fourth
}

public class TestModel : EqualObject
{
	public string FirstName { get; set; }

	public string LastName { get; set; }

	public string SomeData { get; set; }

	[JsonConverter(typeof(StringEnumConverter))]
	public SomeEnum SomeEnum { get; set; }
}

public class TestRequest : EqualObject
{
	public string FirstName { get; set; }

	public string LastName { get; set; }

	public string SomeData { get; set; }

	public string SomeEnum { get; set; }
}

[AllowAnonymous]
public class TestEndpoint(ISomeServiceWorker someServiceWorker) : Endpoint
{
	[HttpGet("api/test")]
	public WebResult GetTestStuff()
	{
		var testModel = Faker.Create<TestModel>();

		testModel.SomeData = $"This is a test endpoint | <{DateTime.Now:h:mm:ss tt zz}>";

		someServiceWorker.DoSomeWork();

		return WebResult.Ok(testModel);
	}
}

[AllowAnonymous]
public class TestPostEndpoint : Endpoint
{
	[HttpPost("api/test/post")]
	public WebResult GetTestStuff([FromBody] TestModel testRequest)
	{
		ConsoleLog.WriteDarkYellow($"SomEnum := <{testRequest.SomeEnum}>");

		var testModel = Faker.Create<TestModel>();

		testModel.SomeData = $"This is a test endpoint | <{DateTime.Now:h:mm:ss tt zz}>";

		return WebResult.Ok(testModel);
	}
}
