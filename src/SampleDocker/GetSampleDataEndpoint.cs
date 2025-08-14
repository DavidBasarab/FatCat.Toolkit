using FatCat.Fakes;
using FatCat.Toolkit;
using FatCat.Toolkit.Data.Mongo;
using FatCat.Toolkit.WebServer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace SampleDocker;

public class GetSampleDataEndpoint : Endpoint
{
	private readonly IConfiguration configuration;
	private readonly IMongoRepository<TestDataObject> mongoRepository;

	public GetSampleDataEndpoint(IConfiguration configuration, IMongoRepository<TestDataObject> mongoRepository)
	{
		this.configuration = configuration;
		this.mongoRepository = mongoRepository;
	}

	[HttpGet("api/SampleData")]
	public async Task<WebResult> GetSampleData()
	{
		var response = new SampleResponse
		{
			ConfigValue = configuration["PlayingSetting"],
			SomeMetaData = "Some Meta Data",
		};

		var testItem = Faker.Create<TestDataObject>();

		await mongoRepository.Create(testItem);

		return Ok(response);
	}
}

public class SampleResponse : EqualObject
{
	public string ConfigValue { get; set; }

	public string SomeMetaData { get; set; }
}
