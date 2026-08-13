namespace Tests.FatCat.Toolkit.Testing.MongoFakeRepositorySpecs;

public class CountByFilterTests : MongoFakeRepositoryTests
{
	[Fact]
	public async Task CaptureTheCountFilter()
	{
		await repository.CountByFilter(i => i.Number == item.Number);

		repository.CountFilterCapture.Value.Should().Not.BeNull();

		repository.CountFilterCapture.Value.Compile()(item).Should().BeTrue();
	}

	[Fact]
	public async Task ReturnTheConfiguredCount()
	{
		repository.CountByFilterResult = Faker.RandomInt();

		var count = await repository.CountByFilter(i => i.Number == item.Number);

		count.Should().Be(repository.CountByFilterResult);
	}
}
