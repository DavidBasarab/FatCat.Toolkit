using FatCat.Toolkit.Logging;

namespace FatCat.Toolkit.Threading;

public interface IThread
{
	public void Run(Action action);

	public void Run(Func<Task> action);

	public Task Sleep(TimeSpan sleepTime);

	public Task Sleep(TimeSpan sleepTime, CancellationToken token);
}

public class Thread(IToolkitLogger logger) : IThread
{
	public void Run(Func<Task> action)
	{
		Task.Run(async () =>
		{
			try
			{
				await action();
			}
			catch (Exception ex)
			{
				HandleException(ex);
			}
		});
	}

	public void Run(Action action)
	{
		Task.Run(() =>
		{
			try
			{
				action();
			}
			catch (Exception ex)
			{
				HandleException(ex);
			}
		});
	}

	public Task Sleep(TimeSpan sleepTime)
	{
		return Task.Delay(sleepTime);
	}

	public Task Sleep(TimeSpan sleepTime, CancellationToken token)
	{
		return Task.Delay(sleepTime, token);
	}

	private void HandleException(Exception ex)
	{
		logger.Error("Exception running background task.");

		logger.Exception(ex);
	}
}
