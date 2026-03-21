using System.Collections.Concurrent;
using FatCat.Toolkit.Injection;
using FatCat.Toolkit.Logging;
using FatCat.Toolkit.Threading;
using Thread = FatCat.Toolkit.Threading.Thread;

namespace FatCat.Toolkit.Messaging;

public static class Messenger
{
	private static readonly ConcurrentDictionary<Type, object> subscribers = new();

	private static IThread thread;

	public static IThread Thread
	{
		get
		{
			if (thread is not null)
			{
				return thread;
			}

			if (SystemScope.Container.TryResolve(typeof(IThread), out var itemThread))
			{
				thread = itemThread as IThread;

				return thread;
			}

			thread = new Thread(new ToolkitLogger());

			return thread;
		}
		set
		{
			thread = value;
		}
	}

	public static void Send<TMessage>(TMessage payload)
		where TMessage : Message
	{
		if (subscribers.TryGetValue(typeof(TMessage), out var bag))
		{
			var holder = bag as MessageCallbackHolder<TMessage>;

			foreach (var callback in holder.GetAll())
			{
				Thread.Run(async () => await callback(payload));
			}
		}
	}

	public static void Subscribe<TMessage>(Func<TMessage, Task> callback)
		where TMessage : Message
	{
		subscribers.AddOrUpdate(
			typeof(TMessage),
			_ => new MessageCallbackHolder<TMessage>(callback),
			(_, bag) =>
			{
				var holder = bag as MessageCallbackHolder<TMessage>;

				holder.Add(callback);

				return bag;
			}
		);
	}

	public static void Unsubscribe<TMessage>(Func<TMessage, Task> callback)
		where TMessage : Message
	{
		if (subscribers.TryGetValue(typeof(TMessage), out var bag))
		{
			var holder = bag as MessageCallbackHolder<TMessage>;

			holder.Remove(callback);
		}
	}
}
