using System.Diagnostics.CodeAnalysis;
using Humanizer;

namespace FatCat.Toolkit.Threading;

public interface IFatRetry
{
	Task<bool> Execute(Func<Task<bool>> action, int maxRetries = 10, TimeSpan delay = default);

	Task<bool> Execute(Func<bool> action, int maxRetries = 10, TimeSpan delay = default);
}

[ExcludeFromCodeCoverage(Justification = "This is a simple wrapper around a retry pattern")]
public class FatRetry : IFatRetry
{
	public async Task<bool> Execute(Func<Task<bool>> action, int maxRetries = 10, TimeSpan delay = default)
	{
		var attempts = 0;
		bool success;

		if (delay == TimeSpan.Zero)
		{
			delay = 5.Seconds();
		}

		do
		{
			attempts++;

			success = await action();

			if (!success)
			{
				await Task.Delay(delay);
			}
		} while (!success && attempts < maxRetries);

		return success;
	}

	public Task<bool> Execute(Func<bool> action, int maxRetries = 10, TimeSpan delay = default)
	{
		return Execute(() => Task.FromResult(action()), maxRetries, delay);
	}
}
