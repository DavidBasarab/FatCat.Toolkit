using FatCat.Toolkit.Console;
using FatCat.Toolkit.Data.Mongo;
using Microsoft.Extensions.Configuration;

namespace SampleDocker;

public class SampleConnectionInformation : IMongoConnectionInformation
{
	private readonly IConfiguration configuration;

	public SampleConnectionInformation(IConfiguration configuration)
	{
		this.configuration = configuration;
	}

	public string GetConnectionString()
	{
		var connectionString = configuration.GetConnectionString("Mongo");

		ConsoleLog.WriteDarkCyan($"Using Sample Connection Information | Connection String: <{connectionString}>");

		return connectionString;
	}

	public string GetDatabaseName()
	{
		return null;
	}
}
