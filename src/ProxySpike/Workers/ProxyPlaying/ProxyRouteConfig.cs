using FatCat.Fakes;
using FatCat.Toolkit.Console;
using Yarp.ReverseProxy.Configuration;

namespace ProxySpike.Workers.ProxyPlaying;

public class ProxyRouteConfig
{
	public static RouteConfig[] GetRoutes()
	{
		ConsoleLog.WriteDarkGreen("Building Routes for Proxy");

		return new[]
		{
			new RouteConfig
			{
				RouteId = $"route_{Faker.RandomInt()}",
				ClusterId = "cluster1",
				Match = new RouteMatch { Path = "{**catch-all}" },
			},
		};
	}
}

public static class ProxyClusterConfig
{
	public static ClusterConfig[] GetClusters()
	{
		ConsoleLog.WriteGreen("Building Clusters for proxy");

		return new[]
		{
			new ClusterConfig
			{
				ClusterId = "cluster1",
				Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
				{
					{
						"destination1",
						new DestinationConfig { Address = "https://localhost:14555/" }
					},
				},
			},
		};
	}
}
