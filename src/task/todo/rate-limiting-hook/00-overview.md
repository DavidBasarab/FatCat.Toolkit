# rate-limiting-hook (FatCat.Toolkit) — Overview

> **Raised by a consumer, not yet planned into phases by this repo's owner.** Written from
> `C:\Code\Fog` while executing that repo's `email_opt_in` phase 07
> (`tasks/todo/email_opt_in/07-passphrase-endpoint-hardening.md`), which was **blocked** by the
> gap described below and stopped rather than restructuring its own startup to work around it.

## Work Item

Add an opt-in **service-registration hook** to `FatCat.Toolkit.WebServer` so a consuming
application can call `IServiceCollection` extensions — specifically ASP.NET Core's
`AddRateLimiter(...)` — during host construction, and make the existing middleware hook usable
for middleware that must observe **endpoint metadata**.

Two separate gaps, both required before a consumer can register a per-endpoint rate limiter:

1. **No `ConfigureServices` hook.** `ApplicationStartUp.ConfigureServices(IServiceCollection)`
   (`src/Toolkit.WebServer/ApplicationStartUp.cs`) registers controllers, CORS, auth, SignalR and
   logging, and then returns. `ToolkitWebApplicationSettings` exposes `ConfigureLogging` and
   `ConfigureMiddleware`, but **nothing** that reaches `builder.Services`. A consumer therefore
   cannot call `services.AddRateLimiter(...)`, `services.AddOutputCache(...)`, or any other
   `IServiceCollection` extension.

2. **`ConfigureMiddleware` runs before `UseRouting`.** `ApplicationStartUp.Configure` invokes
   `ToolkitWebApplication.Settings.ConfigureMiddleware?.Invoke(app)` immediately after
   `app.Use(CaptureMiddlewareExceptions)` and **before** `app.UseFileServer()` / `app.UseRouting()`.
   ASP.NET Core's `RateLimitingMiddleware` resolves its policy from
   `HttpContext.GetEndpoint()`, which is `null` until routing has run. Middleware added through the
   existing hook therefore cannot see `[EnableRateLimiting("policy")]` on a controller action — only
   a `GlobalLimiter` that inspects `Request.Path` by hand would work, which defeats the point of the
   framework's named-policy model.

Both additions should be **null by default and change nothing when unset**, matching the style
already established by `OnLogEvent`, `OnWebApplicationStarted`, `ConfigureLogging` and
`ConfigureMiddleware` (see `src/task/logging/`, which added the last two).

## Why the consumer could not work around it

The consumer (`Fog`'s `Brume` API) has two `[AllowAnonymous]` `GET` endpoints that accept a
three-word passphrase. They need a fixed-window, per-source-IP limiter. Every workaround was
rejected:

- **`app.UseRateLimiter(options)` from `ConfigureMiddleware`.** The options overload does not
  require `AddRateLimiter`, so it compiles — but it lands before `UseRouting`, so named policies
  never bind. Only a path-sniffing global limiter would function.
- **An `IStartupFilter` registered through the consumer's Autofac assembly scan.** Startup filters
  wrap `Configure`, so the middleware still lands before `UseRouting`, and it still cannot reach
  `builder.Services`. It is also exactly the "`IStartupFilter`-through-Autofac trick" that
  `src/task/logging/00-overview.md` set out to remove.
- **Replacing the consumer's `ApplicationRunner`/host bootstrap.** Explicitly out of scope for the
  consumer's phase — restructuring an app's startup to compensate for a missing toolkit hook is the
  problem this task exists to fix.

## Suggested shape

```csharp
// ToolkitWebApplicationSettings — alongside ConfigureLogging / ConfigureMiddleware
public Action<IServiceCollection> ConfigureServices { get; set; }
```

Invoked at the end of `ApplicationStartUp.ConfigureServices`, after the toolkit's own
registrations, so a consumer can add to — or deliberately override — them.

For gap 2, the cheapest additive option is a **second**, later middleware hook rather than moving
the existing one (moving it is a behaviour change for every current consumer):

```csharp
// Invoked in ApplicationStartUp.Configure immediately after app.UseRouting()
// and before the authentication/authorization block.
public Action<IApplicationBuilder> ConfigureRoutedMiddleware { get; set; }
```

`UseRateLimiter()` is documented to sit after `UseRouting` and before `UseAuthorization`, so that
position serves the motivating case and is a sensible general "post-routing" seam.

Naming, ordering and whether to instead parameterise the existing hook are the toolkit owner's
call — this document records the requirement and the constraint, not a mandated API.

## Related finding, same file — request URLs are logged on the exception path

`ApplicationStartUp.CaptureMiddlewareExceptions` logs `context.Request.GetDisplayUrl()` — the
**full URL including the query string** — on both the `TaskCanceledException` and general
`catch` branches:

```csharp
logger!.Information($"Could not complete call to {displayUrl}");
logger!.Warning($"Error calling {displayUrl}");
```

Any consumer whose API carries a secret in the query string therefore leaks it to the log sink
whenever a request throws. `Fog` has exactly that shape:
`GET api/lokr/search?first=…&second=…&third=…` carries a live access-code passphrase. The
consumer removed every one of its own log statements that echoed those words (its AC10), but
cannot remove this one.

Worth considering alongside the hooks above: log `Request.Path` rather than
`GetDisplayUrl()`, or redact the query string, or expose the formatting as another hook.
CWE-532 (Insertion of Sensitive Information into Log File).

## Acceptance Criteria

- [ ] A consumer can call `services.AddRateLimiter(...)` (or any `IServiceCollection` extension)
      without editing the toolkit or replacing `ToolkitWebApplication.Run`.
- [ ] A consumer can add middleware that observes endpoint metadata — a controller action carrying
      `[EnableRateLimiting("name")]` is limited by that named policy.
- [ ] Both hooks unset ⇒ byte-for-byte current behaviour; the whole existing suite stays green.
- [ ] `OneOff` demonstrates both hooks (a request over the limit returns `429`).
- [ ] `dotnet build` / `dotnet test` on `ToolKit.sln` clean.

## Consumer impact if not done

`Fog`'s two anonymous passphrase-lookup endpoints ship **unthrottled**. The consumer closed the
much larger hole in the same phase (an unescaped `$regex` that let `?first=.*&second=.*&third=.*`
return a real Lokr to an anonymous caller) and capped failed attempts per access code, but blind
guessing against the three-word keyspace is only bounded by a rate limiter. See
`C:\Code\Fog\tasks\todo\email_opt_in\07-passphrase-endpoint-hardening.md` and its overview's
**A5** / **OQ-4**.
