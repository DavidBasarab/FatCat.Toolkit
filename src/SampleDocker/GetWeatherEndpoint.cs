using FatCat.Fakes;
using FatCat.Toolkit.Injection;
using FatCat.Toolkit.Json;
using FatCat.Toolkit.WebServer;
using Microsoft.AspNetCore.Mvc;

namespace SampleDocker;

public class GetWeatherEndpoint(IExampleWorker exampleWorker, ISystemScope scope, IJsonOperations jsonOperations)
	: Endpoint
{
	private static readonly string[] Summaries =
	{
		"Freezing",
		"Bracing",
		"Chilly",
		"Cool",
		"Mild",
		"Warm",
		"Balmy",
		"Hot",
		"Sweltering",
		"Scorching",
	};

	[HttpGet("api/weather")]
	public WebResult GetWeather()
	{
		var secondInjectedThing = scope.Resolve<ISecondInjectedThing>();

		var items = Enumerable
			.Range(1, 5)
			.Select(index => new WeatherForecast
			{
				Date = DateTime.Now.AddDays(index),
				TemperatureC = Random.Shared.Next(-20, 55),
				Summary = Summaries[Random.Shared.Next(Summaries.Length)],
				MetaData = $"This has been added by me David Basarab - <{Faker.RandomInt()}>",
				SecondMetaData = $"Just more fake goodness - <{Faker.RandomInt()}>",
				SomeMessage = $"{exampleWorker.GetMessage()} | <{secondInjectedThing.GetSomeNumber()}>",
			})
			.ToArray();

		return WebResult.Ok(jsonOperations.Serialize(items));
	}
}
