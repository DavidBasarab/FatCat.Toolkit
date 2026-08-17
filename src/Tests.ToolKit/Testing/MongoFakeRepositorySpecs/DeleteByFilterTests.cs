namespace Tests.FatCat.Toolkit.Testing.MongoFakeRepositorySpecs;

public class DeleteByFilterTests : MongoFakeRepositoryTests
{
	[Fact]
	public async Task ReturnTheConfiguredDeletedCount()
	{
		repository.DeleteByFilterResult = Faker.RandomInt();

		var count = await repository.DeleteByFilter(i => i.Number == item.Number);

		count.Should().Be(repository.DeleteByFilterResult);
	}

	[Fact]
	public async Task CaptureTheDeleteFilter()
	{
		await repository.DeleteByFilter(i => i.Number == item.Number);

		repository.DeleteFilterCapture.Value.Should().Not.BeNull();

		repository.DeleteFilterCapture.Value.Compile()(item).Should().BeTrue();
	}
}
