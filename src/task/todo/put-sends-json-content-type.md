# put-sends-json-content-type (FatCat.Toolkit)

> **Origin:** raised by a consumer (`C:\Code\Fog`, finding F-1 in `tasks/todo/new-color-scheme/10-report.md`).
> **Status:** suggestion, not started (2026-10-05). Fog has already worked around it on its side, so this is not blocking.

## Problem

The typed `WebCaller.Put` overloads serialize the body to JSON but send it **without a content type**:

```csharp
// src/ToolKit/Web/WebCaller.cs (v1.0.351)
public Task<FatWebResponse> Put<T>(string url, T data)
{
	var json = jsonOperations.Serialize(data);

	return SendWebRequest(HttpMethod.Put, url, Timeout, json);   // no "application/json"
}
```

`SendWebRequest` then builds `new StringContent(data, Encoding.UTF8, contentType)` with `contentType == null`.
`StringContent` falls back to **`text/plain; charset=utf-8`** in that case.

The matching `Post<T>` overloads pass `"application/json"`, so POST and PUT behave differently for the same typed call.

Affected overloads (all four typed PUTs):

- `Put<T>(string url, T data)`
- `Put<T>(string url, List<T> data)`
- `Put<T>(string url, T data, TimeSpan timeout)`
- `Put<T>(string url, List<T> data, TimeSpan timeout)`

The untyped `Put(string url, string data)` / `Put(string url, string data, TimeSpan timeout)` also send `text/plain`. That is arguably correct, since the caller passed a raw string, and the `contentType` overloads exist for that case. Leave them as they are.

## Consumer impact

An ASP.NET Core endpoint that takes `[FromBody] SomeRequest` rejects the request with **415 Unsupported Media Type** before the action runs, so the server logs nothing.

Fog hit this when it saved a palette edit (`PUT api/admin/palettes/{id}`) from its admin site. Unit tests fake `IWebCaller`, so the content type never showed up in them.

Fog now calls `Put(url, json, "application/json")` with JSON it serializes itself.

## Proposed fix

Pass `"application/json"` from the four typed `Put<T>` overloads, the same way `Post<T>` does:

```csharp
return SendWebRequest(HttpMethod.Put, url, Timeout, json, "application/json");
```

### Spec

Add a spec that pins the content type for each typed `Put<T>` overload (and, for symmetry, `Post<T>`). Use a capturing `HttpMessageHandler` through `SetClient`, and assert `request.Content.Headers.ContentType.MediaType == "application/json"`.

### Compatibility

The change is behavioural and only affects consumers that call a typed `Put<T>`.

- A server that accepted `text/plain` and parsed the body by hand will now receive `application/json`. That is very unlikely to break anything.
- Any server that model-binds a JSON body starts working.

Bump the patch version.
