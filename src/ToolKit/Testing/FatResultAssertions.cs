using FluentAssertions;
using FluentAssertions.Primitives;

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

public class FatResultAssertions<T>(FatResult<T> subject)
	: ReferenceTypeAssertions<FatResult<T>, FatResultAssertions<T>>(subject)
{
	protected override string Identifier
	{
		get => "FatResultAssertions";
	}

	public FatResultAssertions<T> Be(FatResult<T> expectedResult)
	{
		new ObjectAssertions(Subject).BeEquivalentTo(expectedResult);

		return this;
	}

	public FatResultAssertions<T> Be(T expectedValue)
	{
		Subject.Should().NotBeNull();

		Subject.Data.Should().BeEquivalentTo(expectedValue);

		return this;
	}

	public FatResultAssertions<T> BeSuccessful()
	{
		Subject.Should().NotBeNull();

		Subject.IsSuccessful.Should().BeTrue();

		return this;
	}

	public FatResultAssertions<T> BeUnsuccessful()
	{
		Subject.Should().NotBeNull();

		Subject.IsUnsuccessful.Should().BeTrue();

		return this;
	}
}
