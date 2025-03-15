using FatCat.Toolkit.Console;
using FatCat.Toolkit.Threading;

namespace OneOff;

public class RetryWorker(IFatRetry fatRetry)
{
	private int maxRetry = 5;
	private int counter;

	public bool TestMethod()
	{
		counter++;

		ConsoleLog.WriteCyan($"Doing attempt {counter}");

		return counter >= maxRetry;
	}

	public async Task<bool> TestMethodAsync()
	{
		await Task.CompletedTask;

		return TestMethod();
	}

	public async Task DoWork()
	{
		ConsoleLog.WriteMagenta("Testing FatRetry");

		var result = await fatRetry.Execute(TestMethod);

		ConsoleLog.WriteMagenta($"Result of FatRetry: {result}");
	}
}
