using FatCat.Toolkit.Web;

namespace FatCat.Toolkit.WebServer;

public static class FatWebResponseExtensions
{
	public static WebResult ToWebResult(this FatWebResponse response)
	{
		return new WebResult(response);
	}
}
