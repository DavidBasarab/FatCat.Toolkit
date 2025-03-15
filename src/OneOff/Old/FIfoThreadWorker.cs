using FatCat.Toolkit.Console;
using FatCat.Toolkit.Threading;
using Humanizer;

namespace OneOff.Old;

public class FIfoThreadWorker
{
	private readonly IFifoThreadQueue queue;

	public FIfoThreadWorker(IFifoThreadQueue queue)
	{
		this.queue = queue;
	}

	public async Task DoWork()
	{
		queue.Start();

		queue.Enqueue(async () =>
		{
			await Task.Delay(20.Milliseconds());

			ConsoleLog.WriteCyan("First Task");

			await Task.Delay(120.Milliseconds());

			ConsoleLog.WriteCyan("Exiting First Task");
		});

		queue.Enqueue(async () =>
		{
			await Task.Delay(45.Milliseconds());

			ConsoleLog.WriteMagenta("Second Task");

			await Task.Delay(76.Milliseconds());

			ConsoleLog.WriteMagenta("Exiting Second Task");
		});

		queue.Enqueue(async () =>
		{
			await Task.Delay(37.Milliseconds());

			ConsoleLog.WriteDarkYellow("Third Task");

			await Task.Delay(93.Milliseconds());

			ConsoleLog.WriteDarkYellow("Exiting Third Task");
		});

		ConsoleLog.WriteBlue("After all Enqueue");

		await Task.Delay(4.Seconds());

		queue.Stop();
	}
}
