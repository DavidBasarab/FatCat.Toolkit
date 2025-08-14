using System.Net;
using FatCat.Toolkit.Json;

namespace FatCat.Toolkit.Web;

public class FatWebResponse<T> : EqualObject
	where T : class
{
	public static FatWebResponse<T> BadRequest()
	{
		return new FatWebResponse<T>(HttpStatusCode.BadRequest);
	}

	public static FatWebResponse<T> BadRequest(string message)
	{
		return new FatWebResponse<T>(HttpStatusCode.BadRequest, message);
	}

	public static FatWebResponse<T> NotFound()
	{
		return new FatWebResponse<T>(HttpStatusCode.NotFound);
	}

	public static FatWebResponse<T> NotImplemented()
	{
		return new FatWebResponse<T>(HttpStatusCode.NotImplemented);
	}

	public static FatWebResponse<T> Ok(T data)
	{
		return new FatWebResponse<T>(HttpStatusCode.OK, data);
	}

	public static FatWebResponse<T> Ok()
	{
		return new FatWebResponse<T>(HttpStatusCode.OK);
	}

	public FatWebResponse BaseResponse { get; }

	public string Content
	{
		get => BaseResponse.Content;
	}

	public string ContentType
	{
		get => BaseResponse.ContentType;
	}

	public T Data
	{
		get => BaseResponse.IsSuccessful ? BaseResponse.To<T>() : null;
	}

	public bool IsSuccessful
	{
		get => BaseResponse.IsSuccessful;
	}

	public bool IsUnsuccessful
	{
		get => !IsSuccessful;
	}

	public HttpStatusCode StatusCode
	{
		get => BaseResponse.StatusCode;
	}

	public FatWebResponse(HttpStatusCode statusCode)
	{
		BaseResponse = new FatWebResponse { StatusCode = statusCode };
	}

	public FatWebResponse(FatWebResponse webResponse)
	{
		BaseResponse = webResponse;
	}

	public FatWebResponse(HttpStatusCode statusCode, string content)
	{
		BaseResponse = new FatWebResponse { StatusCode = statusCode, Content = content };
	}

	public FatWebResponse(HttpStatusCode statusCode, T data)
	{
		BaseResponse = new FatWebResponse
		{
			StatusCode = statusCode,
			Content = new JsonOperations().Serialize(data),
		};
	}

	public override string ToString()
	{
		return $"FatWebResponse | StatusCode <{StatusCode}> | Type {typeof(T).FullName} | {Content}";
	}
}

public class FatWebResponse : EqualObject
{
	public static FatWebResponse BadRequest()
	{
		return new FatWebResponse(HttpStatusCode.BadRequest);
	}

	public static FatWebResponse BadRequest(string message)
	{
		return new FatWebResponse(HttpStatusCode.BadRequest, message);
	}

	public static FatWebResponse NotFound()
	{
		return new FatWebResponse(HttpStatusCode.NotFound);
	}

	public static FatWebResponse NotImplemented()
	{
		return new FatWebResponse(HttpStatusCode.NotImplemented);
	}

	public static FatWebResponse Ok(object data)
	{
		return new FatWebResponse(HttpStatusCode.OK, new JsonOperations().Serialize(data));
	}

	public static FatWebResponse Ok()
	{
		return new FatWebResponse(HttpStatusCode.OK);
	}

	public static FatWebResponse Timeout()
	{
		return new FatWebResponse { StatusCode = HttpStatusCode.RequestTimeout };
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

	public FatWebResponse() { }

	public FatWebResponse(HttpStatusCode statusCode)
	{
		StatusCode = statusCode;
	}

	public FatWebResponse(HttpStatusCode statusCode, string content)
	{
		StatusCode = statusCode;
		Content = content;
	}

	public FatWebResponse(HttpResponseMessage response)
	{
		StatusCode = response.StatusCode;

		httpContent = response.Content;
	}

	public T To<T>()
	{
		return JsonContentConverter.ConvertTo<T>(Content);
	}
}
