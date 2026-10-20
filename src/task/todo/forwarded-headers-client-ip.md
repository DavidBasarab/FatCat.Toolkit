# forwarded-headers-client-ip (FatCat.Toolkit)

> **Origin:** raised by a consumer (`C:\Code\Fog`, bug **B-1** in `tasks/done/more-ux-features/13-report.md`; planning in `tasks/planning/vpn-check-decisions.md`).
> **Status:** not started (2026-10-20). **This is a gate:** Fog's VPN-detection work starts only after this ships and Fog picks up the new version.

## Problem

`ToolkitWebApplication.Run` (v1.0.352) hard-codes the forwarded-headers setup, and consumers cannot change it:

```csharp
// src/Toolkit.WebServer/ToolkitWebApplication.cs
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
	options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
	options.KnownIPNetworks.Clear();
	options.KnownProxies.Clear();
	options.ForwardLimit = 1;
});

// src/Toolkit.WebServer/ApplicationStartUp.cs
app.UseForwardedHeaders();   // always on
```

At request time this does three things:

1. It **trusts any sender**, because both known lists are cleared.
2. It takes the **right-most** `X-Forwarded-For` hop and puts it in `HttpContext.Connection.RemoteIpAddress`. That is one hop, because `ForwardLimit = 1`.
3. It **leaves the remaining hops in `X-Forwarded-For`**. Every hop left there was written by the caller. The original header is also copied to `X-Original-For`.

On Azure App Service this result is correct. The front end appends the real client address last, so `RemoteIpAddress` ends up as the real client.

It goes wrong in two ways:

- **Misleading leftovers.** A consumer that reads `X-Forwarded-For` itself sees only values the caller wrote. Fog's `ClientIpAddress` read the right-most of the remaining hops.
  - Example: a client sends `X-Forwarded-For: 6.6.6.6`. Azure makes it `6.6.6.6, 1.2.3.4`. The middleware moves `1.2.3.4` into `RemoteIpAddress` and leaves `6.6.6.6`.
  - Fog then records **6.6.6.6** as the client IP.
- **No proxy in front, so any header is trusted.** This applies whenever the app is reachable without the Azure front end: running locally, hitting a container directly, or a misconfigured ingress. One forged hop then becomes `RemoteIpAddress`.
  - The code comment says "Only do this if your app is NOT directly exposed". The consumer has no way to say otherwise.

## Consumer impact (Fog)

Fog keys its passphrase lockout, its per-IP rate limiters, the stored consent IP, and geolocation on the client IP. Its next feature adds paid MaxMind VPN detection on the same IP.

The walk reproduced all of the following with a single header:

- **The lockout is bypassed.** A locked-out browser that adds `X-Forwarded-For: 198.51.100.7` gets the locker back with 200.
- **The limiters reset.** Changing the header resets every per-IP limiter.
- **A victim can be locked out.** An attacker can send failed attempts that claim the victim's IP.
- **Location and VPN checks can be faked.** A recipient who sends a clean US home IP passes the US-location check. Once VPN detection exists, that same header would also hide a VPN.
- **We'd pay for lookups the attacker chooses.** An attacker who changes the IP on every request decides which addresses we pay MaxMind to look up.

## Proposed fix

### 1. Let consumers configure forwarded headers

Add a hook to `ToolkitWebApplicationSettings`, following the existing `ConfigureServices` / `ConfigureMiddleware` hooks:

```csharp
public Action<ForwardedHeadersOptions> ConfigureForwardedHeaders { get; set; }
```

- Apply the current defaults first, then invoke the hook, so consumers that don't set it see **no behavior change**.
- Consumers can then:
  - set `KnownProxies` / `KnownIPNetworks`,
  - change `ForwardLimit`, or
  - set `ForwardedHeaders = ForwardedHeaders.None` to turn forwarding off.

### 2. Let consumers turn the middleware off

Add a `WebApplicationOptions.ForwardedHeaders` flag and include it in the **default** `Options` value, so current behavior is kept. `ApplicationStartUp.Configure` calls `app.UseForwardedHeaders()` only when the flag is set.

- Example: an app with no proxy in front drops the flag, and `RemoteIpAddress` becomes the raw socket address.

### 3. Remove the leftover `X-Forwarded-For` hops (opt-in)

Add a setting, `bool StripUnconsumedForwardedFor` (default `false` for compatibility). When it is `true`, a small middleware placed right after `UseForwardedHeaders` removes whatever is left in `X-Forwarded-For`.

- After that, no downstream code can mistake a caller-written hop for the client.
- `X-Original-For` still keeps the full original header for diagnostics.
- Consider making this the default in the next minor version.

### 4. Document the contract

Add a short section to the README / XML docs:

> After the toolkit's forwarded-headers middleware runs, `HttpContext.Connection.RemoteIpAddress` **is** the client IP. Do not parse `X-Forwarded-For` yourself; whatever is left in it was written by the caller. The default trusts any proxy with `ForwardLimit = 1`, which is correct only when every request passes through exactly one trusted proxy that appends the client address, such as Azure App Service. Behind Azure Front Door or a CDN as well, raise `ForwardLimit` and set `KnownIPNetworks` through `ConfigureForwardedHeaders`.

## Specs

Use `TestServer` / `WebApplicationFactory`, or build the middleware pipeline directly with `ApplicationBuilder`, and assert on `HttpContext.Connection.RemoteIpAddress` and `Request.Headers["X-Forwarded-For"]`:

1. **Default, one hop:** `X-Forwarded-For: 1.2.3.4` → `RemoteIpAddress == 1.2.3.4`.
2. **Default, client-written hop plus proxy hop:** `X-Forwarded-For: 6.6.6.6, 1.2.3.4` → `RemoteIpAddress == 1.2.3.4`, and the leftover header is `6.6.6.6`. This pins today's behavior.
3. **`StripUnconsumedForwardedFor = true`:** same request → `RemoteIpAddress == 1.2.3.4`, `X-Forwarded-For` is empty or absent, and `X-Original-For` still holds the original value.
4. **`ForwardedHeaders` flag removed:** `X-Forwarded-For: 6.6.6.6` → `RemoteIpAddress` is the connection address, not 6.6.6.6.
5. **`ConfigureForwardedHeaders` sets `KnownProxies` to an address that isn't the sender** → the header is ignored and `RemoteIpAddress` is the connection address.
6. **`ConfigureForwardedHeaders` not set** → the options equal today's defaults (`ForwardLimit 1`, empty known lists, For | Proto | Host).

## Compatibility

- Everything is additive. A consumer that changes nothing gets byte-for-byte the same pipeline: the flag is on by default, stripping is off by default, and the hook is null.
- Bump the **minor** version, since new public settings are added.
- Run the change past `src/task/todo/rate-limiting-hook/consumer-compatibility.md` for any overlap with the `ConfigureMiddleware` ordering.

## What Fog will do once this ships (not part of this task)

1. Bump `FatCat.Toolkit.WebServer` in `Common/Common.WebServer/Common.WebServer.csproj`.
2. Change `Fog.Brume.GeoLocations.ClientIpAddress.Get()` to read **only** `Connection.RemoteIpAddress` (canonicalised, with IPv4-mapped IPv6 converted to IPv4) and stop parsing `X-Forwarded-For`.
3. Set `StripUnconsumedForwardedFor = true` where Fog builds its settings (`Common/Common.WebServer/Infrastructure/ApplicationRunner.cs`, shared by Brume and Haze).
4. Re-verify on staging from two devices: `curl -H "X-Forwarded-For: 6.6.6.6" …/api/geo/live-test` must echo the real IP, and the passphrase-lockout bypass must no longer work.
