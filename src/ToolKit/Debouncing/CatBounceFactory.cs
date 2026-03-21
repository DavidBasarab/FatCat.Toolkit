namespace FatCat.Toolkit.Debouncing;

public interface ICatBounceFactory
{
	public ICatBounce Create(TimeSpan interval);
}

public class CatBounceFactory : ICatBounceFactory
{
	public ICatBounce Create(TimeSpan interval)
	{
		return new CatBounce(interval);
	}
}
