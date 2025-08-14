using FatCat.Toolkit.Json;
using Microsoft.AspNetCore.Mvc;

namespace FatCat.Toolkit.WebServer;

[Controller]
public abstract class Endpoint : Controller
{
	protected string AuthToken
	{
		get =>
			Request.Headers.TryGetValue("Authorization", out var values) ? values.FirstOrDefault() : string.Empty;
	}

	protected WebResult BadRequest(string message = null)
	{
		return WebResult.BadRequest(message);
	}

	protected WebResult BadRequest(string fieldName, string messageId)
	{
		return WebResult.BadRequest(fieldName, messageId);
	}

	protected WebResult NotAcceptable(string message = null)
	{
		return WebResult.NotAcceptable(message);
	}

	protected WebResult NotFound(string message = null)
	{
		return WebResult.NotFound(message);
	}

	protected WebResult NotImplemented()
	{
		return WebResult.NotImplemented();
	}

	protected WebResult Ok(EqualObject model)
	{
		return Ok(new JsonOperations().Serialize(model));
	}

	protected WebResult Ok<T>(List<T> list)
		where T : EqualObject
	{
		return Ok(new JsonOperations().Serialize(list));
	}

	protected WebResult Ok<T>(IEnumerable<T> list)
		where T : EqualObject
	{
		return Ok(new JsonOperations().Serialize(list));
	}

	/// <summary>
	///  If you call this with an empty string or null for content, it will return a 204.
	/// </summary>
	protected WebResult Ok(string results = null)
	{
		return WebResult.Ok(results);
	}

	protected async Task<string> ReadContent()
	{
		return await Request.ReadContent();
	}

	protected new WebResult Unauthorized()
	{
		return WebResult.Unauthorized();
	}
}
