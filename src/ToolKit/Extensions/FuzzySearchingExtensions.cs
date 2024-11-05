namespace FatCat.Toolkit.Extensions;

public static class FuzzySearchingExtensions
{
	public static List<T> FuzzySearch<T>(this List<T> list, string search, Func<T, string> searchProperty)
	{
		var foundItems = new List<T>();

		foreach (var item in list)
		{
			var propertyValue = searchProperty(item);

			if (propertyValue.Contains(search, StringComparison.OrdinalIgnoreCase))
			{
				foundItems.Add(item);
			}
		}

		return foundItems;
	}
}
