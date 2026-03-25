namespace FatCat.Toolkit.Extensions;

public static class ObjectExtensions
{
	public static T DeepCopy<T>(this T objectToCopy)
		where T : class
	{
		if (objectToCopy == null)
		{
			return null;
		}

		return DeepCopyFactory.Get().Copy(objectToCopy);
	}
}
