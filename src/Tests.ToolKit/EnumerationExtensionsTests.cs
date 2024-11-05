using FatCat.Toolkit.Extensions;
using FatCat.Toolkit.Web.Api;

namespace Tests.FatCat.Toolkit;

public class EnumerationExtensionsTests
{
	[Fact]
	public void IsFlagSetsWorks()
	{
		var options = WebApplicationOptions.HttpsRedirection | WebApplicationOptions.SignalR;

		options.IsFlagSet(WebApplicationOptions.HttpsRedirection).Should().BeTrue();

		options.IsFlagSet(WebApplicationOptions.Authentication).Should().BeFalse();
	}
}
