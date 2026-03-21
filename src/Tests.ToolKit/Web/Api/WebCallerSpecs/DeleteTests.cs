using FatCat.Toolkit.Web;

namespace Tests.FatCat.Toolkit.Web.Api.WebCallerSpecs;

public class DeleteTests : WebCallerTests
{
	protected override string BasicPath
	{
		get => "/delete";
	}

	protected override async Task<FatWebResponse> MakeCallToWeb(string path)
	{
		return await webCaller.Delete(path);
	}
}
