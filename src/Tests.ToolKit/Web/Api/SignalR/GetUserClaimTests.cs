using FatCat.Toolkit.Web.Api.SignalR;

namespace Tests.FatCat.Toolkit.Web.Api.SignalR;

public class GetUserClaimTests
{
	private readonly GetUserClaim getUserClaim;
	private ToolkitClaim claimToFind;
	private ToolkitUser user;

	public GetUserClaimTests()
	{
		SetUpData();

		getUserClaim = new GetUserClaim();
	}

	[Fact]
	public void IfClaimNameIsNotInTheListReturnNull()
	{
		getUserClaim.GetClaim(user, "NotInList").Should().BeNull();
	}

	[Fact]
	public void ReturnClaimBasedOnType()
	{
		getUserClaim.GetClaim(user, claimToFind.Type).Should().Be(claimToFind);
	}

	[Fact]
	public void ReturnClaimRegardlessOfCase()
	{
		getUserClaim.GetClaim(user, claimToFind.Type.ToUpper()).Should().Be(claimToFind);
	}

	private void SetUpData()
	{
		user = Faker.Create<ToolkitUser>();

		claimToFind = Faker.Create<ToolkitClaim>();

		user.Claims.Add(claimToFind);
	}
}
