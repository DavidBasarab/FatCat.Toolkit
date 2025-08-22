using Autofac;
using FatCat.Toolkit.Cryptography;

namespace OneOffBlazor;

public class BlazorOneOffModule : Module
{
	protected override void Load(ContainerBuilder builder)
	{
		builder.RegisterType<OtherTestingService>().As<ITestingService>().SingleInstance();

		// Use Web Crypto in the browser for AES-GCM behind the shared interface
		builder.RegisterType<WebCryptoAesGcm>().As<IFatCatAesEncryption>().SingleInstance();
	}
}
