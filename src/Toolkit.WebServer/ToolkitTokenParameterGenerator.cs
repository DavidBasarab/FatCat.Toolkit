using Microsoft.IdentityModel.Tokens;

namespace FatCat.Toolkit.WebServer;

/// <summary>
///  Used to get TokenParameters for the Api
/// </summary>
public interface IToolkitTokenParameters
{
	public TokenValidationParameters Get();
}
