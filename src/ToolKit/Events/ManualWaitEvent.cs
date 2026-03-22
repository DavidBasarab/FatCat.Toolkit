namespace FatCat.Toolkit.Events;

/// <summary>
///  Used for waiting for triggers in threading
/// </summary>
public interface IManualWaitEvent : IDisposable
{
	public bool HasBeenTriggered { get; }

	/// <summary>
	///  Trigger the event.  If in wait will make it return true to keep with the execution
	/// </summary>
	public void Trigger();

	/// <summary>
	///  Will wait until time out is reached.
	/// </summary>
	/// <param name="timeout">Max Time to wait</param>
	/// <returns>
	///  True => if wait event is triggered
	///  False => if not triggered
	/// </returns>
	public bool Wait(TimeSpan? timeout = null);
}

public class ManualWaitEvent : IManualWaitEvent
{
	private readonly ManualResetEvent manualResetEvent = new(false);

	public bool HasBeenTriggered
	{
		get { return manualResetEvent.WaitOne(1); }
	}

	public void Dispose()
	{
		manualResetEvent.Dispose();
	}

	public void Trigger()
	{
		manualResetEvent.Set();
	}

	public bool Wait(TimeSpan? timeout)
	{
		return timeout.HasValue ? manualResetEvent.WaitOne(timeout.Value) : manualResetEvent.WaitOne();
	}
}
