using FatCat.Fakes;
using FatCat.Toolkit.Data.Mongo;
using FatCat.Toolkit.WebServer;
using Microsoft.AspNetCore.Mvc;

namespace SampleDocker;

public class TestObjectEndpoint : Endpoint
{
	private readonly IMongoRepository<TestDataObject> mongoRepository;

	public TestObjectEndpoint(IMongoRepository<TestDataObject> mongoRepository)
	{
		this.mongoRepository = mongoRepository;
	}

	[HttpGet("api/Test/Data")]
	public async Task<WebResult> GetTestDataObjects()
	{
		var fakeObject = Faker.Create<TestDataObject>();

		await mongoRepository.Create(fakeObject);

		var allItems = await mongoRepository.GetAll();

		return Ok(allItems);
	}
}
