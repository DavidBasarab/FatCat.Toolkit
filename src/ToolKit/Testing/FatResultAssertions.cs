using FatCat.Testing;
using FatCat.Testing.Comparers;
using FatCat.Testing.Objects;

namespace FatCat.Toolkit.Testing;

public static class FatResultAssertionsExtensions
{
	public static FatResultAssertions<T> Should<T>(this Task<FatResult<T>> task)
		where T : class
	{
		var result = task.Result;

		return new FatResultAssertions<T>(result);
	}

	public static FatResultAssertions<T> Should<T>(this FatResult<T> response)
		where T : class
	{
		return new FatResultAssertions<T>(response);
	}
}

public class FatResultAssertions<T>(FatResult<T> subject) : ComparerBase<FatResult<T>, FatResultAssertions<T>>(subject)
	where T : class
{
	private ObjectComparer<FatResult<T>> SubjectAsObject
	{
		get { return new ObjectComparer<FatResult<T>>(Subject); }
	}

	public FatResultAssertions<T> Be(FatResult<T> expectedResult)
	{
		SubjectAsObject.BeEquivalentTo(expectedResult);

		return this;
	}

	public FatResultAssertions<T> Be(T expectedValue)
	{
		SubjectAsObject.Not.BeNull();

		Subject.Data.Should().BeEquivalentTo(expectedValue);

		return this;
	}

	public FatResultAssertions<T> BeSuccessful()
	{
		SubjectAsObject.Not.BeNull();

		Subject.IsSuccessful.Should().BeTrue();

		return this;
	}

	public FatResultAssertions<T> BeUnsuccessful()
	{
		SubjectAsObject.Not.BeNull();

		Subject.IsUnsuccessful.Should().BeTrue();

		return this;
	}
}
