using Microsoft.Win32;

namespace FatCat.Toolkit.Tools;

#pragma warning disable CA1416

public interface IRegistryRepository
{
	object? Get(string applicationName, string keyName);

	void Set(string applicationName, string keyName, object value);
}

public class RegistryRepository : IRegistryRepository
{
	public object? Get(string applicationName, string keyName)
	{
		var subKey = OpenSubKey(applicationName);

		return subKey?.GetValue(keyName);
	}

	public void Set(string applicationName, string keyName, object value)
	{
		var key = Registry.LocalMachine.CreateSubKey(GetSubKeyName(applicationName));

		key.SetValue(keyName, value);
	}

	private static string GetSubKeyName(string applicationName) => $@"SOFTWARE\{applicationName}";

	private static RegistryKey? OpenSubKey(string applicationName) => Registry.LocalMachine.OpenSubKey(GetSubKeyName(applicationName));
}