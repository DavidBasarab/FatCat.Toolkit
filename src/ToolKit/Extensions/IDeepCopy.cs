namespace FatCat.Toolkit.Extensions;

public interface IDeepCopy
{
	T Copy<T>(T objectToCopy)
		where T : class;
}
