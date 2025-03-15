using FatCat.Toolkit;
using FatCat.Toolkit.Console;

namespace OneOff.Old;

public class GeneratorWorker(IGenerator generator)
{
	public void DoWork()
	{
		var fakeList = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };

		for (var i = 0; i < 200; i++)
		{
			var randomIndex = generator.NextRandom(0, fakeList.Count);

			ConsoleLog.WriteCyan($"RandomIndex: <{randomIndex}> | Value: <{fakeList[randomIndex]}>");
		}
	}
}
