using FatCat.Toolkit.Testing;

namespace Tests.FatCat.Toolkit.Data.Mongo.DataRepositorySpecs;

public class CreateItemTests : EnsureCollectionTests
{
	[Fact]
	public async Task InsertOneOnTheCollection()
	{
		await TestMethod();

		A.CallTo(() => collection.InsertOneAsync(item, default, default)).MustHaveHappened();
	}

	[Fact]
	public async Task PlayingWithCapture()
	{
		var easyCapture = new EasyCapture<TestingMongoObject>();

		A.CallTo(() => collection.InsertOneAsync(easyCapture, default, default)).Returns(Task.CompletedTask);

		await TestMethod();

		easyCapture.Value.Should().BeEquivalentTo(item);
	}

	[Fact]
	public void ReturnItem()
	{
		repository.Create(item).Should().Be(item);
	}

	protected override Task TestMethod()
	{
		return repository.Create(item);
	}
}
