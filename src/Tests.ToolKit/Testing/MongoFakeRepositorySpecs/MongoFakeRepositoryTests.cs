using FatCat.Toolkit.Testing;
using Tests.FatCat.Toolkit.Data.Mongo;

namespace Tests.FatCat.Toolkit.Testing.MongoFakeRepositorySpecs;

public abstract class MongoFakeRepositoryTests
{
	protected readonly TestingMongoObject item;
	protected readonly MongoFakeRepository<TestingMongoObject> repository = new();

	protected MongoFakeRepositoryTests()
	{
		item = Faker.Create<TestingMongoObject>();
	}
}
