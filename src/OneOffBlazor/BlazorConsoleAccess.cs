using FatCat.Toolkit.Console;

namespace OneOffBlazor;

public class BlazorConsoleAccess : IConsoleAccess
{
	public void WriteLineWithColor(ConsoleColor color, string message)
	{
		Console.WriteLine(message);
	}
}
