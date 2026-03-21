using CommandLine;

namespace ProxySpike.Options;

[Verb("server", HelpText = "Starts the server")]
public class ServerOptions
{
	[Option('p', "port", Required = true)]
	public ushort WebPort { get; set; }
}
