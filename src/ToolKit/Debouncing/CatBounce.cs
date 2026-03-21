using DebounceThrottle;

namespace FatCat.Toolkit.Debouncing;

public interface ICatBounce
{
	public void Debounce(Action action);

	public void Throttle(Action action);

	public void ThrottleAsync(Func<Task> action);
}

public class CatBounce(TimeSpan interval) : ICatBounce
{
	private readonly DebounceDispatcher debounceDispatcher = new(interval);
	private readonly ThrottleDispatcher throttleDispatcher = new(interval);

	public void Debounce(Action action)
	{
		debounceDispatcher.Debounce(action);
	}

	public void Throttle(Action action)
	{
		throttleDispatcher.Throttle(action);
	}

	public void ThrottleAsync(Func<Task> action)
	{
		throttleDispatcher.ThrottleAsync(action).Wait();
	}
}
