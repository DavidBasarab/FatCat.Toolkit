using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.WebServer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace SampleDocker;

[AllowAnonymous]
public class GetSampleToken : Endpoint
{
	private readonly JwtSecurityTokenHandler securityTokenHandler = new();

	[HttpGet("api/Sample/Token")]
	public WebResult GetToken()
	{
		var userClaims = new[] { new Claim(ClaimTypes.Name, "John Doe") };

		var identity = new ClaimsIdentity(userClaims);

		var securityToken = GetSecurityTokenDescriptorCommon(identity);

		var token = securityTokenHandler.CreateToken(securityToken);

		var tokenString = securityTokenHandler.WriteToken(token);

		ConsoleLog.WriteCyan($"TokenString <{tokenString}>");

		return Ok(tokenString);
	}

	private SecurityTokenDescriptor GetSecurityTokenDescriptorCommon(ClaimsIdentity user)
	{
		var key = new RsaSecurityKey(SecureData.Rsa);

		return new SecurityTokenDescriptor
		{
			Subject = user,
			Expires = DateTime.UtcNow.AddMinutes(15),
			Audience = "https://foghaze.com/Brume",
			Issuer = "FogHaze",
			NotBefore = DateTime.UtcNow.AddSeconds(-10),
			SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSsaPssSha256),
		};
	}
}
