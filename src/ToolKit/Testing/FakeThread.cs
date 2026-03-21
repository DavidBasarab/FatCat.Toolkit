using FakeItEasy;
using FatCat.Toolkit.Threading;

namespace FatCat.Toolkit.Testing;

public class FakeThread : IThread
{
	private readonly IThread thread;

	public FakeThread()
	{
		thread = A.Fake<IThread>();

		SetUpRunActionOnCall();
		SetUnRunFuncOnCall();
	}

	public void Run(Action action)
	{
		thread.Run(action);
	}

	public void Run(Func<Task> action)
	{
		thread.Run(action);
	}

	public Task Sleep(TimeSpan sleepTime)
	{
		return thread.Sleep(sleepTime);
	}

	public Task Sleep(TimeSpan sleepTime, CancellationToken token)
	{
		return thread.Sleep(sleepTime, token);
	}

	public void VerifyRunAction()
	{
		A.CallTo(() => thread.Run(A<Action>._)).MustHaveHappened();
	}

	public void VerifyRunFunc()
	{
		A.CallTo(() => thread.Run(A<Func<Task>>._)).MustHaveHappened();
	}

	public void VerifySleep(TimeSpan expectedSleep)
	{
		A.CallTo(() => thread.Sleep(expectedSleep)).MustHaveHappened();
	}

	public void VerifySleep(TimeSpan expectedSleep, CancellationToken token)
	{
		A.CallTo(() => thread.Sleep(expectedSleep, token)).MustHaveHappened();
	}

	private void SetUnRunFuncOnCall()
	{
		A.CallTo(() => thread.Run(A<Func<Task>>._))
			.Invokes(callObject =>
			{
				var passedFunction = callObject.Arguments[0] as Func<Task>;

				passedFunction?.Invoke().Wait();
			});
	}

	private void SetUpRunActionOnCall()
	{
		A.CallTo(() => thread.Run(A<Action>._))
			.Invokes(callObject =>
			{
				var passedAction = callObject.Arguments[0] as Action;

				passedAction?.Invoke();
			});
	}
}
