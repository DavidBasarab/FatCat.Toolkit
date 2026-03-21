#nullable enable
using System.Runtime.CompilerServices;

namespace FatCat.Toolkit.Logging;

/// <summary>
///  Will write a {ApplicationName}.log file to executing directory to be used as a quick and easy way
///  to log messages to the file system without a lot of setup
/// </summary>
public interface ISimpleLogger
{
	public void SetLogLevel(LogLevel logLevel);

	public void Write(
		LogLevel logLevel,
		string message,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0
	);

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

	public void WriteFatal(
		string message,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0
	);

	public void WriteInformation(
		string message,
		[CallerMemberName] string memberName = "",
		[CallerFilePath] string sourceFilePath = "",
		[CallerLineNumber] int sourceLineNumber = 0
	);

	public void WriteVerbose(
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

public class SimpleLogger(IApplicationTools applicationTools, string? logName = null) : ISimpleLogger
{
	private readonly string? logName = logName ?? applicationTools.ExecutableName;

	private LogLevel logLevel = LogLevel.Information;

	public void SetLogLevel(LogLevel logLevel)
	{
		this.logLevel = logLevel;
	}

	public void Write(
		LogLevel logLevel,
		string message,
		string memberName = "",
		string sourceFilePath = "",
		int sourceLineNumber = 0
	)
	{
		var fullMessage =
			$"{DateTime.Now} | {logLevel} | {Path.GetFileName(sourceFilePath)} @ {sourceLineNumber} {memberName} | {message}";

		var logFileFullPath = Path.Join(applicationTools.ExecutingDirectory, $"{logName}.log");

		if (!File.Exists(logFileFullPath))
		{
			var createStream = File.Create(logFileFullPath);

			createStream.Dispose();
		}

		using var fileStream = File.Open(logFileFullPath, FileMode.Append);

		using var streamWriter = new StreamWriter(fileStream);

		streamWriter.WriteLine(fullMessage);
	}

	public void WriteDebug(
		string message,
		string memberName = "",
		string sourceFilePath = "",
		int sourceLineNumber = 0
	)
	{
		Write(LogLevel.Debug, message, memberName, sourceFilePath, sourceLineNumber);
	}

	public void WriteError(
		string message,
		string memberName = "",
		string sourceFilePath = "",
		int sourceLineNumber = 0
	)
	{
		Write(LogLevel.Error, message, memberName, sourceFilePath, sourceLineNumber);
	}

	public void WriteFatal(
		string message,
		string memberName = "",
		string sourceFilePath = "",
		int sourceLineNumber = 0
	)
	{
		Write(LogLevel.Fatal, message, memberName, sourceFilePath, sourceLineNumber);
	}

	public void WriteInformation(
		string message,
		string memberName = "",
		string sourceFilePath = "",
		int sourceLineNumber = 0
	)
	{
		Write(LogLevel.Information, message, memberName, sourceFilePath, sourceLineNumber);
	}

	public void WriteVerbose(
		string message,
		string memberName = "",
		string sourceFilePath = "",
		int sourceLineNumber = 0
	)
	{
		Write(LogLevel.Verbose, message, memberName, sourceFilePath, sourceLineNumber);
	}

	public void WriteWarning(
		string message,
		string memberName = "",
		string sourceFilePath = "",
		int sourceLineNumber = 0
	)
	{
		Write(LogLevel.Warning, message, memberName, sourceFilePath, sourceLineNumber);
	}
}
