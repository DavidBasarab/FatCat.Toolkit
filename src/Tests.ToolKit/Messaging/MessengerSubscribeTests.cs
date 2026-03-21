using FatCat.Toolkit.Messaging;
using FatCat.Toolkit.Testing;

namespace Tests.FatCat.Toolkit.Messaging;

public class MessengerSubscribeTests
{
	private bool wasCallback1Hit;
	private bool wasCallback2Hit;

	public MessengerSubscribeTests()
	{
		Messenger.Thread = new FakeThread();
	}

	[Fact]
	public void CanRegisterForAMessageAndHitCallback()
	{
		Messenger.Subscribe<TestMessage1>(OnTestMessage1Callback);

		Messenger.Send(new TestMessage1());

		wasCallback1Hit.Should().BeTrue();
	}

	[Fact]
	public void CanUSubscribeFromMessenger()
	{
		Messenger.Subscribe<TestMessage2>(OnTestMessage2Callback);
		Messenger.Unsubscribe<TestMessage2>(OnTestMessage2Callback);

		Messenger.Send(new TestMessage2());

		wasCallback2Hit.Should().BeFalse();
	}

	private Task OnTestMessage1Callback(TestMessage1 arg)
	{
		wasCallback1Hit = true;

		return Task.CompletedTask;
	}

	private Task OnTestMessage2Callback(TestMessage2 arg)
	{
		wasCallback2Hit = true;

		return Task.CompletedTask;
	}

	public class TestMessage1 : Message { }

	public class TestMessage2 : Message { }
}
