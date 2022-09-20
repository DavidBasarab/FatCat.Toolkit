﻿using FakeItEasy;
using FatCat.Fakes;
using FatCat.Toolkit.Testing;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Tests.FatCat.Toolkit.Data.DataRepositorySpecs;

public class GetByIdWithObjectIdTests : DataRepositoryTests
{
	private readonly EasyCapture<ExpressionFilterDefinition<TestingMongoObject>> expressionCapture;
	private readonly TestingMongoObject filterItem;
	private readonly ObjectId id;

	public GetByIdWithObjectIdTests()
	{
		id = ObjectId.GenerateNewId();

		filterItem = Faker.Create<TestingMongoObject>(afterCreate: i => i.Id = id);

		expressionCapture = new EasyCapture<ExpressionFilterDefinition<TestingMongoObject>>();

		A.CallTo(() => collection.FindAsync<TestingMongoObject>(expressionCapture!, default, default))
		.Returns(new TestingAsyncCursor<TestingMongoObject>(new List<TestingMongoObject> { filterItem }));
	}

	[Fact]
	public async Task CallFindAsyncWithFilter()
	{
		await repository.GetById(id);

		A.CallTo(() => collection.FindAsync<TestingMongoObject>(A<ExpressionFilterDefinition<TestingMongoObject>>._!, default, default))
		.MustHaveHappened();

		expressionCapture.Value
						.Should()
						.NotBeNull();

		var filter = expressionCapture.Value.Expression.Compile();

		foreach (var currentItem in itemList)
		{
			filter(currentItem!)
				.Should()
				.BeFalse();
		}

		filter(filterItem!)
			.Should()
			.BeTrue();
	}

	[Fact]
	public void ReturnFilterItem()
	{
		repository.GetById(id)
				.Should()
				.Be(filterItem);
	}
}