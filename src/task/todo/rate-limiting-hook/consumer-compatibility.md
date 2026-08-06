# Consumer Compatibility — Fog and Apostil

Checked `C:\Code\Fog` and `C:\Code\Apostil` against every phase in this plan (Phases 1–3,
`00-overview.md`).

| Repo | Packages | Version | Role |
|---|---|---|---|
| Fog | `FatCat.Toolkit` / `FatCat.Toolkit.WebServer` | **1.0.339** | server **and** client — heaviest consumer, and the requester of this work item |
| Apostil | `FatCat.Toolkit` / `FatCat.Toolkit.WebServer` | **1.0.343** | server on the current baseline; the **only** consumer that uses `ConfigureMiddleware` / `ConfigureLogging` today |

**Verdict: no phase in this plan is a breaking change for either repository.** Every change is
additive (two null-default properties) or log-content-only (Phase 3). Neither repo needs a code
change to keep working after upgrading; both need code changes only to opt in. No plan amendments
are required — unlike the SignalR plan, which needed two.

The single change that *would* have broken a consumer — moving the existing `ConfigureMiddleware`
invocation — is forbidden by overview ADR-1, gated in Phase 2's Definition of Done, and
independently re-checked by the orchestrator. **Apostil is the concrete reason** (see below).

---

## What Fog actually touches

`Fog/Common/Common.WebServer/Infrastructure/ApplicationRunner.cs` is the single place Fog builds the
toolkit host — `Common.WebServer.csproj` is the only project referencing `FatCat.Toolkit.WebServer`
(1.0.339). It constructs `ToolkitWebApplicationSettings` with `Options`, `TlsCertificate`,
`ContainerAssemblies`, `SignalRPath`, `OnWebApplicationStarted`, `ToolkitTokenParameters`, `Args`,
`JwtBearerEvents`, `AllowAllOrigins`, `CorsSevers`, `OnLogEvent`; subscribes all four hub events; and
calls `ToolkitWebApplication.Run`.

Relevant negatives, verified by search across the repo (excluding `bin`/`obj`):

- Fog sets **neither** `ConfigureMiddleware` **nor** `ConfigureLogging` — it has no middleware in the
  pre-routing seam whose relative order could change. (It cannot: those hooks are 1.0.340+, and Fog
  is on 339.)
- Fog implements **no `IStartupFilter`**. The only `ConfigureLogging` hit in the repo is a private
  method in `Common/Common/Infrastructure/CommonModule.cs` feeding `LoggerFactory.Create` — unrelated
  to the settings property and not a name that can collide with one.
- Fog sets neither `WebApplicationOptions.FileSystem` nor `BasePath`.
- Fog has **no** rate-limiting code today (`grep RateLimit` → nothing); it is waiting on this work.
- Fog never subclasses `ToolkitWebApplicationSettings`, never extends it with extension methods, and
  never compares two settings instances — the new properties cannot collide with a member or perturb
  `EqualObject` equality in any live path.
- Fog does **not** grep, parse, alert on, or test against `Error calling`,
  `Could not complete call to`, or `GetDisplayUrl` anywhere — searched `.cs`, `.ts` and `.json`
  across the repo including `EndToEndTests`.
- Fog **does** register `ToolkitLogger : IToolkitLogger`
  (`Common/Common/Infrastructure/CommonModule.cs:97`) over Serilog with the Mongo sink configured in
  `ApplicationRunner.ConfigureMongoLogSink` — so the toolkit's exception-path lines really do reach a
  database. That is what makes Phase 3 a live fix rather than a theoretical one.

## What Apostil actually touches

`Api/Apostil.Api/Infrastructure/Program.cs` is the single place Apostil builds the host
(`Apostil.Api.csproj` is the only project referencing `FatCat.Toolkit.WebServer`, 1.0.343). Unlike
Fog, it **uses both existing hooks**:

```csharp
ConfigureLogging = loggingBuilder =>
    loggingBuilder.Services.AddSingleton<ILoggerProvider>(provider =>
        new SerilogLoggerProvider(provider.GetRequiredService<ILogger>())),
ConfigureMiddleware = applicationBuilder => applicationBuilder.UseMiddleware<RequestLoggingMiddleware>(),
```

- `Options = CommonOptions | Authentication | SignalR` (= `Cors | HttpsRedirection | Authentication |
  SignalR`) — no `FileSystem`, no `BasePath`.
- Registers `SerilogToolkitLogger : IToolkitLogger` (`ApostilApiModule.cs:33`), so toolkit log lines
  flow into Apostil's Serilog sinks.
