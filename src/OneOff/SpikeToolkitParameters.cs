using System.Security.Cryptography.X509Certificates;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.WebServer;
using Humanizer;
using Microsoft.IdentityModel.Tokens;

namespace OneOff;

public class SpikeToolkitParameters : IToolkitTokenParameters
{
	public TokenValidationParameters Get()
	{
		ConsoleLog.WriteCyan("Getting token parameters");

		var cert = new X509Certificate2(@"C:\DevelopmentCert\DevelopmentCert.pfx", "basarab_cert");

		return new TokenValidationParameters
				{
					IssuerSigningKey = new X509SecurityKey(cert),
					ValidAudience = "https://foghaze.com/Brume",
					ValidIssuer = "FogHaze",
					ClockSkew = 10.Seconds()
				};
	}
}