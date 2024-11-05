using FatCat.Fakes;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.Web;
using Newtonsoft.Json;

namespace OneOffToolkitOnly;

public class TestCallerIssueWorker(IWebCallerFactory webCallerFactory)
{
	public async Task DoWork(string[] args)
	{
		await Task.CompletedTask;

		/*
		* var webCallerFactory = SystemScope.Container.Resolve<IWebCallerFactory>();

		return webCallerFactory.GetWebCaller(new Uri(TestData.BrumeUrl));
		*/

		var webCaller = webCallerFactory.GetWebCaller(new Uri("http://localhost:14555/api"));

		var request = Faker.Create<TestRequest>();

		var json = JsonConvert.SerializeObject(request, Formatting.Indented);

		var response = await webCaller.Post("jesus", request);

		if (response.IsSuccessful)
		{
			ConsoleLog.WriteGreen("Success");
		}
		else
		{
			ConsoleLog.WriteRed($"Failed | <{response.StatusCode}>");
		}
	}

	private class TestRequest
	{
		public string MessageText { get; set; }

		public List<string> UserIds { get; set; }
	}
}