- No `IStartupFilter`, no rate-limiting code, no `GetDisplayUrl`/`DisplayPath` usage, no extension
  methods on `ToolkitWebApplicationSettings`, no subclassing, and no test or script that asserts on
  the toolkit's log strings (searched `.cs`/`.ts`/`.json` including `EndToEnd`).
- `Apostil.Site` references only `FatCat.Toolkit` (transitively, via `Apostil.Common`) and runs its
  own ASP.NET host — nothing in `Toolkit.WebServer` reaches it, so no phase can affect it.

**The important one:** Apostil's shipped `RequestLoggingMiddleware` depends on the *documented
position* of `ConfigureMiddleware`. Its own phase file
(`Apostil/tasks/done/api_logging/02-request-logging.md`) states it verbatim:

> "…positioned after forwarded-headers processing and the toolkit's exception-capture middleware,
> before static files and routing. **That position is the contract this middleware relies on**:
> proxy-corrected request values, exception coverage from the toolkit, and visibility of **all**
> downstream traffic (static, unmatched-route 404s, endpoints, the SignalR path)."

Its smoke check asserts a 404 on a nonsense route produces a completion log line — which only holds
while the hook stays upstream of routing. This is the concrete proof that ADR-1's "add a second hook,
do not move the existing one" is a correctness requirement, not a style preference.

---

## Phase-by-phase compatibility

### Phase 1 — `ConfigureServices` hook: **safe for both, verified**

Additive property, null by default, invoked null-conditionally. Neither repo sets it, so
`ApplicationStartUp.ConfigureServices` runs exactly the registrations it runs today and then invokes
nothing.

Two things specifically checked because both repos boot through Autofac:

1. `ToolkitWebApplication.Run` calls `applicationStartUp.ConfigureServices(builder.Services)` at
   line 60 and `builder.Build()` at line 66 — the hook runs **before** the container is built, so
   consumer registrations are in the collection in time.
2. `ToolkitServiceProviderFactory.CreateBuilder` does `containerBuilder.Populate(services)`
   (`src/ToolKit/Injection/ToolkitServiceProviderFactory.cs`), so anything registered via the hook
   resolves through Autofac alongside `SystemScope`'s assembly-scanned registrations. Both repos'
   `Run` path always goes through this factory — there is no MS-DI/Autofac split to worry about here.

Placing the invocation **outside** the existing `try/catch` (ADR-2) changes nothing for either repo:
the swallowed-exception behaviour of the toolkit's own registrations is untouched, and neither has a
delegate to throw.

Apostil-specific note: `ConfigureLogging` is invoked *inside* `AddLogging`, which is inside that
`try`. Phase 1 must not touch that call — Apostil's Serilog provider bridge depends on it. Phase 1's
design only appends a statement after the `catch` block closes.

### Phase 2 — `ConfigureRoutedMiddleware` hook: **safe for both, verified**

Additive property, null by default, invoked null-conditionally between `app.UseRouting()` and the
auth block. With it unset, `Configure` emits precisely the pipeline it emits today.

- **Fog:** does not use the pre-routing hook at all; nothing to reorder. When it opts in with
  `app.UseRateLimiter()`, its limiter sits after routing and before authentication — exactly right
  for the two `[AllowAnonymous]` passphrase endpoints
  (`Brume/Brume/Lokrs/Endpoints/SearchForLokrByWordsEndpoint.cs`, `[HttpGet("api/lokr/search")]`):
  throttled before any auth work, and the named policy binds because routing has already matched.
- **Apostil:** `RequestLoggingMiddleware` stays exactly where it is, so it keeps seeing static, 404
  and SignalR traffic, and its 404 smoke assertion keeps holding. Because it is *upstream* of the new
  seam, it still observes and logs the response of anything a routed-hook middleware produces — a
  `429` from a future rate limiter would appear in its completion line at Warning level, which is
  correct behaviour, not a regression.

Phase 2's DoD requires the reviewer to confirm from the diff that
`Settings.ConfigureMiddleware?.Invoke(app)` is byte-for-byte in its original position, and the
orchestrator repeats that check after phase 2.

### Phase 3 — path-only exception logging: **safe for both; improves Apostil's privacy posture**

No API removed or changed; `GetDisplayUrl` is a framework extension, not a toolkit one, so nothing
either repo compiles against moves. `DisplayPath` is purely additive public surface, and neither repo
defines an `HttpRequest` extension of that name (checked — no ambiguity at any call site).

The observable change is two log lines losing scheme, host and query string:

