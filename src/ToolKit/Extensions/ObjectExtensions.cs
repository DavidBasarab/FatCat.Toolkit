using Force.DeepCloner;

namespace FatCat.Toolkit.Extensions;

public static class ObjectExtensions
{
	public static T DeepCopy<T>(this T objectToCopy)
		where T : class
	{
		return objectToCopy?.DeepClone();
	}
}
