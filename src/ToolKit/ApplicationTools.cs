using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using FatCat.Toolkit.Extensions;

namespace FatCat.Toolkit;

public interface IApplicationTools
{
	string? ExecutableName { get; }

	string? ExecutingDirectory { get; }

	string? MacAddress { get; }

	ushort FindNextOpenPort(ushort startingPort);

	string GetHost();

	string? GetIPAddress();

	IPAddress? GetIPAddressObject();

	List<string> GetIPList();
}

public class ApplicationTools : IApplicationTools
{
	private string? executableName;
	private string? executingDirectory;

	private IPAddress? ipAddress;
	private string? macAddress;

	public string? ExecutableName
	{
		get
		{
			if (executableName.IsNullOrEmpty())
			{
				var fileName = Process.GetCurrentProcess().MainModule?.FileName;

				if (fileName!.IsNotNullOrEmpty()) executableName = Path.GetFileNameWithoutExtension(fileName);
			}

			return executableName;
		}
	}

	public string? ExecutingDirectory
	{
		get
		{
			if (executingDirectory.IsNullOrEmpty())
			{
				var fileName = Process.GetCurrentProcess().MainModule?.FileName;

				if (fileName!.IsNotNullOrEmpty()) executingDirectory = Path.GetDirectoryName(fileName);
			}

			return executingDirectory;
		}
	}

	public string? MacAddress
	{
		get
		{
			if (macAddress.IsNullOrEmpty()) FindMacAddress();

			return macAddress;
		}
	}

	public ushort FindNextOpenPort(ushort startingPort)
	{
		var usedPorts = new List<int>();

		var properties = IPGlobalProperties.GetIPGlobalProperties();

		// Ignore active connections
		var connections = properties.GetActiveTcpConnections();

		usedPorts.AddRange(from n in connections
							where n.LocalEndPoint.Port >= startingPort
							select n.LocalEndPoint.Port);

		// Ignore active tcp listeners
		var endPoints = properties.GetActiveTcpListeners();

		usedPorts.AddRange(from n in endPoints
							where n.Port >= startingPort
							select n.Port);

		// Ignore active udp listeners
		endPoints = properties.GetActiveUdpListeners();

		usedPorts.AddRange(from n in endPoints
							where n.Port >= startingPort
							select n.Port);

		usedPorts.Sort();

		for (var portToCheck = startingPort; portToCheck < ushort.MaxValue; portToCheck++)
		{
			if (!usedPorts.Contains(portToCheck)) return portToCheck;
		}

		return 0;
	}

	public string GetHost()
	{
		var domainName = IPGlobalProperties.GetIPGlobalProperties().DomainName;
		var hostName = Dns.GetHostName();

		if (domainName.IsNotNullOrEmpty())
		{
			domainName = "." + domainName;

			if (!hostName.EndsWith(domainName)) hostName += domainName;
		}

		return hostName.ToLower();
	}

	public string? GetIPAddress() => GetIPAddressObject()?.ToString();

	public IPAddress? GetIPAddressObject()
	{
		if (ipAddress != null) return ipAddress;

		ipAddress = IPAddress.Parse("127.0.0.1");

		var host = Dns.GetHostEntry(Dns.GetHostName());

		foreach (var ip in host.AddressList)
		{
			if (ip.AddressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6)
			{
				ipAddress = ip;

				break;
			}
		}

		return ipAddress;
	}

	public List<string> GetIPList()
	{
		var host = Dns.GetHostEntry(Dns.GetHostName());

		return host.AddressList.Where(ip => ip.AddressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6).Select(ip => ip.ToString()).ToList();
	}

	private void FindMacAddress()
	{
		var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

		foreach (var adapter in networkInterfaces)
		{
			macAddress = adapter.GetPhysicalAddress().ToString();

			if (macAddress.IsNotNullOrEmpty()) break;
		}
	}
}