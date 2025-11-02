using System.Text.Json;
using Microsoft.JSInterop;

namespace OneOffBlazor;

public class WebAuthnInterop(IJSRuntime js)
{
	public async Task<object> CreateCredentialAsync(object options)
	{
		var json = JsonSerializer.Serialize(options);

		return await js.InvokeAsync<JsonElement>("createCredential", json);
	}

	public async Task<object> GetAssertionAsync(object options)
	{
		var json = JsonSerializer.Serialize(options);

		return await js.InvokeAsync<JsonElement>("getAssertion", json);
	}
}
