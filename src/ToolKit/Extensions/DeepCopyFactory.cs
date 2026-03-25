namespace FatCat.Toolkit.Extensions;

public static class DeepCopyFactory
{
	private static IDeepCopy deepCopy = new ReflectionDeepCopy();

	public static IDeepCopy Get()
	{
		return deepCopy;
	}

	public static void Set(IDeepCopy customDeepCopy)
	{
		deepCopy = customDeepCopy;
	}
}
