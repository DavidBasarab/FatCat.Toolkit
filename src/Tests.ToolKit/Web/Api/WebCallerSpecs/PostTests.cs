using FatCat.Toolkit;
using FatCat.Toolkit.Json;
using FatCat.Toolkit.Web;
using Humanizer;

namespace Tests.FatCat.Toolkit.Web.Api.WebCallerSpecs;

public class PostTests : WebCallerTests
{
	protected override string BasicPath
	{
		get
		{
			return "/post";
		}
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
	public async Task CarryTheCallerTokenWhenPostingAList()
	{
		await cancellationSource.CancelAsync();

		var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			webCaller.Post(BasicPath, Faker.Create<List<TestData>>(), 1.Minutes(), cancellationSource.Token)
		);

		VerifyCarriesTheCallerToken(exception);
	}

	[Fact]
	public async Task CarryTheCallerTokenWhenPostingStringDataWithAContentType()
	{
		await cancellationSource.CancelAsync();

		var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			webCaller.Post(BasicPath, "this is plain text", 1.Minutes(), "text/plain", cancellationSource.Token)
		);

		VerifyCarriesTheCallerToken(exception);
	}

	[Fact]
	public async Task CarryTheCallerTokenWhenPostingWithoutData()
	{
		await cancellationSource.CancelAsync();

		var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			webCaller.Post(BasicPath, 1.Minutes(), cancellationSource.Token)
		);

		VerifyCarriesTheCallerToken(exception);
	}

	[Fact]
	public async Task PostADataObjectWithACancellationToken()
	{
		var data = Faker.Create<TestData>();

		var result = await webCaller.Post(BasicPath, data, 1.Minutes(), cancellationSource.Token);

		var jsonResponse = result.To<HttpBinDataJsonResponse<TestData>>();

		jsonResponse.Json.Should().BeEquivalentTo(data);
	}

	[Fact]
	public async Task PostAListOfDataWithACancellationToken()
	{
		var dataList = Faker.Create<List<TestData>>();

		var result = await webCaller.Post(BasicPath, dataList, 1.Minutes(), cancellationSource.Token);

		var jsonResponse = result.To<HttpBinDataJsonResponse<List<TestData>>>();

		jsonResponse.Json.Should().BeEquivalentTo(dataList);
	}

	[Fact]
	public async Task PostStringDataWithAContentTypeAndACancellationToken()
	{
		var plainText = "this is plain text";

		var result = await webCaller.Post(BasicPath, plainText, 1.Minutes(), "text/plain", cancellationSource.Token);

		response = result.To<HttpBinResponse>();

		response.ContentTypeHeader.Should().Be("text/plain; charset=utf-8");
	}

	[Fact]
	public async Task PostWithoutDataWithACancellationToken()
	{
		var result = await webCaller.Post(BasicPath, 1.Minutes(), cancellationSource.Token);

		result.IsSuccessful.Should().BeTrue();
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

	protected override Task<FatWebResponse> MakeCallToWeb(
		string path,
		TimeSpan timeout,
		CancellationToken cancellationToken
	)
	{
		return webCaller.Post(path, Faker.Create<TestData>(), timeout, cancellationToken);
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
