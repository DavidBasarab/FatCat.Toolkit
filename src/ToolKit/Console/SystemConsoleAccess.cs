namespace FatCat.Toolkit.Console;

public interface IConsoleAccess
{
	void WriteLineWithColor(ConsoleColor color, string message);
}

public class SystemConsoleAccess : IConsoleAccess
{
	private readonly Lock lockObject = new();

	public void WriteLineWithColor(ConsoleColor color, string message)
	{
		lock (lockObject)
		{
			var oldColor = System.Console.ForegroundColor;

			System.Console.ForegroundColor = color;

			System.Console.WriteLine(message);

			System.Console.ForegroundColor = oldColor;
		}
	}
}

public class NoOpConsoleAccess : IConsoleAccess
{
	public void WriteLineWithColor(ConsoleColor color, string message) { }
}

public class NoColorConsoleAccess : IConsoleAccess
{
	public void WriteLineWithColor(ConsoleColor color, string message)
	{
		System.Console.WriteLine(message);
	}
}
