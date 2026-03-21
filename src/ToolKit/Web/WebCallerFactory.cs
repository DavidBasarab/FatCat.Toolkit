using FatCat.Toolkit.Json;
using FatCat.Toolkit.Logging;

namespace FatCat.Toolkit.Web;

public interface IWebCallerFactory
{
	public IWebCaller GetWebCaller(Uri baseUri);
}

public class WebCallerFactory(IToolkitLogger toolkitLogger, IJsonOperations jsonOperations) : IWebCallerFactory
{
	public IWebCaller GetWebCaller(Uri baseUri)
	{
		return new WebCaller(baseUri, jsonOperations, toolkitLogger);
	}
}
