using FatCat.Toolkit.Data.Mongo;
using Tests.FatCat.Toolkit.Data.Mongo;

namespace Tests.FatCat.Toolkit.Testing.MongoFakeRepositorySpecs;

public class QueryByFilterTests : MongoFakeRepositoryTests
{
	[Fact]
	public async Task ReturnTheConfiguredPage()
	{
		repository.QueryByFilterResult = new PagedResults<TestingMongoObject>
		{
			Items = Faker.Create<List<TestingMongoObject>>(3),
			TotalCount = Faker.RandomInt(),
		};

		repository.SetUpQuery<int>();

		var page = await repository.QueryByFilter(i => i.Number == item.Number, i => i.Number, true, 0, 10);

		page.Should().Be(repository.QueryByFilterResult);
	}

	[Fact]
	public async Task ReturnAnEmptyPageWhenNoneWasSetUp()
	{
		var page = await repository.QueryByFilter(i => i.Number == item.Number, i => i.Number, true, 0, 10);

		var aPageWasReturned = page is not null;

		aPageWasReturned.Should().BeTrue();

		page.Items.Should().BeEmpty();

		page.TotalCount.Should().BeZero();
	}

	[Fact]
	public async Task CaptureTheQueryFilter()
	{
		repository.SetUpQuery<int>();

		await repository.QueryByFilter(i => i.Number == item.Number, i => i.Number, true, 0, 10);

		repository.QueryFilterCapture.Value.Should().Not.BeNull();

		repository.QueryFilterCapture.Value.Compile()(item).Should().BeTrue();
	}

	[Fact]
	public async Task CaptureThePagingArguments()
	{
		var skip = Faker.RandomInt();
		var limit = Faker.RandomInt();

		repository.SetUpQuery<int>();

		await repository.QueryByFilter(i => i.Number == item.Number, i => i.Number, true, skip, limit);

		repository.QuerySkip.Should().Be(skip);

		repository.QueryLimit.Should().Be(limit);

		repository.QuerySortDescending.Should().BeTrue();
	}
}
