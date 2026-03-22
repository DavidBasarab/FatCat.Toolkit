using FatCat.Toolkit.WebServer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OneOff.MongoProof;

[AllowAnonymous]
public class ProofEndpoint : Endpoint
{
	[HttpGet("api/proof")]
	public WebResult Get()
	{
		return Ok("PROOF_GET_OK");
	}

	[HttpPost("api/proof")]
	public WebResult Post([FromBody] string message)
	{
		return Ok($"PROOF_POST_OK:{message}");
	}
}
