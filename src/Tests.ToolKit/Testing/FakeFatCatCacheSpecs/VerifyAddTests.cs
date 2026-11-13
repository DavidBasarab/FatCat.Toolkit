namespace Tests.FatCat.Toolkit.Testing.FakeFatCatCacheSpecs;

public class VerifyAddTests : FakeFatCatCacheTests
{
	[Fact]
	public void FailForADifferentItem()
	{
		fake.Add(otherItem, timeout);

		Action verifyAction = () => fake.VerifyAdd(item);

		verifyAction.Should().Throw<ExpectationException>();
	}

	[Fact]
	public void FailForADifferentItemWithTheMatchingTimeout()
	{
		fake.Add(otherItem, timeout);

		Action verifyAction = () => fake.VerifyAdd(item, timeout);

		verifyAction.Should().Throw<ExpectationException>();
	}

	[Fact]
	public void FailForADifferentTimeout()
	{
		fake.Add(item, timeout);

		Action verifyAction = () => fake.VerifyAdd(item, otherTimeout);

		verifyAction.Should().Throw<ExpectationException>();
	}

	[Fact]
	public void FailForANullTimeoutWhenOneWasGiven()
	{
		fake.Add(item, timeout);

		Action verifyAction = () => fake.VerifyAdd(item, null);

		verifyAction.Should().Throw<ExpectationException>();
	}

	[Fact]
	public void FailForATimeoutWhenNoneWasGiven()
	{
		fake.Add(item);

		Action verifyAction = () => fake.VerifyAdd(item, timeout);

		verifyAction.Should().Throw<ExpectationException>();
	}

	[Fact]
	public void FailForTheTimeoutWhenNothingWasAdded()
	{
		Action verifyAction = () => fake.VerifyAdd(item, timeout);

		verifyAction.Should().Throw<ExpectationException>();
	}

	[Fact]
	public void FailWhenNothingWasAdded()
	{
		Action verifyAction = () => fake.VerifyAdd();

		verifyAction.Should().Throw<ExpectationException>();
	}

	[Fact]
	public void PassForAnItemAddedWithATimeout()
	{
		fake.Add(item, timeout);

		fake.VerifyAdd(item);
	}

	[Fact]
	public void PassForANullTimeoutWhenNoneWasGiven()
	{
		fake.Add(item);

		fake.VerifyAdd(item, null);
	}

	[Fact]
	public void PassForAnyItemAddedWithATimeout()
	{
		fake.Add(item, timeout);

		fake.VerifyAdd();
	}

	[Fact]
	public void PassForTheMatchingTimeout()
	{
		fake.Add(item, timeout);

		fake.VerifyAdd(item, timeout);
	}
}
