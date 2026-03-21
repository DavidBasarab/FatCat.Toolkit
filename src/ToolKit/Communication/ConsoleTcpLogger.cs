using System.Runtime.CompilerServices;
using FatCat.Toolkit.Console;

namespace FatCat.Toolkit.Communication;

public interface IFatTcpLogger
{
	public void WriteDebug(
		string message,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0
	);

	public void WriteError(
		string message,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0
	);

	public void WriteException(Exception ex);

	public void WriteInformation(
		string message,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0
	);

	public void WriteWarning(
		string message,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0
	);
}

public class ConsoleFatTcpLogger : IFatTcpLogger
{
	public void WriteDebug(
		string message,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0
	)
	{
		ConsoleLog.Write(message, memberName, sourceFilePath, sourceLineNumber);
	}

	public void WriteError(
		string message,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0
	)
	{
		ConsoleLog.WriteRed(message, memberName, sourceFilePath, sourceLineNumber);
	}

	public void WriteException(Exception ex)
	{
		ConsoleLog.WriteException(ex);
	}

	public void WriteInformation(
		string message,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0
	)
	{
		ConsoleLog.WriteMagenta(message, memberName, sourceFilePath, sourceLineNumber);
	}

	public void WriteWarning(
		string message,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0
	)
	{
		ConsoleLog.WriteYellow(message, memberName, sourceFilePath, sourceLineNumber);
	}
}
