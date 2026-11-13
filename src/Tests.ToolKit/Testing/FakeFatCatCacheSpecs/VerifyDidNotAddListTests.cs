namespace Tests.FatCat.Toolkit.Testing.FakeFatCatCacheSpecs;

public class VerifyDidNotAddListTests : FakeFatCatCacheTests
{
	[Fact]
	public void FailWhenAnyListWasAddedWithATimeout()
	{
		fake.Add(items, timeout);

		Action verifyAction = () => fake.VerifyDidNotAddList();

		verifyAction.Should().Throw<ExpectationException>();
	}

	[Fact]
	public void FailWhenTheListWasAddedWithATimeout()
	{
		fake.Add(items, timeout);

		Action verifyAction = () => fake.VerifyDidNotAddList(items);

		verifyAction.Should().Throw<ExpectationException>();
	}

	[Fact]
	public void PassForAnyListWhenNothingWasAdded()
	{
		fake.VerifyDidNotAddList();
	}

	[Fact]
	public void PassWhenADifferentListWasAdded()
	{
		fake.Add(otherItems, timeout);

		fake.VerifyDidNotAddList(items);
	}

	[Fact]
	public void PassWhenNothingWasAdded()
	{
		fake.VerifyDidNotAddList(items);
	}
}
