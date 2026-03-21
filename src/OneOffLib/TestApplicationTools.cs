using System.Net;
using System.Reflection;
using FatCat.Toolkit;

namespace OneOffLib;

public class TestApplicationTools : IApplicationTools
{
	public string ExecutableFullPath
	{
		get => "Junk";
	}

	public string ExecutableName
	{
		get => "This is from the Test Application Tools";
	}

	public string ExecutingDirectory
	{
		get => ExecutableName;
	}

	public bool InContainer { get; }

	public string MacAddress
	{
		get => ExecutableName;
	}

	public string MachineName
	{
		get => ExecutableName;
	}

	public ushort FindNextOpenPort(ushort startingPort)
	{
		throw new NotImplementedException();
	}

	public string GetHost()
	{
		throw new NotImplementedException();
	}

	public string GetIPAddress()
	{
		throw new NotImplementedException();
	}

	public IPAddress GetIPAddressObject()
	{
		throw new NotImplementedException();
	}

	public List<string> GetIPList()
	{
		throw new NotImplementedException();
	}

	public string GetVersionFromAssembly(Assembly assembly)
	{
		throw new NotImplementedException();
	}
}
