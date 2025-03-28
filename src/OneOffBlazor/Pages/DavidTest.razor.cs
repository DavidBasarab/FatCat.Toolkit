using Microsoft.AspNetCore.Components;
using Xunit.Abstractions;

namespace OneOffBlazor.Pages;

public partial class DavidTest(ITestingService testingService) : ComponentBase
{
	public void DoSomeWork()
	{
		testingService.PrintAMessage();
	}
}
