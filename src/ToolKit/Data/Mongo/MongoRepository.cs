#nullable enable
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FatCat.Toolkit.Data.Mongo;

public interface IMongoRepository<T> : IDataRepository<T>
	where T : MongoObject
{
	IMongoCollection<T> Collection { get; }

	string DatabaseName { get; }

	void Connect(string? connectionString = null, string? databaseName = null);

	Task<T?> GetById(string id);

	Task<T?> GetById(ObjectId id);
}

public class MongoRepository<T>(IMongoDataConnection mongoDataConnection, IMongoNames mongoNames)
	: IMongoRepository<T>
	where T : MongoObject, new()
{
	public IMongoCollection<T> Collection { get; set; }

	public string DatabaseName { get; set; } = null!;

	public void Connect(string? connectionString = null, string? databaseName = null)
	{
		Collection = mongoDataConnection.GetCollection<T>(connectionString, databaseName);
		DatabaseName = databaseName ?? mongoNames.GetDatabaseName<T>();
	}

	public async Task<T> Create(T item)
	{
		EnsureCollection();

		await Collection.InsertOneAsync(item);

		return item;
	}

	public async Task<List<T>> Create(List<T> items)
	{
		foreach (var item in items)
		{
			await Create(item);
		}

		return items;
	}

	public async Task<T> Delete(T item)
	{
		EnsureCollection();

		await Collection.DeleteOneAsync(i => i.Id == item.Id);

		return item;
	}

	public async Task<List<T>> Delete(List<T> items)
	{
		foreach (var item in items)
		{
			await Delete(item);
		}

		return items;
	}

	public async Task<List<T>> GetAll()
	{
		EnsureCollection();

		var cursor = await Collection.FindAsync(i => true);

		return await cursor.ToListAsync();
	}

	public async Task<List<T>> GetAllByFilter(Expression<Func<T, bool>> filter)
	{
		EnsureCollection();

		var cursor = await Collection.FindAsync(filter);

		return await cursor.ToListAsync();
	}

	public async Task<T?> GetByFilter(Expression<Func<T, bool>> filter)
	{
		var list = await GetAllByFilter(filter);

		return list.FirstOrDefault();
	}

	public async Task<T?> GetById(string id)
	{
		return await GetByFilter(i => i.Id == new ObjectId(id));
	}

	public async Task<T?> GetById(ObjectId id)
	{
		return await GetByFilter(i => i.Id == id);
	}

	public async Task<T?> GetFirst()
	{
		return await GetByFilter(i => true);
	}

	public async Task<T> GetFirstOrCreate()
	{
		var firstItem = await GetFirst();

		if (firstItem == null)
		{
			firstItem = new T();

			await Create(firstItem);
		}

		return firstItem;
	}

	public async Task<T> Update(T item)
	{
		EnsureCollection();

		await Collection.ReplaceOneAsync(i => i.Id == item.Id, item);

		return item;
	}

	public async Task<List<T>> Update(List<T> items)
	{
		foreach (var item in items)
		{
			await Update(item);
		}

		return items;
	}

	private void EnsureCollection()
	{
		if (Collection == null)
		{
			throw new ConnectionToMongoIsRequired();
		}
	}
}
