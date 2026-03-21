#nullable enable
using System.Linq.Expressions;

namespace FatCat.Toolkit.Data.FileSystem;

public interface IFileSystemRepository<T>
	where T : FileSystemDataObject
{
	public Task<T> Create(T item);

	public Task<List<T>> Create(List<T> items);

	public Task<T> Delete(T item);

	public Task<List<T>> Delete(List<T> items);

	public Task<List<T>> GetAll();

	public Task<List<T>> GetAllByFilter(Expression<Func<T, bool>> filter);

	public Task<T?> GetByFilter(Expression<Func<T, bool>> filter);

	public Task<T?> GetById(string id);

	public Task<T?> GetFirst();

	public Task<T> GetFirstOrCreate();

	public Task<T> Update(T item);

	public Task<List<T>> Update(List<T> items);
}

/// <summary>
///  Keep collection in memory and write changes to file system.
///  How do you handle a delete?
/// </summary>
/// <typeparam name="T"></typeparam>
public class FileSystemRepository<T> : IFileSystemRepository<T>
	where T : FileSystemDataObject
{
	public Task<T> Create(T item)
	{
		throw new NotImplementedException();
	}

	public Task<List<T>> Create(List<T> items)
	{
		throw new NotImplementedException();
	}

	public Task<T> Delete(T item)
	{
		throw new NotImplementedException();
	}

	public Task<List<T>> Delete(List<T> items)
	{
		throw new NotImplementedException();
	}

	public Task<List<T>> GetAll()
	{
		throw new NotImplementedException();
	}

	public Task<List<T>> GetAllByFilter(Expression<Func<T, bool>> filter)
	{
		throw new NotImplementedException();
	}

	public Task<T?> GetByFilter(Expression<Func<T, bool>> filter)
	{
		throw new NotImplementedException();
	}

	public Task<T?> GetById(string id)
	{
		throw new NotImplementedException();
	}

	public Task<T?> GetFirst()
	{
		throw new NotImplementedException();
	}

	public Task<T> GetFirstOrCreate()
	{
		throw new NotImplementedException();
	}

	public Task<T> Update(T item)
	{
		throw new NotImplementedException();
	}

	public Task<List<T>> Update(List<T> items)
	{
		throw new NotImplementedException();
	}
}
