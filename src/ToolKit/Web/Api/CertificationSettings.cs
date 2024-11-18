using System.Security.Cryptography.X509Certificates;

namespace FatCat.Toolkit.Web.Api;

public class CertificationSettings : EqualObject
{
	public string Location { get; set; }

	public string Password { get; set; }

	public X509Certificate2 GetCertificate()
	{
		if (Location == null || Password == null)
		{
			return null;
		}

		return X509CertificateLoader.LoadPkcs12FromFile(Location, Password);
	}
}
