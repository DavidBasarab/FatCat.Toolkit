#nullable enable
using FatCat.Toolkit.Extensions;

namespace FatCat.Toolkit;

public interface IObjectTools
{
	public T? DeepClone<T>(T? obj)
		where T : class;

	public bool IsEquals(EqualObject? obj1, EqualObject? obj2);
}

public class ObjectTools : IObjectTools
{
	public T? DeepClone<T>(T? obj)
		where T : class
	{
		return obj?.DeepCopy();
	}

	public bool IsEquals(EqualObject? obj1, EqualObject? obj2)
	{
		return ObjectEquals.AreEqual(obj1, obj2);
	}
}
