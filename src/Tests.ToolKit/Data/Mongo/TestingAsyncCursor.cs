using MongoDB.Driver;

namespace Tests.FatCat.Toolkit.Data.Mongo;

public class TestingAsyncCursor<T> : IAsyncCursor<T>
{
	private readonly List<T> items;

	private bool movedCalled;

	public IEnumerable<T> Current
	{
		get => items;
	}

	public TestingAsyncCursor(List<T> items)
	{
		this.items = items;
	}

	public void Dispose() { }

	public bool MoveNext(CancellationToken cancellationToken = new())
	{
		// Only need to call move once, after that return false
		if (movedCalled)
		{
			return false;
		}

		movedCalled = true;

		return true;
	}

	public Task<bool> MoveNextAsync(CancellationToken cancellationToken = new())
	{
		var result = MoveNext(cancellationToken);

		return Task.FromResult(result);
	}
}
