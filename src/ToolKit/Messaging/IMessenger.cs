namespace FatCat.Toolkit.Messaging;

public interface IMessenger
{
	public void Send<TMessage>(TMessage payload)
		where TMessage : Message;

	public void Subscribe<TMessage>(Func<TMessage, Task> callback)
		where TMessage : Message;

	public void Unsubscribe<TMessage>(Func<TMessage, Task> callback)
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
