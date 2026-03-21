using FatCat.Toolkit.WebServer;

namespace Tests.FatCat.Toolkit;

public class TestingEndpoint(TestingModel modelToBeReturned) : Endpoint
{
	public WebResult DoSomeWork()
	{
		return Ok(modelToBeReturned);
	}
}
