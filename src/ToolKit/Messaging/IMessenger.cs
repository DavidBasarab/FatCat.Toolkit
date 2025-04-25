namespace FatCat.Toolkit.Messaging;

public interface IMessenger
{
	void Send<TMessage>(TMessage payload)
		where TMessage : Message;

	void Subscribe<TMessage>(Func<TMessage, Task> callback)
		where TMessage : Message;

	void Unsubscribe<TMessage>(Func<TMessage, Task> callback)
		where TMessage : Message;
}

public class MessengerImplementation : IMessenger
{
	public void Send<TMessage>(TMessage payload)
		where TMessage : Message
	{
		Messenger.Send(payload);
	}

	public void Subscribe<TMessage>(Func<TMessage, Task> callback)
		where TMessage : Message
	{
		Messenger.Subscribe(callback);
	}

	public void Unsubscribe<TMessage>(Func<TMessage, Task> callback)
		where TMessage : Message
	{
		Messenger.Unsubscribe(callback);
	}
}
