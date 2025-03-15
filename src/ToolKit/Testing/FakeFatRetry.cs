using FakeItEasy;
using FatCat.Toolkit.Threading;

namespace FatCat.Toolkit.Testing;

public class FakeFatRetry : IFatRetry
{
	private bool executeState;

	public IFatRetry Fake { get; } = A.Fake<IFatRetry>();

	public FakeFatRetry()
	{
		SetUpRunOnCall();
	}

	public Task<bool> Execute(Func<Task<bool>> action, int maxRetries = 10, TimeSpan delay = default)
	{
		return Fake.Execute(action, maxRetries, delay);
	}

	public bool Execute(Func<bool> action, int maxRetries = 10, TimeSpan delay = default)
	{
		return Fake.Execute(action, maxRetries, delay);
	}

	public void SetToExecuteSuccessfully()
	{
		executeState = true;
	}

	public void SetToExecuteUnsuccessfully()
	{
		executeState = false;
	}

	public void VerifyExecuteCalled()
	{
		A.CallTo(() => Fake.Execute(A<Func<bool>>._, A<int>._, A<TimeSpan>._)).MustHaveHappened();
	}

	public void VerifyExecuteCalledAsync()
	{
		A.CallTo(() => Fake.Execute(A<Func<Task<bool>>>._, A<int>._, A<TimeSpan>._)).MustHaveHappened();
	}

	private void SetUpRunOnCall()
	{
		A.CallTo(() => Fake.Execute(A<Func<Task<bool>>>._, A<int>._, A<TimeSpan>._))
			.Invokes(callObject =>
			{
				var action = callObject.GetArgument<Func<Task<bool>>>(0);

				action.Invoke().Wait();
			})
			.ReturnsLazily(() => Task.FromResult(executeState));

		A.CallTo(() => Fake.Execute(A<Func<bool>>._, A<int>._, A<TimeSpan>._))
			.Invokes(callObject =>
			{
				var action = callObject.GetArgument<Func<bool>>(0);

				action.Invoke();
			})
			.ReturnsLazily(() => executeState);
	}
}
