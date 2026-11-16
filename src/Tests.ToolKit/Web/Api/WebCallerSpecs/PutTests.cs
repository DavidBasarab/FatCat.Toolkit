using FatCat.Toolkit;
using FatCat.Toolkit.Json;
using FatCat.Toolkit.Web;
using Humanizer;

namespace Tests.FatCat.Toolkit.Web.Api.WebCallerSpecs;

public class PutTests : WebCallerTests
{
	protected override string BasicPath
	{
		get
		{
			return "/put";
		}
	}

	[Fact]
	public async Task CanMakeAPutWithCustomContentType()
	{
		var plainText = "this is plain text";

		var result = await webCaller.Put(BasicPath, plainText, "text/plain");

		result.IsSuccessful.Should().BeTrue();

		response = result.To<HttpBinResponse>();

		response.RawData.Should().Be(plainText);

		response.ContentTypeHeader.Should().Be("text/plain; charset=utf-8");
	}

	[Fact]
	public async Task CarryTheCallerTokenWhenPutingAList()
	{
		await cancellationSource.CancelAsync();

		var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			webCaller.Put(BasicPath, Faker.Create<List<TestData>>(), 1.Minutes(), cancellationSource.Token)
		);

		VerifyCarriesTheCallerToken(exception);
	}

	[Fact]
	public async Task CarryTheCallerTokenWhenPutingStringDataWithAContentType()
	{
		await cancellationSource.CancelAsync();

		var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			webCaller.Put(BasicPath, "this is plain text", 1.Minutes(), "text/plain", cancellationSource.Token)
		);

		VerifyCarriesTheCallerToken(exception);
	}

	[Fact]
	public async Task CarryTheCallerTokenWhenPutingWithoutData()
	{
		await cancellationSource.CancelAsync();

		var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			webCaller.Put(BasicPath, 1.Minutes(), cancellationSource.Token)
		);

		VerifyCarriesTheCallerToken(exception);
	}

	[Fact]
	public async Task PutADataObjectWithACancellationToken()
	{
		var data = Faker.Create<TestData>();

		var result = await webCaller.Put(BasicPath, data, 1.Minutes(), cancellationSource.Token);

		var jsonResponse = result.To<HttpBinDataJsonResponse<TestData>>();

		jsonResponse.Json.Should().BeEquivalentTo(data);
	}

	[Fact]
	public async Task PutAListOfDataWithACancellationToken()
	{
		var dataList = Faker.Create<List<TestData>>();

		var result = await webCaller.Put(BasicPath, dataList, 1.Minutes(), cancellationSource.Token);

		var jsonResponse = result.To<HttpBinDataJsonResponse<List<TestData>>>();

		jsonResponse.Json.Should().BeEquivalentTo(dataList);
	}

	[Fact]
	public async Task PutStringDataWithAContentTypeAndACancellationToken()
	{
		var plainText = "this is plain text";

		var result = await webCaller.Put(BasicPath, plainText, 1.Minutes(), "text/plain", cancellationSource.Token);

		response = result.To<HttpBinResponse>();

		response.ContentTypeHeader.Should().Be("text/plain; charset=utf-8");
	}

	[Fact]
	public async Task PutWithoutDataWithACancellationToken()
	{
		var result = await webCaller.Put(BasicPath, 1.Minutes(), cancellationSource.Token);

		result.IsSuccessful.Should().BeTrue();
	}

	[Fact]
	public async Task PutWithAListOfData()
	{
		var dataList = Faker.Create<List<TestData>>();

		var result = await webCaller.Put(BasicPath, dataList);

		result.IsSuccessful.Should().BeTrue();

		var jsonResponse = result.To<HttpBinDataJsonResponse<List<TestData>>>();

		jsonResponse.Json.Should().BeEquivalentTo(dataList);
	}

	[Fact]
	public async Task PutWithDataObject()
	{
		var data = Faker.Create<TestData>();

		var result = await webCaller.Put(BasicPath, data);

		result.IsSuccessful.Should().BeTrue();

		var jsonResponse = result.To<HttpBinDataJsonResponse<TestData>>();

		jsonResponse.Json.Should().BeEquivalentTo(data);
	}

	[Fact]
	public async Task PutWithJsonData()
	{
		var data = Faker.Create<TestData>();

		var json = new JsonOperations().Serialize(data);

		var result = await webCaller.Put(BasicPath, json);

		result.IsSuccessful.Should().BeTrue();

		var jsonResponse = result.To<HttpBinDataJsonResponse<TestData>>();

		jsonResponse.Json.Should().BeEquivalentTo(data);
	}

	[Fact]
	public async Task PutWithStringData()
	{
		var data = "this is just some data";

		var result = await webCaller.Put(BasicPath, data, "text/plain");

		result.IsSuccessful.Should().BeTrue();

		response = result.To<HttpBinResponse>();

		response.RawData.Should().Be($"{data}");
	}

	protected override Task<FatWebResponse> MakeCallToWeb(string path)
	{
		return webCaller.Put(path);
	}

	protected override Task<FatWebResponse> MakeCallToWeb(
		string path,
		TimeSpan timeout,
		CancellationToken cancellationToken
	)
	{
		return webCaller.Put(path, Faker.Create<TestData>(), timeout, cancellationToken);
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
