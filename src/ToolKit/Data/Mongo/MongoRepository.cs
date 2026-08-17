#nullable enable
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FatCat.Toolkit.Data.Mongo;

public interface IMongoRepository<T> : IDataRepository<T>
	where T : MongoObject
{
	public IMongoCollection<T> Collection { get; }

	public string DatabaseName { get; }

	public void Connect(string? connectionString = null, string? databaseName = null);

	/// <summary>
	/// The number of documents matching <paramref name="filter" />, counted <b>on the server</b>: no
	/// document is transferred or deserialised. Returns <c>0</c> when nothing matches.
	/// </summary>
	public Task<long> CountByFilter(Expression<Func<T, bool>> filter);

	/// <summary>
	/// The distinct values of <paramref name="field" /> across the documents matching
	/// <paramref name="filter" />, computed <b>on the server</b>. Returns an empty list when nothing
	/// matches.
	/// <para>
	/// <b>A matching document that carries no value for the field contributes <c>null</c> to the result,
	/// and that entry is deliberately not filtered out.</b> "Some documents have no value here" is
	/// frequently the answer a caller is asking for — dropping it would make that state invisible.
	/// </para>
	/// </summary>
	public Task<List<TValue>> DistinctByFilter<TValue>(Expression<Func<T, TValue>> field, Expression<Func<T, bool>> filter);

	/// <summary>
	/// A single page of the documents matching <paramref name="filter" />, sorted on
	/// <paramref name="sortBy" /> (<paramref name="sortDescending" /> chooses the direction), sliced by
	/// <paramref name="skip" /> and <paramref name="limit" /> — all <b>on the server</b>. The returned
	/// <see cref="PagedResults{T}.TotalCount" /> is <b>every</b> document matching the filter, independent of
	/// <paramref name="skip" /> and <paramref name="limit" />. Returns an empty page with a <c>0</c> count
	/// when nothing matches.
	/// </summary>
	public Task<PagedResults<T>> QueryByFilter<TSort>(
		Expression<Func<T, bool>> filter,
		Expression<Func<T, TSort>> sortBy,
		bool sortDescending,
		int skip,
		int limit
	);

	/// <summary>
	/// Deletes every document matching <paramref name="filter" /> in one server-side command and returns how
	/// many were removed. A filter matching nothing is a legal no-op that returns <c>0</c> and does not throw.
	/// </summary>
	public Task<long> DeleteByFilter(Expression<Func<T, bool>> filter);

	public Task<T?> GetById(string id);

	public Task<T?> GetById(ObjectId id);
}

public class MongoRepository<T>(IMongoDataConnection mongoDataConnection, IMongoNames mongoNames) : IMongoRepository<T>
	where T : MongoObject, new()
{
	public IMongoCollection<T> Collection { get; set; }

	public string DatabaseName { get; set; } = null!;

	public void Connect(string? connectionString = null, string? databaseName = null)
	{
		Collection = mongoDataConnection.GetCollection<T>(connectionString, databaseName);
		DatabaseName = databaseName ?? mongoNames.GetDatabaseName<T>();
	}

	public async Task<long> CountByFilter(Expression<Func<T, bool>> filter)
	{
		EnsureCollection();

		return await Collection.CountDocumentsAsync(filter);
	}

	public async Task<T> Create(T item)
	{
		EnsureCollection();

		await Collection.InsertOneAsync(item);

		return item;
	}

	public async Task<List<T>> Create(List<T> items)
	{
		if (items.Count == 0)
		{
			return items;
		}

		EnsureCollection();

		await Collection.InsertManyAsync(items);

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
		if (items.Count == 0)
		{
			return items;
		}

		EnsureCollection();

		await Collection.DeleteManyAsync(Builders<T>.Filter.In(item => item.Id, items.Select(item => item.Id)));

		return items;
	}

	public async Task<long> DeleteByFilter(Expression<Func<T, bool>> filter)
	{
		EnsureCollection();

		var result = await Collection.DeleteManyAsync(filter);

		return result.DeletedCount;
	}

	public async Task<List<TValue>> DistinctByFilter<TValue>(
		Expression<Func<T, TValue>> field,
		Expression<Func<T, bool>> filter
	)
	{
		EnsureCollection();

		var cursor = await Collection.DistinctAsync(new ExpressionFieldDefinition<T, TValue>(field), filter);

		return await cursor.ToListAsync();
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

	public async Task<PagedResults<T>> QueryByFilter<TSort>(
		Expression<Func<T, bool>> filter,
		Expression<Func<T, TSort>> sortBy,
		bool sortDescending,
		int skip,
		int limit
	)
	{
		EnsureCollection();

		var totalCount = await Collection.CountDocumentsAsync(filter);

		var sortField = Expression.Lambda<Func<T, object>>(Expression.Convert(sortBy.Body, typeof(object)), sortBy.Parameters);

		var sort = sortDescending ? Builders<T>.Sort.Descending(sortField) : Builders<T>.Sort.Ascending(sortField);

		var items = await Collection.Find(filter).Sort(sort).Skip(skip).Limit(limit).ToListAsync();

		return new PagedResults<T> { Items = items, TotalCount = totalCount };
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
		if (items.Count == 0)
		{
			return items;
		}

		EnsureCollection();

		await Collection.BulkWriteAsync(items.Select(ReplaceModelFor));

		return items;
	}

	private ReplaceOneModel<T> ReplaceModelFor(T item)
	{
		return new ReplaceOneModel<T>(Builders<T>.Filter.Eq(current => current.Id, item.Id), item);
	}

	private void EnsureCollection()
	{
		if (Collection == null)
		{
			throw new ConnectionToMongoIsRequired();
		}
	}
}
