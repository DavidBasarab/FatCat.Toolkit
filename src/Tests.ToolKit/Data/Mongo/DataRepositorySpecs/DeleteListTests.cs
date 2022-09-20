﻿using FakeItEasy;
using FatCat.Toolkit.Testing;
using MongoDB.Driver;
using Xunit;

namespace Tests.FatCat.Toolkit.Data.Mongo.DataRepositorySpecs;

public class DeleteListTests : DataRepositoryTests
{
	[Fact]
	public async Task CallDeleteOneForAllItems()
	{
		await repository.Delete(itemList);

		A.CallTo(() => collection.DeleteOneAsync(A<ExpressionFilterDefinition<TestingMongoObject>>._, default))
		.MustHaveHappened(itemList.Count, Times.Exactly);
	}

	[Fact]
	public void ReturnDeleteItemList()
	{
		repository.Delete(itemList)
				.Should()
				.BeEquivalentTo(itemList);
	}
}