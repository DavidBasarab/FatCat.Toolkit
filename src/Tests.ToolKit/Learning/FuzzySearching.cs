using FatCat.Toolkit;
using FatCat.Toolkit.Extensions;

namespace Tests.FatCat.Toolkit.Learning;

public class FuzzySearching
{
	private readonly List<SearchObject> searchList =
	[
		new()
		{
			FirstName = "Joe",
			LastName = "Burrow"
		},
		new()
		{
			FirstName = "Ja'Marr",
			LastName = "Chase"
		},
		new()
		{
			FirstName = "Joe",
			LastName = "Mixon"
		},
		new()
		{
			FirstName = "Joe",
			LastName = "Montana"
		},
		new()
		{
			FirstName = "Zack",
			LastName = "Taylor"
		},
		new()
		{
			FirstName = "Jason",
			LastName = "Taylor"
		},
		new()
		{
			FirstName = "Trader",
			LastName = "Joe"
		},
		new()
		{
			FirstName = "Taylor",
			LastName = "Zack"
		},
		new()
		{
			FirstName = "Taylor",
			LastName = "Jason"
		},
	];

	[Fact]
	public void CanFindAllTheJoeOnFirstName()
	{
		var search = "Joe";

		var foundJoe = searchList.FuzzySearch(search, x => x.FirstName);

		foundJoe.Count.Should().Be(3);
	}

	[Fact]
	public void CanFindBasedOnPartialName()
	{
		var search = "tay";

		var foundJoe = searchList.FuzzySearch(search, x => x.LastName);

		foundJoe.Count.Should().Be(2);
	}

	[Fact]
	public void WillSearchBothFirstAndLastNames()
	{
		var search = "tay";

		var result = searchList.FuzzySearch(search, x => $"{x.FirstName} {x.LastName}");

		result.Count.Should().Be(4);

		result.Should()
			.ContainEquivalentOf(new SearchObject
								{
									FirstName = "Zack",
									LastName = "Taylor"
								});

		result.Should()
			.ContainEquivalentOf(new SearchObject
								{
									FirstName = "Jason",
									LastName = "Taylor"
								});

		result.Should()
			.ContainEquivalentOf(new SearchObject
								{
									FirstName = "Taylor",
									LastName = "Zack"
								});

		result.Should()
			.ContainEquivalentOf(new SearchObject
								{
									FirstName = "Taylor",
									LastName = "Jason"
								});
	}

	private class SearchObject : EqualObject
	{
		public string FirstName { get; set; }

		public string LastName { get; set; }
	}
}