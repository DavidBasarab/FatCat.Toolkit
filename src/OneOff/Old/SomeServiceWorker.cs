using FatCat.Toolkit.Console;

namespace OneOff.Old;

public interface ISomeServiceWorker
{
	void DoSomeWork();
}

public class SomeServiceWorker : ISomeServiceWorker
{
	public void DoSomeWork()
	{
		ConsoleLog.WriteGreen("This will do some work");
		ConsoleLog.WriteCyan("This is another color and stuff is going on");
	}
}
