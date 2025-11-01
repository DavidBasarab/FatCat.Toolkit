using FatCat.Toolkit.Console;
using FatCat.Toolkit.Web;
using Microsoft.AspNetCore.Components;

namespace OneOffBlazor.Pages;

public partial class DavidTest(IWebCallerFactory factory) : ComponentBase
{
	public async Task DoSomeWork()
	{
		try
		{
			// Use the testing service to verify DI and remove unused parameter warning
			var caller = factory.GetWebCaller(new Uri("http://localhost:14555"));

			var badResponse = await caller.Get("request/bad");

			ConsoleLog.Write($"Bad Response: {badResponse.Content}");

			var goodResponse = await caller.Get("request/good");

			ConsoleLog.Write($"Good Response: {goodResponse.Content}");
		}
		catch (Exception ex)
		{
			ConsoleLog.Write(ex.Message);
			ConsoleLog.Write(ex.StackTrace);
		}
	}
}
