namespace FatCat.Toolkit.Web.Api;

[Flags]
public enum WebApplicationOptions
{
	None = 0,
	FileSystem = 1,
	SignalR = 2,
	Authentication = 4,
	HttpsRedirection = 8,
	Cors = 16,
	CommonOptions = Cors | HttpsRedirection,
}
