using FatCat.Fakes;
using FatCat.Toolkit;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.WebServer;

namespace OneOff.Old;

public class SomeObject : EqualObject
{
	public string FirstName { get; set; }

	public string LastName { get; set; }

	public int Number { get; set; }

	public SomeEnum SomeEnum { get; set; }
}

public class WebResultWorker : SpikeWorker
{
	public override async Task DoWork()
	{
		await Task.CompletedTask;

		var some = Faker.Create<SomeObject>();

		var result = WebResult.Ok(some);

		ConsoleLog.WriteCyan(result.Content);
	}
}
