using FatCat.Toolkit.Console;

namespace OneOff.Old;

public interface ISillyInterfaceThing
{
	void DoSomethingSilly();
}

public class SillyInterfaceThing : ISillyInterfaceThing
{
	public void DoSomethingSilly()
	{
		ConsoleLog.WriteCyan("This is going to be something silly!!!!!");
		ConsoleLog.WriteCyan(
			"silly, silly, silly, silly, silly, silly, silly, silly, silly, silly, silly, silly, silly, silly, silly"
		);
	}
}
