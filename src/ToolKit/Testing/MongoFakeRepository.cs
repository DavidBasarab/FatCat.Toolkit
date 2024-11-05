#nullable enable
using System.Linq.Expressions;
using FakeItEasy;
using FatCat.Fakes;
using FatCat.Toolkit.Data.Mongo;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FatCat.Toolkit.Testing;

public class MongoFakeRepository<T> : IMongoRepository<T>
	where T : MongoObject
{
	private readonly IMongoRepository<T> repository;

	public IMongoCollection<T> Collection { get; } = null!;

	public EasyCapture<T> CreatedCapture { get; } = new();

	public T CreatedItem { get; set; } = null!;

	public List<T> CreatedList { get; set; } = null!;

	public string DatabaseName
	{
		get => repository.DatabaseName;
	}

	public T DeletedItem { get; set; } = null!;

	public List<T> DeletedList { get; set; } = null!;

	public EasyCapture<Expression<Func<T, bool>>> FilterCapture { get; private set; } = null!;

	public T Item { get; set; } = null!;

	public string ItemId { get; set; } = null!;

	public List<T> Items { get; set; } = null!;

	public EasyCapture<T> UpdatedCapture { get; } = new();

	public T UpdatedItem { get; set; } = null!;

	public List<T> UpdatedList { get; set; } = null!;

	public MongoFakeRepository()
	{
		repository = A.Fake<IMongoRepository<T>>();

		SetUpGet();
		SetUpCreate();
		SetUpUpdate();
		SetUpDelete();
		SetUpGetByFilter();
	}

	public void Connect(string? connectionString = null, string? databaseName = null)
	{
		repository.Connect(connectionString, databaseName);
	}

	public async Task<T> Create(T item)
	{
		return await repository.Create(item);
	}

	public async Task<List<T>> Create(List<T> items)
	{
		return await repository.Create(items);
	}

	public async Task<T> Delete(T item)
	{
		return await repository.Delete(item);
	}

	public async Task<List<T>> Delete(List<T> items)
	{
		return await repository.Delete(items);
	}

	public async Task<List<T>> GetAll()
	{
		return await repository.GetAll();
	}

	public async Task<List<T>> GetAllByFilter(Expression<Func<T, bool>> filter)
	{
		return await repository.GetAllByFilter(filter);
	}

	public async Task<T?> GetByFilter(Expression<Func<T, bool>> filter)
	{
		return await repository.GetByFilter(filter);
	}

	public async Task<T?> GetById(string id)
	{
		return await repository.GetById(id);
	}

	public async Task<T?> GetById(ObjectId id)
	{
		return await repository.GetById(id);
	}

	public async Task<T?> GetFirst()
	{
		return await repository.GetFirst();
	}

	public async Task<T> GetFirstOrCreate()
	{
		return await repository.GetFirstOrCreate();
	}

	public void SetUpItemNotInRepository()
	{
		A.CallTo(() => repository.GetById(A<string>._)).Returns(null as T);
	}

	public async Task<T> Update(T item)
	{
		return await repository.Update(item);
	}

	public async Task<List<T>> Update(List<T> items)
	{
		return await repository.Update(items);
	}

	public void VerifyCreate(Action<T> matcher)
	{
		A.CallTo(() => repository.Create(A<T>.That.Matches(matcher))).MustHaveHappened();
	}

	public void VerifyCreate(T expectedItem)
	{
		A.CallTo(() => repository.Create(A<T>._)).MustHaveHappened();

		CreatedCapture.Value.Should().Be(expectedItem);
	}

	public void VerifyCreate()
	{
		A.CallTo(() => repository.Create(A<T>._)).MustHaveHappened();
	}

	public void VerifyDelete(Action<T> matcher)
	{
		A.CallTo(() => repository.Delete(A<T>.That.Matches(matcher))).MustHaveHappened();
	}

	public void VerifyDelete()
	{
		A.CallTo(() => repository.Delete(A<T>._)).MustHaveHappened();
	}

	public void VerifyDelete(T expectedItem)
	{
		A.CallTo(() => repository.Delete(expectedItem)).MustHaveHappened();
	}

	public void VerifyDidNotCreate()
	{
		A.CallTo(() => repository.Create(A<T>._)).MustNotHaveHappened();
	}

	public void VerifyDidNotGetAll()
	{
		A.CallTo(() => repository.GetAll()).MustNotHaveHappened();
	}

	public void VerifyDidNotGetByFilter()
	{
		FilterCapture.Value.Should().BeNull();
	}

	public void VerifyDidNotGetById()
	{
		A.CallTo(() => repository.GetById(A<string>._)).MustNotHaveHappened();
	}

	public void VerifyDidNotUpdate()
	{
		A.CallTo(() => repository.Update(A<T>._)).MustNotHaveHappened();
	}

	public void VerifyGetAll()
	{
		A.CallTo(() => repository.GetAll()).MustHaveHappened();
	}

	public void VerifyGetByFilterByItemFalse(T item)
	{
		FilterCapture.Value.Should().NotBeNull();

		var compliedExpression = FilterCapture.Value.Compile();

		compliedExpression(item).Should().BeFalse();
	}

	public void VerifyGetByFilterByItemTrue(T item)
	{
		FilterCapture.Value.Should().NotBeNull();

		var compliedExpression = FilterCapture.Value.Compile();

		compliedExpression(item).Should().BeTrue();
	}

	public void VerifyGetById()
	{
		A.CallTo(() => repository.GetById(ItemId)).MustHaveHappened();
	}

	public void VerifyGetFirst()
	{
		A.CallTo(() => repository.GetFirst()).MustHaveHappened();
	}

	public void VerifyNotGetFirst()
	{
		A.CallTo(() => repository.GetFirst()).MustNotHaveHappened();
	}

	public void VerifyUpdate(Action<T> matcher)
	{
		A.CallTo(() => repository.Update(A<T>.That.Matches(matcher))).MustHaveHappened();
	}

	public void VerifyUpdate(T expectedData)
	{
		A.CallTo(() => repository.Update(expectedData)).MustHaveHappened();
	}

	private void SetUpCreate()
	{
		CreatedItem = Faker.Create<T>();
		CreatedList = Faker.Create<List<T>>();

		A.CallTo(() => repository.Create(CreatedCapture)).ReturnsLazily(() => CreatedItem);

		A.CallTo(() => repository.Create(A<List<T>>._)).ReturnsLazily(() => CreatedList);
	}

	private void SetUpDelete()
	{
		DeletedItem = Faker.Create<T>();
		DeletedList = Faker.Create<List<T>>();

		A.CallTo(() => repository.Delete(A<T>._)).ReturnsLazily(() => DeletedItem);

		A.CallTo(() => repository.Delete(A<List<T>>._)).ReturnsLazily(() => DeletedList);
	}

	private void SetUpGet()
	{
		ItemId = Faker.RandomString();
		Items = Faker.Create<List<T>>();
		Item = Faker.Create<T>();

		A.CallTo(() => repository.GetById(A<string>._)).ReturnsLazily(() => Item);

		A.CallTo(() => repository.GetFirst()).ReturnsLazily(() => Item);

		A.CallTo(() => repository.GetAll()).ReturnsLazily(() => Items);
	}

	private void SetUpGetByFilter()
	{
		FilterCapture = new EasyCapture<Expression<Func<T, bool>>>();

		A.CallTo(() => repository.GetByFilter(FilterCapture)).ReturnsLazily(() => Item);

		A.CallTo(() => repository.GetAllByFilter(FilterCapture)).ReturnsLazily(() => Items);
	}

	private void SetUpUpdate()
	{
		UpdatedItem = Faker.Create<T>();
		UpdatedList = Faker.Create<List<T>>();

		A.CallTo(() => repository.Update(UpdatedCapture)).ReturnsLazily(() => UpdatedItem);

		A.CallTo(() => repository.Update(A<List<T>>._)).ReturnsLazily(() => UpdatedList);
	}
}
