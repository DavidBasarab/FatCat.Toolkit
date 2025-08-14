using FatCat.Fakes;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.Data.Mongo;
using MongoDB.Driver;

namespace OneOff.Old;

[Flags]
public enum SearchFlags
{
	None = 0,
	FirstName = 1,
	LastName = 2,
	AnotherFlag = 4,
	WalkWithLove = 8,
	RubixCube = 16,
}

public class TestSearchingObject : MongoObject
{
	private SearchFlags searchFlags;

	public string FirstName { get; set; }

	public long FlagsValue
	{
		get => (long)SearchFlags;
	}

	public string LastName { get; set; }

	public int Number { get; set; }

	public long SearchFlagNumber { get; set; }

	public SearchFlags SearchFlags
	{
		get => searchFlags;
		set
		{
			searchFlags = value;

			SearchFlagNumber = (long)searchFlags;
		}
	}
}

public class MongoSearchingWorker : SpikeWorker
{
	private readonly IMongoRepository<TestSearchingObject> mongo;

	public MongoSearchingWorker(IMongoRepository<TestSearchingObject> mongo)
	{
		this.mongo = mongo;
	}

	public override async Task DoWork()
	{
		await Task.CompletedTask;
		await Task.CompletedTask;
		await Task.CompletedTask;

		ConsoleLog.WriteBlue("Going to do some work on searching the Mongo Databases");

		var firstObject = new TestSearchingObject
		{
			Number = 1,
			FirstName = Faker.RandomString(),
			LastName = Faker.RandomString(),
			SearchFlags = SearchFlags.FirstName | SearchFlags.LastName,
		};

		await mongo.Create(firstObject);

		var secondObject = new TestSearchingObject
		{
			Number = 2,
			FirstName = Faker.RandomString(),
			LastName = Faker.RandomString(),
			SearchFlags = SearchFlags.WalkWithLove,
		};

		await mongo.Create(secondObject);

		ConsoleLog.WriteMagenta("Done creating objects");

		var cursor = await mongo.Collection.FindAsync(i => true);

		var allItems = await cursor.ToListAsync();

		ConsoleLog.WriteCyan($"Found {allItems.Count} objects");

		var flagsToFind = SearchFlags.FirstName | SearchFlags.LastName | SearchFlags.WalkWithLove;

		var filter = Builders<TestSearchingObject>.Filter.BitsAnySet(i => i.SearchFlagNumber, (long)flagsToFind);

		var firstObjects = mongo.Collection.Find(filter).ToList();

		ConsoleLog.WriteMagenta(
			$"Found {firstObjects.Count} objects with the flags {flagsToFind} | <{(long)flagsToFind}>"
		);
	}
}
