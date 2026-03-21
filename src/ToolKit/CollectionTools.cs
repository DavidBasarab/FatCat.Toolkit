using FatCat.Toolkit.Extensions;

namespace FatCat.Toolkit;

public interface ICollectionTools
{
	public bool AreEqual<T>(IEnumerable<T> first, IEnumerable<T> second);
}

public class CollectionTools : ICollectionTools
{
	public bool AreEqual<T>(IEnumerable<T> first, IEnumerable<T> second)
	{
		return first.ListsAreEqual(second);
	}
}
