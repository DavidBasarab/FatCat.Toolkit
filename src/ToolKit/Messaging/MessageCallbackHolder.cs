using System.Collections.Concurrent;

namespace FatCat.Toolkit.Messaging;

internal class MessageCallbackHolder<TMessage>
	where TMessage : Message
{
	private readonly ConcurrentBag<Func<TMessage, Task>> callbacks = new();

	public MessageCallbackHolder(Func<TMessage, Task> callback)
	{
		callbacks.Add(callback);
	}

	public void Add(Func<TMessage, Task> callback)
	{
		callbacks.Add(callback);
	}

	public IEnumerable<Func<TMessage, Task>> GetAll()
	{
		return callbacks;
	}

	public void Remove(Func<TMessage, Task> callback)
	{
		var newBag = new ConcurrentBag<Func<TMessage, Task>>();

		Parallel.ForEach(
			callbacks,
			currentItem =>
			{
				if (!EqualityComparer<Func<TMessage, Task>>.Default.Equals(currentItem, callback))
				{
					newBag.Add(currentItem);
				}
			}
		);

		callbacks.Clear();

		foreach (var newItem in newBag)
		{
			callbacks.Add(newItem);
		}
	}
}
