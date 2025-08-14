using FatCat.Toolkit;
using FatCat.Toolkit.Json;
using FatCat.Toolkit.WebServer;

namespace Tests.FatCat.Toolkit;

public class WebResultAsTests
{
	[Fact]
	public void CanConvertAMoreComplicatedItem()
	{
		var item = Faker.Create<TestingModel>();

		var result = WebResult.Ok(item);

		result.To<TestingModel>().Should().BeEquivalentTo(item);
	}

	[Fact]
	public void CanConvertList()
	{
		var itemList = Faker.Create<List<TestItem>>();

		var result = WebResult.Ok(itemList);

		result.To<List<TestItem>>().Should().BeEquivalentTo(itemList);
	}

	[Fact]
	public void CanConvertSingleObject()
	{
		var testItem = Faker.Create<TestItem>();

		var result = WebResult.Ok(testItem);

		result.To<TestItem>().Should().BeEquivalentTo(testItem);
	}

	[Fact]
	public void CanGetProperItemBackFromEndpoint()
	{
		var item = Faker.Create<TestingModel>();

		var endpoint = new TestingEndpoint(item);

		var result = endpoint.DoSomeWork();

		result.To<TestingModel>().Should().BeEquivalentTo(item);
	}

	[Fact]
	public void JsonConvertedWithStringCanWork()
	{
		var item = Faker.Create<TestingModel>();

		var json = new JsonOperations().Serialize(item);

		var result = WebResult.Ok(json);

		result.To<TestingModel>().Should().BeEquivalentTo(item);
	}

	private class TestItem : EqualObject
	{
		public DateTime DateTime { get; set; }

		public string FirstName { get; set; }

		public int Number { get; set; }
	}
}
