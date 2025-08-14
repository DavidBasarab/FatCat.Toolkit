using FatCat.Toolkit;
using FatCat.Toolkit.Json;
using FatCat.Toolkit.Web;

namespace Tests.FatCat.Toolkit.Web.Api.WebCallerSpecs;

public class PostTests : WebCallerTests
{
	protected override string BasicPath
	{
		get => "/post";
	}

	[Fact]
	public async Task CanMakeAPostWithCustomContentType()
	{
		var plainText = "this is plain text";

		var result = await webCaller.Post(BasicPath, plainText, "text/plain");

		result.IsSuccessful.Should().BeTrue();

		response = result.To<HttpBinResponse>();

		response.RawData.Should().Be(plainText);

		response.ContentTypeHeader.Should().Be("text/plain; charset=utf-8");
	}

	[Fact]
	public async Task PostWithAListOfData()
	{
		var dataList = Faker.Create<List<TestData>>();

		var result = await webCaller.Post(BasicPath, dataList);

		result.IsSuccessful.Should().BeTrue();

		var jsonResponse = result.To<HttpBinDataJsonResponse<List<TestData>>>();

		jsonResponse.Json.Should().BeEquivalentTo(dataList);
	}

	[Fact]
	public async Task PostWithDataObject()
	{
		var data = Faker.Create<TestData>();

		var result = await webCaller.Post(BasicPath, data);

		result.IsSuccessful.Should().BeTrue();

		var jsonResponse = result.To<HttpBinDataJsonResponse<TestData>>();

		jsonResponse.Json.Should().BeEquivalentTo(data);
	}

	[Fact]
	public async Task PostWithJsonData()
	{
		var data = Faker.Create<TestData>();

		var json = new JsonOperations().Serialize(data);

		var result = await webCaller.Post(BasicPath, json);

		result.IsSuccessful.Should().BeTrue();

		var jsonResponse = result.To<HttpBinDataJsonResponse<TestData>>();

		jsonResponse.Json.Should().BeEquivalentTo(data);
	}

	[Fact]
	public async Task PostWithStringData()
	{
		var data = "this is just some data";

		var result = await webCaller.Post(BasicPath, data, "text/plain");

		result.IsSuccessful.Should().BeTrue();

		response = result.To<HttpBinResponse>();

		response.RawData.Should().Be($"{data}");
	}

	protected override Task<FatWebResponse> MakeCallToWeb(string path)
	{
		return webCaller.Post(path);
	}

	public class TestData : EqualObject
	{
		public string FirstName { get; set; }

		public string LastName { get; set; }

		public int Number { get; set; }

		public string ToJson()
		{
			return new JsonOperations().Serialize(this);
		}
	}
}