```
- Error calling https://brume.example/api/lokr/search?first=blue&second=river&third=stone
+ Error calling /api/lokr/search
```

- **Fog:** nothing reads those lines, so there is no regression surface. The fix is the point: a
  thrown exception on `api/lokr/search` currently persists a live access-code passphrase to Mongo
  through `ToolkitLogger`. Fog already stripped those words from every log statement it owns (its
  `email_opt_in` AC10); this closes the one it could not reach. Fog sets no `BasePath`, so
  `DisplayPath`'s `PathBase` term is empty and the result is exactly `Request.Path`.
- **Apostil:** nothing reads those lines either, and the change **resolves an inconsistency in its own
  logging policy**. Its `api_logging` ADR-4 privacy rule restricts its middleware to "method, path,
  status code, elapsed milliseconds, content type, content length" and forbids query strings —
  `RequestLoggingMiddleware` logs `context.Request.Path.Value` accordingly. Meanwhile the toolkit's
  exception line was writing the full URL, query string included, into the *same* Serilog sink via
  `SerilogToolkitLogger`. Phase 3 brings the toolkit line into line with the policy Apostil already
  applies to its own. Apostil also sets no `BasePath`, so the result is exactly `Request.Path` there
  too.

---

## Version gaps

| Repo | From | Also picks up on the way to 1.0.344 | Risk |
|---|---|---|---|
| Apostil | 1.0.343 | Nothing — 343 is the current source baseline | **Clean single-release bump.** |
| Fog | 1.0.339 | 340–342 (`ConfigureMiddleware`, `ConfigureLogging` — additive, null-default, Fog sets neither) and 343 (SignalR hub improvements) | Non-breaking on paper, but a three-release jump; give it its own build-and-smoke pass. |

For Fog's 343 hop specifically: that plan's own `03-consumer-compatibility.md` found it non-breaking
for Fog **given** the two amendments it required, both of which shipped. Fog subscribes all four hub
events, so the null-guard work is a pure improvement for it, and its `SignalConnection` still receives
`TimeoutException` on the request/response path.

The Fog upgrade pass belongs to Fog, not to this plan; this plan ends with three commits on
`rate-limiting-hook`.

Out of scope and intentionally left alone: `Fog/Spikes/dude2` pins 1.0.240 / 1.0.64. Spike code; do
not upgrade it.

---

## What the consumers do after 1.0.344 is published

Not part of this plan — recorded so the handoff is unambiguous.

**Fog** (`tasks/todo/email_opt_in/07-passphrase-endpoint-hardening.md`):

1. Bump `FatCat.Toolkit` / `FatCat.Toolkit.WebServer` to 1.0.344 in the four non-spike `.csproj`
   files; build and smoke the whole app for the 339 → 344 jump.
2. In `Common/Common.WebServer/Infrastructure/ApplicationRunner.cs`, add to the settings initializer:

   ```csharp
   ConfigureServices = services =>
       services.AddRateLimiter(options => { /* fixed window, partitioned by source IP */ }),
   ConfigureRoutedMiddleware = applicationBuilder => applicationBuilder.UseRateLimiter(),
   ```

3. Put `[EnableRateLimiting("…")]` on `SearchForLokrByWordsEndpoint` and the second anonymous
   passphrase endpoint.
4. Partition by the **forwarded** client IP — the toolkit runs `UseForwardedHeaders` first with
   `ForwardLimit = 1` and cleared known-proxy lists, so `HttpContext.Connection.RemoteIpAddress` is
   already proxy-corrected when the limiter sees it.

**Apostil:** nothing required. A one-line version bump in `Apostil.Api.csproj` picks up the safer
exception logging; the two new hooks are available if it ever wants throttling on its anonymous
endpoints, and it should use `ConfigureRoutedMiddleware` — not `ConfigureMiddleware` — for anything
endpoint-metadata-aware.

---

## Summary

| Phase | Compile break (either repo) | Runtime change with hooks unset | Fog | Apostil |
|---|---|---|---|---|
| 1 — `ConfigureServices` | No | None | Safe | Safe (`ConfigureLogging` call untouched) |
| 2 — `ConfigureRoutedMiddleware` | No | None (existing hook position frozen by DoD + orchestrator check) | Safe | Safe — **and the reason the freeze is mandatory** |
| 3 — path-only exception logs | No | Two log lines lose the query string — the intended fix | Safe; closes its Mongo-sink leak | Safe; aligns the toolkit line with its own privacy ADR |

**No plan amendments are required for either consumer.** The only follow-up is Fog's own 339 → 344
upgrade pass.
