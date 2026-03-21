namespace FatCat.Toolkit.Web.Api.SignalR;

public interface IGetUserClaim
{
	/// <summary>
	///  Return claim from the user if found
	/// </summary>
	/// <param name="user">ToolkitUser</param>
	/// <param name="claimType">name of the claim</param>
	/// <returns>Claim, null if claim is not found</returns>
	public ToolkitClaim GetClaim(ToolkitUser user, string claimType);
}

public class GetUserClaim : IGetUserClaim
{
	public ToolkitClaim GetClaim(ToolkitUser user, string claimType)
	{
		return user.Claims.FirstOrDefault(i => i.Type!.Equals(claimType, StringComparison.OrdinalIgnoreCase));
	}
}
