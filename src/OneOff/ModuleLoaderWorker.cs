using FatCat.Toolkit;
using FatCat.Toolkit.Console;

namespace OneOff;

public class ModuleLoaderWorker(IApplicationTools applicationTools) : SpikeWorker
{
	public override async Task DoWork()
	{
		await Task.CompletedTask;

		ConsoleLog.WriteCyan($"ApplicationTools.Type := <{applicationTools.GetType().FullName}>");
		ConsoleLog.WriteMagenta($"ExecutableName := <{applicationTools.ExecutableName}>");
	}
}
