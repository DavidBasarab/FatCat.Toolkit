using System.Diagnostics.CodeAnalysis;
using FatCat.Toolkit.Threading;

namespace FatCat.Toolkit;

public interface IFatWaiter
{
	Task Wait(Func<bool> condition, TimeSpan interval);

	Task Wait(Func<bool> condition, TimeSpan interval, TimeSpan maxWaitTime);

	Task Wait(Func<bool> condition, TimeSpan interval, CancellationToken cancellationToken);

	Task Wait(Func<bool> condition, TimeSpan interval, TimeSpan maxWaitTime, CancellationToken cancellationToken);

	Task Wait(Func<Task<bool>> condition, TimeSpan interval);

	Task Wait(Func<Task<bool>> condition, TimeSpan interval, TimeSpan maxWaitTime);

	Task Wait(Func<Task<bool>> condition, TimeSpan interval, CancellationToken cancellationToken);

	Task Wait(
		Func<Task<bool>> condition,
		TimeSpan interval,
		TimeSpan maxWaitTime,
		CancellationToken cancellationToken
	);
}

[ExcludeFromCodeCoverage(Justification = "doing wait loops loops is hard to test")]
public class FatWaiter(IThread thread) : IFatWaiter
{
	public Task Wait(Func<bool> condition, TimeSpan interval)
	{
		return Wait(condition, interval, CancellationToken.None);
	}

	public Task Wait(Func<bool> condition, TimeSpan interval, TimeSpan maxWaitTime)
	{
		return Wait(condition, interval, maxWaitTime, CancellationToken.None);
	}

	public async Task Wait(Func<bool> condition, TimeSpan interval, CancellationToken cancellationToken)
	{
		while (!condition())
		{
			await thread.Sleep(interval, cancellationToken);
		}
	}

	public Task Wait(
		Func<bool> condition,
		TimeSpan interval,
		TimeSpan maxWaitTime,
		CancellationToken cancellationToken
	)
	{
		var startTime = DateTime.UtcNow;

		return Wait(() => condition() || DateTime.UtcNow - startTime > maxWaitTime, interval, cancellationToken);
	}

	public Task Wait(Func<Task<bool>> condition, TimeSpan interval)
	{
		return Wait(condition, interval, CancellationToken.None);
	}

	public Task Wait(Func<Task<bool>> condition, TimeSpan interval, TimeSpan maxWaitTime)
	{
		return Wait(condition, interval, maxWaitTime, CancellationToken.None);
	}

	public async Task Wait(Func<Task<bool>> condition, TimeSpan interval, CancellationToken cancellationToken)
	{
		while (!await condition())
		{
			await thread.Sleep(interval, cancellationToken);
		}
	}

	public Task Wait(
		Func<Task<bool>> condition,
		TimeSpan interval,
		TimeSpan maxWaitTime,
		CancellationToken cancellationToken
	)
	{
		var startTime = DateTime.UtcNow;

		return Wait(
			async () => await condition() || DateTime.UtcNow - startTime > maxWaitTime,
			interval,
			cancellationToken
		);
	}
}
