using System.Runtime.CompilerServices;
using FatCat.Toolkit.Extensions;

namespace FatCat.Toolkit.Console;

public static class ConsoleLog
{
	public static IConsoleAccess ConsoleAccess { get; set; } = new SystemConsoleAccess();

	public static bool LogCallerInformation { get; set; } = true;

	public static void Write(
		string message,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0
	)
	{
		Write(ConsoleColor.Gray, message, memberName, sourceFilePath, sourceLineNumber);
	}

	public static void Write(
		ConsoleColor color,
		string message,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0
	)
	{
		var messageToLog =
			$"{DateTime.Now:yyyy-MM-dd HH:mm:ss:fff} | {Path.GetFileName(sourceFilePath)} @ {sourceLineNumber} {memberName} | {message}";

		WriteLineWithColor(color, LogCallerInformation ? messageToLog : message);
	}

	public static void WriteBlue(
		string message,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0
	)
	{
		Write(ConsoleColor.Blue, message, memberName, sourceFilePath, sourceLineNumber);
	}

	public static void WriteCyan(
		string message,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0
	)
	{
		Write(ConsoleColor.Cyan, message, memberName, sourceFilePath, sourceLineNumber);
	}

	public static void WriteDarkBlue(
		string message,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0
	)
	{
		Write(ConsoleColor.DarkBlue, message, memberName, sourceFilePath, sourceLineNumber);
	}

	public static void WriteDarkCyan(
		string message,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0
	)
	{
		Write(ConsoleColor.DarkCyan, message, memberName, sourceFilePath, sourceLineNumber);
	}

	public static void WriteDarkGray(
		string message,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0
	)
	{
		Write(ConsoleColor.DarkGray, message, memberName, sourceFilePath, sourceLineNumber);
	}

	public static void WriteDarkGreen(
		string message,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0
	)
	{
		Write(ConsoleColor.DarkGreen, message, memberName, sourceFilePath, sourceLineNumber);
	}

	public static void WriteDarkMagenta(
		string message,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0
	)
	{
		Write(ConsoleColor.DarkMagenta, message, memberName, sourceFilePath, sourceLineNumber);
	}

	public static void WriteDarkRed(
		string message,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0
	)
	{
		Write(ConsoleColor.DarkRed, message, memberName, sourceFilePath, sourceLineNumber);
	}

	public static void WriteDarkYellow(
		string message,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0
	)
	{
		Write(ConsoleColor.DarkYellow, message, memberName, sourceFilePath, sourceLineNumber);
	}

	public static void WriteEmptyLine()
	{
		WriteLineWithColor(ConsoleColor.Black, string.Empty);
	}

	public static void WriteException(
		Exception exception,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0
	)
	{
		var level = 0;

		var currentException = exception;

		WriteEquals(ConsoleColor.Red);

		while (currentException != null && level < 5)
		{
			level++;

			WriteExceptionForLevel(currentException, level, memberName, sourceFilePath, sourceLineNumber);

			currentException = currentException.InnerException;
		}

		WriteEquals(ConsoleColor.Red, NewLineLocation.Before);
	}

	public static void WriteGray(
		string message,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0
	)
	{
		Write(ConsoleColor.Gray, message, memberName, sourceFilePath, sourceLineNumber);
	}

	public static void WriteGreen(
		string message,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0
	)
	{
		Write(ConsoleColor.Green, message, memberName, sourceFilePath, sourceLineNumber);
	}

	public static void WriteMagenta(
		string message,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0
	)
	{
		Write(ConsoleColor.Magenta, message, memberName, sourceFilePath, sourceLineNumber);
	}

	public static void WriteRed(
		string message,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0
	)
	{
		Write(ConsoleColor.Red, message, memberName, sourceFilePath, sourceLineNumber);
	}

	public static void WriteWhite(
		string message,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0
	)
	{
		Write(ConsoleColor.White, message, memberName, sourceFilePath, sourceLineNumber);
	}

	public static void WriteYellow(
		string message,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0
	)
	{
		Write(ConsoleColor.Yellow, message, memberName, sourceFilePath, sourceLineNumber);
	}

	private static string GetPrintableStackTrace(string spaces, Exception exception)
	{
		return string.IsNullOrEmpty(exception.StackTrace)
			? null
			: exception.StackTrace.Replace(Environment.NewLine, $"{spaces}{Environment.NewLine}                ");
	}

	private static void WriteEquals(ConsoleColor color, NewLineLocation lineLocation = NewLineLocation.None)
	{
		if (lineLocation.IsFlagSet(NewLineLocation.Before))
		{
			WriteEmptyLine();
		}

		WriteLineWithColor(color, new string('=', 125));

		if (lineLocation.IsFlagSet(NewLineLocation.After))
		{
			WriteEmptyLine();
		}
	}

	private static void WriteExceptionForLevel(
		Exception exception,
		int level,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0
	)
	{
		var spaces = new string(' ', level * 4);

		WriteEmptyLine();
		WriteRed($"{spaces}Error     : {exception.Message}", memberName, sourceFilePath, sourceLineNumber);

		WriteRed(
			$"{spaces}StackTrace: {GetPrintableStackTrace(spaces, exception)}",
			memberName,
			sourceFilePath,
			sourceLineNumber
		);
	}

	private static void WriteLineWithColor(ConsoleColor color, string message)
	{
		ConsoleAccess.WriteLineWithColor(color, message);
	}

	private enum NewLineLocation
	{
		None = 0,
		Before = 1,
		After = 2,
		Both = Before | After,
	}
}
