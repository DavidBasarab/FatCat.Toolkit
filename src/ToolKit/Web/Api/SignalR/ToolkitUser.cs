using System.Security.Claims;

namespace FatCat.Toolkit.Web.Api.SignalR;

public class ToolkitUser : EqualObject
{
	public static ToolkitUser Create(ClaimsPrincipal contextUser)
	{
		if (contextUser == null)
		{
			return new ToolkitUser();
		}

		return new()
		{
			Name = contextUser.Identity?.Name,
			AuthenticationType = contextUser.Identity?.AuthenticationType,
			IsAuthenticated = contextUser.Identity?.IsAuthenticated ?? false,
			Claims = contextUser.Claims.Select(ToolkitClaim.Create).ToList(),
		};
	}

	public string AuthenticationType { get; set; }

	public List<ToolkitClaim> Claims { get; set; } = new();

	public bool IsAuthenticated { get; set; }

	public string Name { get; set; }
}
