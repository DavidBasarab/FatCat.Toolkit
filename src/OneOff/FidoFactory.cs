using Fido2NetLib;

namespace OneOff;

public static class FidoFactory
{
	private static readonly IFido2 fido = new Fido2(
													new Fido2Configuration
													{
														ServerDomain = "localhost",
														ServerName = "Fido2 Test",
														Origins = new HashSet<string> { "https://localhost:14555", "http://localhost:5000" },
													}
													);

	public static IFido2 GetFido2()
	{
		return fido;
	}
}