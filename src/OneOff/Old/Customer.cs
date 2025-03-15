using FatCat.Toolkit.Data.Mongo;

namespace OneOff.Old;

public class Customer : MongoObject
{
	public bool IsActive { get; set; }

	public string Name { get; set; }

	public List<string> Phones { get; set; }
}
