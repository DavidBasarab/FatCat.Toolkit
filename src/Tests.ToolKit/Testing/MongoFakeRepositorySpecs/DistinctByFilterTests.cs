namespace Tests.FatCat.Toolkit.Testing.MongoFakeRepositorySpecs;

public class DistinctByFilterTests : MongoFakeRepositoryTests
{
	[Fact]
	public async Task CaptureTheDistinctFilter()
	{
		repository.SetUpDistinct(Faker.Create<List<string>>(3));

		await repository.DistinctByFilter(i => i.Name, i => i.Number == item.Number);

		repository.DistinctFilterCapture.Value.Should().Not.BeNull();

		repository.DistinctFilterCapture.Value.Compile()(item).Should().BeTrue();
	}

	[Fact]
	public async Task ReturnNoDistinctValuesWhenNoneWereSetUp()
	{
		var values = await repository.DistinctByFilter(i => i.Name, i => i.Number == item.Number);

		var aListWasReturned = values is not null;

		aListWasReturned.Should().BeTrue();

		values.Should().BeEmpty();
	}

	[Fact]
	public async Task ReturnTheConfiguredDistinctValues()
	{
		var names = Faker.Create<List<string>>(3);

		repository.SetUpDistinct(names);

		var values = await repository.DistinctByFilter(i => i.Name, i => i.Number == item.Number);

		values.Should().Equal(names);
	}
}
