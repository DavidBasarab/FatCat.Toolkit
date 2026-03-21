#nullable enable
using FatCat.Toolkit.Events;

namespace FatCat.Toolkit.Console;

public interface IConsoleUtilities
{
	public void Exit();

	/// <summary>
	///  Will hook up to Console events to wait for the user to hit Ctrl-C
	///  to exit
	/// </summary>
	/// <param name="onExit">Callback function to be triggered when Ctrl-C is hit</param>
	public void WaitForExit(Action? onExit = null);
}

public class ConsoleUtilities(IManualWaitEvent stopEvent) : IConsoleUtilities
{
	public void Exit()
	{
		OnCancel(null, null);
	}

	public void WaitForExit(Action? onExit = null)
	{
		System.Console.CancelKeyPress += OnCancel;

		// Spin for 10 ms to keep getting feedback if user hit Ctrl-C
		while (!stopEvent.Wait(TimeSpan.FromMilliseconds(10))) { }

		onExit?.Invoke();
	}

	private void OnCancel(object? sender, ConsoleCancelEventArgs? e)
	{
		if (e != null)
		{
			e.Cancel = true;
		}

		stopEvent.Trigger();
	}
}
