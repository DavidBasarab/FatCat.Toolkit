using FatCat.Toolkit.Data.Mongo;

namespace OneOff.MongoProof;

public class ProofItem : MongoObject
{
	public string Name { get; set; }

	public int Value { get; set; }
}
