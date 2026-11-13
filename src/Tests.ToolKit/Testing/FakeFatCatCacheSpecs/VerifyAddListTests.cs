namespace Tests.FatCat.Toolkit.Testing.FakeFatCatCacheSpecs;

public class VerifyAddListTests : FakeFatCatCacheTests
{
	[Fact]
	public void FailForADifferentList()
	{
		fake.Add(otherItems, timeout);

		Action verifyAction = () => fake.VerifyAddList(items);

		verifyAction.Should().Throw<ExpectationException>();
	}

	[Fact]
	public void FailForADifferentListWithTheMatchingTimeout()
	{
		fake.Add(otherItems, timeout);

		Action verifyAction = () => fake.VerifyAddList(items, timeout);

		verifyAction.Should().Throw<ExpectationException>();
	}

	[Fact]
	public void FailForADifferentTimeout()
	{
		fake.Add(items, timeout);

		Action verifyAction = () => fake.VerifyAddList(items, otherTimeout);

		verifyAction.Should().Throw<ExpectationException>();
	}

	[Fact]
	public void FailForANullTimeoutWhenOneWasGiven()
	{
		fake.Add(items, timeout);

		Action verifyAction = () => fake.VerifyAddList(items, null);

		verifyAction.Should().Throw<ExpectationException>();
	}

	[Fact]
	public void FailForATimeoutWhenNoneWasGiven()
	{
		fake.Add(items);

		Action verifyAction = () => fake.VerifyAddList(items, timeout);

		verifyAction.Should().Throw<ExpectationException>();
	}

	[Fact]
	public void FailForTheTimeoutWhenNothingWasAdded()
	{
		Action verifyAction = () => fake.VerifyAddList(items, timeout);

		verifyAction.Should().Throw<ExpectationException>();
	}

	[Fact]
	public void FailWhenNothingWasAdded()
	{
		Action verifyAction = () => fake.VerifyAddList();

		verifyAction.Should().Throw<ExpectationException>();
	}

	[Fact]
	public void FailWhenOnlyAnItemWasAdded()
	{
		fake.Add(item);

		Action verifyAction = () => fake.VerifyAddList();

		verifyAction.Should().Throw<ExpectationException>();
	}

	[Fact]
	public void PassForAListAddedWithATimeout()
	{
		fake.Add(items, timeout);

		fake.VerifyAddList(items);
	}

	[Fact]
	public void PassForAListAddedWithoutATimeout()
	{
		fake.Add(items);

		fake.VerifyAddList(items);
	}

	[Fact]
	public void PassForANullTimeoutWhenNoneWasGiven()
	{
		fake.Add(items);

		fake.VerifyAddList(items, null);
	}

	[Fact]
	public void PassForAnyListAddedWithATimeout()
	{
		fake.Add(items, timeout);

		fake.VerifyAddList();
	}

	[Fact]
	public void PassForAnyListAddedWithoutATimeout()
	{
		fake.Add(items);

		fake.VerifyAddList();
	}

	[Fact]
	public void PassForTheMatchingTimeout()
	{
		fake.Add(items, timeout);

		fake.VerifyAddList(items, timeout);
	}
}
