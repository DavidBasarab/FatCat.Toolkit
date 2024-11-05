using System.Security.Claims;

namespace FatCat.Toolkit.Web.Api.SignalR;

public class ToolkitClaim : EqualObject
{
	public static ToolkitClaim Create(Claim claim)
	{
		return new ToolkitClaim
		{
			Issuer = claim.Issuer,
			Type = claim.Type,
			Value = claim.Value,
			OriginalIssuer = claim.OriginalIssuer,
		};
	}

	public string Issuer { get; set; }

	public string OriginalIssuer { get; set; }

	public string Type { get; set; }

	public string Value { get; set; }

	public ToolkitClaim() { }

	public ToolkitClaim(string claimType, string value)
	{
		Type = claimType;
		Value = value;
	}
}
