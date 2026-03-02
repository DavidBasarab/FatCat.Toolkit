using FatCat.Toolkit.Console;
using FatCat.Toolkit.Logging;

namespace OneOff;

public class OneOffLogger : IToolkitLogger
{
	public void Debug(string message)
	{
		ConsoleLog.WriteMagenta($"OneOff | {message}");
	}

	public void Error(string message)
	{
		ConsoleLog.WriteRed($"OneOff | {message}");
	}

	public void Exception(Exception ex)
	{
		ConsoleLog.WriteException(ex);
	}

	public void Information(string message)
	{
		ConsoleLog.WriteCyan($"OneOff | {message}");
	}

	public void Warning(string message)
	{
		ConsoleLog.WriteYellow($"OneOff | {message}");
	}
}
