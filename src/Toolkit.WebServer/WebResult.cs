using System.Net;
using FatCat.Toolkit.Extensions;
using FatCat.Toolkit.Json;
using FatCat.Toolkit.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FatCat.Toolkit.WebServer;

public class WebResult<T> : IActionResult
	where T : class
{
	public static WebResult<T> BadRequest(string message)
	{
		return new WebResult<T>(WebResult.BadRequest(message));
	}

	public static WebResult<T> BadRequest()
	{
		return new WebResult<T>(WebResult.BadRequest(string.Empty));
	}

	public static WebResult<T> NotFound()
	{
		return new WebResult<T>(WebResult.NotFound());
	}

	public static WebResult<T> NotImplemented()
	{
		return new WebResult<T>(WebResult.NotImplemented());
	}

	public static WebResult<T> Ok()
	{
		return new WebResult<T>(WebResult.Ok());
	}

	public static WebResult<T> Ok(T item)
	{
		return new WebResult<T>(WebResult.Ok(new JsonOperations().Serialize(item)));
	}

	public WebResult BaseResult { get; }

	public string Content
	{
		get => BaseResult.Content;
	}

	public string ContentType
	{
		get => BaseResult.ContentType;
	}

	public T Data
	{
		get => BaseResult.IsSuccessful ? BaseResult.To<T>() : null;
	}

	public bool IsSuccessful
	{
		get => BaseResult.IsSuccessful;
	}

	public bool IsUnsuccessful
	{
		get => BaseResult.IsUnsuccessful;
	}

	public HttpStatusCode StatusCode
	{
		get => BaseResult.StatusCode;
	}

	public WebResult(WebResult result)
	{
		BaseResult = result;
	}

	public async Task ExecuteResultAsync(ActionContext context)
	{
		await BaseResult.ExecuteResultAsync(context);
	}

	public override string ToString()
	{
		return $"WebResult | StatusCode <{StatusCode}> | Type {typeof(T).FullName} | {BaseResult.Content}";
	}
}

public class WebResult : IActionResult
{
	public static WebResult BadRequest(ModelStateDictionary modelState)
	{
		return new WebResult(HttpStatusCode.BadRequest, modelState);
	}

	public static WebResult BadRequest(string fieldName, string messageId)
	{
		var modelState = new ModelStateDictionary();

		if (messageId.IsNotNullOrEmpty())
		{
			modelState.AddModelError(fieldName, messageId!);
		}

		return BadRequest(modelState);
	}

	public static WebResult BadRequest(string messageId)
	{
		return BadRequest("General", messageId);
	}

	public static WebResult NotAcceptable(string content = null)
	{
		return new WebResult(HttpStatusCode.NotAcceptable, content);
	}

	public static WebResult NotFound(ModelStateDictionary modelState)
	{
		return new WebResult(HttpStatusCode.NotFound, modelState);
	}

	public static WebResult NotFound(string fieldName, string messageId)
	{
		var modelState = new ModelStateDictionary();

		modelState.AddModelError(fieldName, messageId);

		return NotFound(modelState);
	}

	public static WebResult NotFound(string content = null)
	{
		return new WebResult(HttpStatusCode.NotFound, content);
	}

	public static WebResult NotImplemented(string content = null)
	{
		return new WebResult(HttpStatusCode.NotImplemented, content);
	}

	public static WebResult Ok(string content = null)
	{
		return new WebResult(content!.IsNullOrEmpty() ? HttpStatusCode.NoContent : HttpStatusCode.OK, content!);
	}

	public static WebResult Ok(EqualObject returnObject)
	{
		return new WebResult(returnObject == null ? HttpStatusCode.NoContent : HttpStatusCode.OK, returnObject!);
	}

	public static WebResult Ok(IEnumerable<EqualObject> returnList)
	{
		return new WebResult(returnList);
	}

	public static WebResult Ok(List<EqualObject> returnList)
	{
		return new WebResult(returnList);
	}

	public static WebResult PreconditionFailed(string content = null)
	{
		return new WebResult(HttpStatusCode.PreconditionFailed, content);
	}

	public static WebResult Timeout()
	{
		return new WebResult(HttpStatusCode.RequestTimeout);
	}

	public static WebResult Unauthorized()
	{
		return new WebResult(HttpStatusCode.Unauthorized);
	}

	private readonly HttpContent httpContent;

	private string content;

	public string Content
	{
		get
		{
			content ??= httpContent?.ReadAsStringAsync().Result;

			return content;
		}
		set => content = value;
	}

	public string ContentType { get; set; } = "application/json; charset=UTF-8";

	public bool IsSuccessful
	{
		get => (int)StatusCode >= 200 && (int)StatusCode <= 299;
	}

	public bool IsUnsuccessful
	{
		get => !IsSuccessful;
	}

	public HttpStatusCode StatusCode { get; set; }

	public WebResult(HttpResponseMessage response)
	{
		StatusCode = response.StatusCode;

		httpContent = response.Content;
	}

	public WebResult(EqualObject resultObject)
		: this(resultObject == null ? HttpStatusCode.NoContent : HttpStatusCode.OK, resultObject!) { }

	/// <summary>
	///  If you call this with an empty string or null for content, it will return a 204.
	/// </summary>
	public WebResult(string content = null)
		: this(content!.IsNullOrEmpty() ? HttpStatusCode.NoContent : HttpStatusCode.OK, content) { }

	public WebResult(HttpStatusCode statusCode, EqualObject resultObject)
		: this(statusCode, new JsonOperations().Serialize(resultObject)) { }

	public WebResult(HttpStatusCode statusCode, IEnumerable<EqualObject> returnList)
		: this(statusCode, new JsonOperations().Serialize(returnList)) { }

	public WebResult(IEnumerable<EqualObject> returnList)
		: this(HttpStatusCode.OK, new JsonOperations().Serialize(returnList)) { }

	public WebResult(FatWebResponse webResponse)
		: this(webResponse.StatusCode, webResponse.Content)
	{
		ContentType = webResponse.ContentType;
	}

	public WebResult(HttpStatusCode statusCode, ModelStateDictionary modelState)
		: this(statusCode, new JsonOperations().Serialize(new SerializableError(modelState))) { }

	public WebResult(HttpStatusCode statusCode, string content)
	{
		Content = content;
		StatusCode = statusCode;
	}

	public WebResult(HttpStatusCode statusCode)
	{
		StatusCode = statusCode == HttpStatusCode.OK ? HttpStatusCode.NoContent : statusCode;
	}

	public WebResult() { }

	public Task<WebResult> AsTask()
	{
		return Task.FromResult(this);
	}

	public async Task ExecuteResultAsync(ActionContext context)
	{
		var result = new ContentResult
		{
			Content = Content,
			StatusCode = (int?)StatusCode,
			ContentType = ContentType,
		};

		await result.ExecuteResultAsync(context);
	}

	public T To<T>()
	{
		return JsonContentConverter.ConvertTo<T>(Content);
	}

	public override string ToString()
	{
		return $"StatusCode := <{StatusCode}> | {Content}";
	}
}
