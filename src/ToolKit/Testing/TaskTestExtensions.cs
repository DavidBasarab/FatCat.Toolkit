using FatCat.Testing.Comparers;
using FatCat.Testing.Objects;

namespace FatCat.Toolkit.Testing;

public static class TaskTestExtensions
{
	public static TaskTestAssertions<T> Should<T>(this Task<T> task)
	{
		return new TaskTestAssertions<T>(task);
	}
}

public class TaskTestAssertions<T>(Task<T> subject) : ComparerBase<Task<T>, TaskTestAssertions<T>>(subject)
{
	private ObjectComparer<object> ResultAsObject
	{
		get { return new ObjectComparer<object>(Subject.Result!); }
	}

	public TaskTestAssertions<T> Be(T expectedValue)
	{
		ResultAsObject.Be(expectedValue!);

		return this;
	}

	public TaskTestAssertions<T> BeEquivalentTo(T expectedValue)
	{
		ResultAsObject.BeEquivalentTo(expectedValue!);

		return this;
	}

	public TaskTestAssertions<T> BeFalse()
	{
		ResultAsObject.Be(false);

		return this;
	}

	public TaskTestAssertions<T> BeTrue()
	{
		ResultAsObject.Be(true);

		return this;
	}
}
