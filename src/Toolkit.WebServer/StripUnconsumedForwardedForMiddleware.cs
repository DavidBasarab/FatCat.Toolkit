using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace FatCat.Toolkit.WebServer;

public class StripUnconsumedForwardedForMiddleware(RequestDelegate next, IOptions<ForwardedHeadersOptions> options)
{
	public Task Invoke(HttpContext context)
	{
		context.Request.Headers.Remove(options.Value.ForwardedForHeaderName);

		return next(context);
	}
}
