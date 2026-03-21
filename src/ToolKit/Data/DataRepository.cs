#nullable enable
using System.Linq.Expressions;

namespace FatCat.Toolkit.Data;

public interface IDataRepository<T>
	where T : DataObject
{
	public Task<T> Create(T item);

	public Task<List<T>> Create(List<T> items);

	public Task<T> Delete(T item);

	public Task<List<T>> Delete(List<T> items);

	public Task<List<T>> GetAll();

	public Task<List<T>> GetAllByFilter(Expression<Func<T, bool>> filter);

	public Task<T?> GetByFilter(Expression<Func<T, bool>> filter);

	public Task<T?> GetFirst();

	public Task<T> GetFirstOrCreate();

	public Task<T> Update(T item);

	public Task<List<T>> Update(List<T> items);
}
