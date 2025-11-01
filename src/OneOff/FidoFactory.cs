using Fido2NetLib;

namespace OneOff;

public static class FidoFactory
{
	private static readonly IFido2 fido = new Fido2(
		new Fido2Configuration
		{
			// Must be domain only, NOT including port or scheme
			ServerDomain = "localhost",
			ServerName = "Fido2 Test (Dev)",
			// Allow all your common dev ports (Blazor + API)
			Origins = new HashSet<string>
			{
				"http://localhost:5023", // Blazor (dotnet watch)
				"http://localhost:14555", // API
				"http://localhost:5000", // fallback
				"https://localhost:7180", // if you sometimes use HTTPS
			},
		}
	);

	public static IFido2 GetFido2() => fido;
}
