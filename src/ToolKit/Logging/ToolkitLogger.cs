using FatCat.Toolkit.Console;

namespace FatCat.Toolkit.Logging;

public interface IToolkitLogger
{
	void Debug(string message);

	void Error(string message);

	void Exception(Exception ex);

	void Information(string message);

	void Warning(string message);
}

public class ToolkitLogger : IToolkitLogger
{
	public static bool Enabled { get; set; } = false;

	public void Debug(string message)
	{
		if (Enabled)
		{
			ConsoleLog.WriteGray(message);
		}
	}

	public void Error(string message)
	{
		if (Enabled)
		{
			ConsoleLog.WriteRed(message);
		}
	}

	public void Exception(Exception ex)
	{
		if (Enabled)
		{
			ConsoleLog.WriteException(ex);
		}
	}

	public void Information(string message)
	{
		if (Enabled)
		{
			ConsoleLog.WriteGreen(message);
		}
	}

	public void Warning(string message)
	{
		if (Enabled)
		{
			ConsoleLog.WriteYellow(message);
		}
	}
}
