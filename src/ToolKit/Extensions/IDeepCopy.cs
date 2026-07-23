namespace FatCat.Toolkit.Extensions;

public interface IDeepCopy
{
	public T Copy<T>(T objectToCopy)
		where T : class;
}
