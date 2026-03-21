using FatCat.Fakes;

namespace SampleDocker;

public interface IExampleWorker
{
	public string GetMessage();
}

public class ExampleWorker : IExampleWorker
{
	public string GetMessage()
	{
		return $"This is the message | {Faker.RandomInt()}";
	}
}
