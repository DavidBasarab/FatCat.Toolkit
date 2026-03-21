using FatCat.Toolkit;

namespace ProxySpike.Workers.ServiceModels;

public class SamplePostRequest : EqualObject
{
	public string FirstName { get; set; }

	public string LastName { get; set; }

	public int SomeNumber { get; set; }
}
