namespace FatCat.Toolkit.Events;

/// <summary>
///  An event that will auto reset when triggered
/// </summary>
public interface IAutoWaitEvent : IDisposable
{
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

public class AutoWaitEvent : IAutoWaitEvent
{
	private readonly AutoResetEvent autoResetEvent = new(false);

	public void Dispose()
	{
		autoResetEvent.Dispose();
	}

	public void Trigger()
	{
		autoResetEvent.Set();
	}

	public bool Wait(TimeSpan? timeout)
	{
		return timeout.HasValue ? autoResetEvent.WaitOne(timeout.Value) : autoResetEvent.WaitOne();
	}
}
