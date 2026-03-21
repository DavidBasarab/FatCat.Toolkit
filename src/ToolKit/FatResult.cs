namespace FatCat.Toolkit;

public class FatResult<T> : EqualObject
{
	public static FatResult<T> Failed(string message)
	{
		return new FatResult<T> { IsSuccessful = false, Message = message };
	}

	public static FatResult<T> Success(T data)
	{
		return new FatResult<T> { IsSuccessful = true, Data = data };
	}

	public T Data { get; set; }

	public bool IsSuccessful { get; set; }

	public bool IsUnsuccessful
	{
		get => !IsSuccessful;
	}

	public string Message { get; set; }
}
