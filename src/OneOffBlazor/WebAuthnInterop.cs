using System.Text.Json;
using Microsoft.JSInterop;

namespace OneOffBlazor;

public class WebAuthnInterop(IJSRuntime js)
{
	public async Task<string> CreateCredentialAsync(object options)
	{
		var json = JsonSerializer.Serialize(options);
		return await js.InvokeAsync<string>("createCredential", json);
	}

	public async Task<string> GetAssertionAsync(object options)
	{
		var json = JsonSerializer.Serialize(options);
		return await js.InvokeAsync<string>("getAssertion", json);
	}
}
