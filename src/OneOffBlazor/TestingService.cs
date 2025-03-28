using FatCat.Toolkit.Console;

namespace OneOffBlazor;

public interface ITestingService
{
	void PrintAMessage();
}

public class TestingService : ITestingService
{
	public void PrintAMessage()
	{
		ConsoleLog.WriteMagenta("This will be a message for a console app.");
	}
}

public class OtherTestingService : ITestingService
{
	private int counter;

	public void PrintAMessage()
	{
		counter++;

		ConsoleLog.WriteMagenta($"This will be a message for a console app. Counter: {counter}");
	}
}
