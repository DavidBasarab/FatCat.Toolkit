namespace Tests.FatCat.Toolkit.Testing.FakeFatCatCacheSpecs;

public class VerifyDidNotAddTests : FakeFatCatCacheTests
{
	[Fact]
	public void FailWhenAnyItemWasAddedWithATimeout()
	{
		fake.Add(item, timeout);

		Action verifyAction = () => fake.VerifyDidNotAdd();

		verifyAction.Should().Throw<ExpectationException>();
	}

	[Fact]
	public void FailWhenAnyItemWasAddedWithoutATimeout()
	{
		fake.Add(item);

		Action verifyAction = () => fake.VerifyDidNotAdd();

		verifyAction.Should().Throw<ExpectationException>();
	}

	[Fact]
	public void FailWhenTheItemWasAddedWithATimeout()
	{
		fake.Add(item, timeout);

		Action verifyAction = () => fake.VerifyDidNotAdd(item);

		verifyAction.Should().Throw<ExpectationException>();
	}

	[Fact]
	public void FailWhenTheItemWasAddedWithoutATimeout()
	{
		fake.Add(item);

		Action verifyAction = () => fake.VerifyDidNotAdd(item);

		verifyAction.Should().Throw<ExpectationException>();
	}

	[Fact]
	public void PassForAnyItemWhenNothingWasAdded()
	{
		fake.VerifyDidNotAdd();
	}

	[Fact]
	public void PassWhenADifferentItemWasAdded()
	{
		fake.Add(otherItem, timeout);

		fake.VerifyDidNotAdd(item);
	}

	[Fact]
	public void PassWhenOnlyAListWasAdded()
	{
		fake.Add(items);

		fake.VerifyDidNotAdd();
	}

	[Fact]
	public void PassWhenNothingWasAdded()
	{
		fake.VerifyDidNotAdd(item);
	}
}
