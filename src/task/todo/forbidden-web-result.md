# forbidden-web-result (FatCat.Toolkit)

> **Origin:** raised by a consumer (`C:\Code\Fog`, phase 09 of `tasks/todo/locker-deletion-bug-fixes/`, "a non-creator gets 403, not 401").
> **Status:** suggestion, not started (2026-10-23). Fog already works around it on its side, so this is not blocking.

## Problem

`WebResult`, `WebResult<T>` and `Endpoint` (v1.0.353) have no helper for **403 Forbidden**, and the test assertions have no `BeForbidden()`.

```csharp
// src/Toolkit.WebServer/WebResult.cs
public static WebResult Unauthorized()
{
	return new WebResult(HttpStatusCode.Unauthorized);
}
// ...no Forbidden()

// src/Toolkit.WebServer/Endpoint.cs
protected new WebResult Unauthorized()
{
	return WebResult.Unauthorized();
}
// ...no Forbidden()
```

The only refusal helper is `Unauthorized()`, so consumers reach for it when a **signed-in** caller is not allowed on a resource. A 401 means "not signed in". A client that treats a 401 as an expired session signs the user out, when the right answer was "you are signed in, but this is not yours".

`WebResult<T>` has no `Unauthorized()` either, so neither refusal is available on the typed result.

## Consumer impact (Fog)

Fog's locker endpoints answered 401 to a signed-in lawyer who is not the locker's creator. Fog's site treats any 401 as an expired session and signs the lawyer out.

Fog now returns `new WebResult(HttpStatusCode.Forbidden)` from:
- `BaseBindingProcessor.SetResultToForbidden`, which its three locker model binders call;
- 17 inline "not the creator" checks in its endpoints.

Its tests assert `result.Should().HaveStatusCode(HttpStatusCode.Forbidden)`, because there is no `BeForbidden()`.

## Proposed fix

### 1. `Forbidden()` helpers

```csharp
// WebResult
public static WebResult Forbidden(string content = null)
{
	return new WebResult(HttpStatusCode.Forbidden, content);
}

// WebResult<T>
public static WebResult<T> Forbidden()
{
	return new WebResult<T>(WebResult.Forbidden());
}

// Endpoint
protected WebResult Forbidden(string message = null)
{
	return WebResult.Forbidden(message);
}
```

- `Endpoint.Forbidden` does **not** need `new`. `Controller.Forbid()` has a different name, so nothing is hidden. Check this when you build it.
- Optionally add `WebResult<T>.Unauthorized()` at the same time, for symmetry.

### 2. `BeForbidden()` assertions

Add `BeForbidden()` next to `BeUnauthorized()`, built on `HaveStatusCode(HttpStatusCode.Forbidden)`:

- `src/Toolkit.WebServer/Testing/WebResultAssertions.cs` (`WebResultAssertions`);
- `src/Toolkit.WebServer/Testing/WebResultClosedOverAssertions.cs` (`WebResultClosedOverAssertions<T>`). It has no `BeUnauthorized()` today either. Add both.

The client-side `FatWebResponseAssertions` (`src/ToolKit/Testing/WebResultAssertions.cs`) could get a matching `BeForbidden()` for symmetry. That is optional.

### Spec

Test first, one assertion per test, in `Tests.ToolKit`:
- `WebResult.Forbidden()` has status 403, and its content is the content passed in;
- `WebResult<T>.Forbidden()` has status 403 and `IsUnsuccessful` is true;
- `Endpoint.Forbidden()` returns a 403. Use a test endpoint like `TestingEndpoint`;
- `BeForbidden()` passes on a 403 and fails on a 401, on both assertion classes.

### Compatibility

The change only adds members, so no consumer breaks. Bump the patch version.

Once it ships, Fog can replace `new WebResult(HttpStatusCode.Forbidden)` with `Forbidden()`, and `HaveStatusCode(HttpStatusCode.Forbidden)` with `BeForbidden()`. That follow-up is a pure refactor with no change in behaviour.
