using FatCat.Fakes;
using MongoDB.Bson;

namespace FatCat.Toolkit;

public interface IGenerator
{
	IEnumerable<byte> Bytes(int length);

	bool IsValidObjectId(string idToTest);

	Guid NewGuid();

	string NewId();

	ObjectId NewObjectId();

	int NextRandom(int? minNumber = null, int? maxNumber = null);

	int RandomNumber(int? minNumber = null, int? maxNumber = null);

	string RandomString(string prefix = null, int? length = null);
}

public class Generator : IGenerator
{
	public IEnumerable<byte> Bytes(int length)
	{
		return Faker.RandomBytes(length);
	}

	public bool IsValidObjectId(string idToTest)
	{
		return ObjectId.TryParse(idToTest, out _);
	}

	public Guid NewGuid()
	{
		return Guid.NewGuid();
	}

	public string NewId()
	{
		return NewObjectId().ToString();
	}

	public ObjectId NewObjectId()
	{
		return ObjectId.GenerateNewId();
	}

	public int NextRandom(int? minNumber = null, int? maxNumber = null)
	{
		return Faker.RandomInt(minNumber, maxNumber);
	}

	public int RandomNumber(int? minNumber = null, int? maxNumber = null)
	{
		return Faker.RandomInt(minNumber, maxNumber);
	}

	public string RandomString(string prefix = null, int? length = null)
	{
		return Faker.RandomString(prefix, length);
	}
}
