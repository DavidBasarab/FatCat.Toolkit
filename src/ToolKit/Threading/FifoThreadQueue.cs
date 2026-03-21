using System.Collections.Concurrent;
using FatCat.Toolkit.Logging;

namespace FatCat.Toolkit.Threading;

public interface IFifoThreadQueue : IDisposable
{
	public CancellationToken CancelToken { get; }

	public int QueueCount { get; }

	public void Enqueue(Action actionToQueue);

	public void Enqueue(Func<Task> actionToQueue);

	public void Next();

	public void Start();

	public void Stop();
}

public class FifoThreadQueue : IFifoThreadQueue
{
	private readonly IToolkitLogger logger;
	private readonly ConcurrentQueue<Action> queue = new();
	private readonly AutoResetEvent queueEvent = new(false);
	private bool active = true;
	private CancellationTokenSource cancelSource = new();
	private System.Threading.Thread deQueueThread = null!;
	private bool disposed;
	private TaskFactory taskFactory;

	public CancellationToken CancelToken { get; set; }

	public int QueueCount
	{
		get => queue.Count;
	}

	public FifoThreadQueue()
		: this(new ToolkitLogger()) { }

	public FifoThreadQueue(IToolkitLogger logger)
	{
		this.logger = logger;

		CancelToken = cancelSource.Token;

		taskFactory = new TaskFactory(CancelToken);
	}

	public void Dispose()
	{
		if (disposed)
		{
			return;
		}

		disposed = true;

		Stop();

		queueEvent?.Dispose();
	}

	public void Enqueue(Action actionToQueue)
	{
		queue.Enqueue(actionToQueue);

		queueEvent.Set();
	}

	public void Enqueue(Func<Task> actionToQueue)
	{
		Enqueue(() =>
		{
			try
			{
				// taskFactory.StartNew(actionToQueue, cancelToken);

				actionToQueue().Wait(CancelToken);
			}
			catch (TaskCanceledException) { }
			catch (OperationCanceledException) { }
			catch (Exception ex)
			{
				logger.Exception(ex);
			}
		});
	}

	public void Next()
	{
		if (queue.Count == 1)
		{
			return;
		}

		// Just dequeue the next action no reason that will skip to next if there is any
		Dequeue();
	}

	public void Start()
	{
		Stop();

		CreateCancelToken();

		active = true;

		StartExecutionThread();
	}

	public void Stop()
	{
		active = false;

		queue.Clear();

		queueEvent?.Set();

		cancelSource?.Cancel(false);
	}

	protected virtual void ExecuteAction(Action actionToExecute)
	{
		try
		{
			actionToExecute?.Invoke();
		}
		catch (Exception ex)
		{
			logger.Exception(ex);
		}
	}

	private void CreateCancelToken()
	{
		cancelSource = new CancellationTokenSource();
		CancelToken = cancelSource.Token;
	}

	private Action Dequeue()
	{
		queue.TryDequeue(out var result);

		return result!;
	}

	private void ExecuteAllActionsInQueue()
	{
		while (QueueCount > 0)
		{
			var actionToExecute = Dequeue();

			ExecuteAction(actionToExecute);
		}
	}

	private void ExecutionThread()
	{
		while (active)
		{
			queueEvent.WaitOne();

			ExecuteAllActionsInQueue();
		}
	}

	private void StartExecutionThread()
	{
		ThreadStart threadStart = ExecutionThread;

		deQueueThread = new System.Threading.Thread(threadStart);

		deQueueThread.Start();
	}
}
