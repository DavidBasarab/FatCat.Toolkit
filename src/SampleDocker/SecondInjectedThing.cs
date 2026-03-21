using FatCat.Fakes;

namespace SampleDocker;

public interface ISecondInjectedThing
{
	public int GetSomeNumber();
}

public class SecondInjectedThing : ISecondInjectedThing
{
	public int GetSomeNumber()
	{
		return Faker.RandomInt();
	}
}
