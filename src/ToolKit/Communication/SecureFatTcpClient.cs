﻿using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace FatCat.Toolkit.Communication;

public class SecureFatTcpClient : FatTcpClient, IFatTcpClient
{
	private readonly X509Certificate certificate;

	public SecureFatTcpClient(X509Certificate certificate)
	{
		this.certificate = certificate;
	}

	protected override Stream GetStream()
	{
		var sslStream = new SslStream(tcpClient.GetStream(), false, CertValidation, null);

		var clientCertificateCollection = new X509CertificateCollection(new[] { certificate });

		sslStream.AuthenticateAsClient(
			certificate.Subject,
			clientCertificateCollection,
			SslProtocols.Tls12,
			false
		);

		return sslStream;
	}

	private bool CertValidation(
		object sender,
		X509Certificate remoteCertificate,
		X509Chain chain,
		SslPolicyErrors sslPolicyErrors
	)
	{
		return CertificateHelper.CertValidation(remoteCertificate, certificate, sslPolicyErrors);
	}
}
