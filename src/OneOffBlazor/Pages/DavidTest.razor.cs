﻿using Microsoft.AspNetCore.Components;

namespace OneOffBlazor.Pages;

public partial class DavidTest(ITestingService testingService) : ComponentBase
{
	public void DoSomeWork()
	{
		testingService.PrintAMessage();
	}
}
