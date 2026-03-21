using FakeItEasy;

namespace FatCat.Toolkit.Testing;

public class EasyCapture<T>
{
	public static implicit operator T(EasyCapture<T> capture)
	{
		A<T>.That.Matches(
			i =>
			{
				capture.CaptureValue(i);

				return true;
			},
			$"Captured parameter {typeof(T).FullName}"
		);

		return default!;
	}

	private readonly List<T> values = new();

	public bool HasValues
	{
		get
		{
			return values.Any();
		}
	}

	public T Value
	{
		get
		{
			return values.First();
		}
	}

	public IReadOnlyList<T> Values
	{
		get
		{
			return values.AsReadOnly();
		}
	}

	private void CaptureValue(T value)
	{
		values.Add(value);
	}
}
