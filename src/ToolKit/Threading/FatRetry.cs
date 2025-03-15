using System.Diagnostics.CodeAnalysis;
using Humanizer;

namespace FatCat.Toolkit.Threading;

public interface IFatRetry
{
	/// <summary>
	/// Will execute async function until it returns true or maxRetries is reached
	/// </summary>
	/// <param name="action">Action to execute</param>
	/// <param name="maxRetries">Max retries defaults to 10</param>
	/// <param name="delay">Delay between retries defaults to 5 seconds</param>
	/// <returns></returns>
	Task<bool> Execute(Func<Task<bool>> action, int maxRetries = 10, TimeSpan delay = default);

	/// <summary>
	/// Will execute function until it returns true or maxRetries is reached
	/// </summary>
	/// <param name="action">Action to execute</param>
	/// <param name="maxRetries">Max retries defaults to 10</param>
	/// <param name="delay">Delay between retries defaults to 5 seconds</param>
	/// <returns></returns>
	bool Execute(Func<bool> action, int maxRetries = 10, TimeSpan delay = default);
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

	public bool Execute(Func<bool> action, int maxRetries = 10, TimeSpan delay = default)
	{
		return Execute(() => Task.FromResult(action()), maxRetries, delay).Result;
	}
}
