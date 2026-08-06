# rate-limiting-hook (FatCat.Toolkit) — Overview

> **Origin:** raised by a consumer. Written from `C:\Code\Fog` while executing that repo's
> `email_opt_in` phase 07 (`tasks/todo/email_opt_in/07-passphrase-endpoint-hardening.md`), which was
> **blocked** by the gaps described below and stopped rather than restructuring its own startup to
> work around them.
>
> **Status:** planned into phases (2026-08-06). Phases 1–3 below; runbook in `orchestrator.md`;
> consumer verification in `consumer-compatibility.md`.

## Work Item

Add opt-in hooks to `FatCat.Toolkit.WebServer` so a consuming application can call
`IServiceCollection` extensions — specifically ASP.NET Core's `AddRateLimiter(...)` — during host
construction, and can add middleware that observes **endpoint metadata**.

Three items, the first two required before a consumer can register a per-endpoint rate limiter:

1. **No `ConfigureServices` hook.** `ApplicationStartUp.ConfigureServices(IServiceCollection)`
   (`src/Toolkit.WebServer/ApplicationStartUp.cs`) registers controllers, CORS, auth, SignalR and
   logging, and then returns. `ToolkitWebApplicationSettings` exposes `ConfigureLogging` and
   `ConfigureMiddleware`, but **nothing** that reaches `builder.Services`. A consumer therefore
   cannot call `services.AddRateLimiter(...)`, `services.AddOutputCache(...)`, or any other
   `IServiceCollection` extension.

2. **`ConfigureMiddleware` runs before `UseRouting`.** `ApplicationStartUp.Configure` invokes
   `ToolkitWebApplication.Settings.ConfigureMiddleware?.Invoke(app)` immediately after
   `app.Use(CaptureMiddlewareExceptions)` and **before** `app.UseFileServer()` / `app.UseRouting()`.
   ASP.NET Core's `RateLimitingMiddleware` resolves its policy from `HttpContext.GetEndpoint()`,
   which is `null` until routing has run. Middleware added through the existing hook therefore
   cannot see `[EnableRateLimiting("policy")]` on a controller action — only a `GlobalLimiter` that
   inspects `Request.Path` by hand would work, which defeats the point of the framework's
   named-policy model.

3. **Request URLs are logged with their query strings on the exception path.**
   `ApplicationStartUp.CaptureMiddlewareExceptions` logs `context.Request.GetDisplayUrl()` — the
   **full URL including the query string** — on both the `TaskCanceledException` and the general
   `catch` branch:

   ```csharp
   logger!.Information($"Could not complete call to {displayUrl}");
   logger!.Warning($"Error calling {displayUrl}");
   ```

   Any consumer whose API carries a secret in the query string leaks it to the log sink whenever a
   request throws. `Fog` has exactly that shape: `GET api/lokr/search?first=…&second=…&third=…`
   carries a live access-code passphrase. The consumer removed every one of its own log statements
   that echoed those words (its AC10), but cannot remove this one. CWE-532 (Insertion of Sensitive
   Information into Log File).

All three changes are **null-default / log-content-only and change nothing a consumer compiles
against**, matching the style already established by `OnLogEvent`, `OnWebApplicationStarted`,
`ConfigureLogging` and `ConfigureMiddleware` (see `src/task/done/logging/`, which added the last two).

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
  `src/task/done/logging/00-overview.md` set out to remove.
- **Replacing the consumer's `ApplicationRunner`/host bootstrap.** Explicitly out of scope for the
  consumer's phase — restructuring an app's startup to compensate for a missing toolkit hook is the
  problem this task exists to fix.

## Current state that shapes the design (verified against source)

- `ToolkitWebApplicationSettings` (`src/Toolkit.WebServer/ToolkitWebApplicationSettings.cs`) already
  carries optional bootstrap delegates (`OnLogEvent`, `OnWebApplicationStarted`, `ConfigureLogging`,
  `ConfigureMiddleware`) — the new hooks match that established style.
- `ApplicationStartUp.Configure` builds the pipeline in fixed order: `UseForwardedHeaders` →
  optional `UseHttpsRedirection` → optional `UseCors` → `app.Use(CaptureMiddlewareExceptions)` →
  **`ConfigureMiddleware` hook** → `UseFileServer`/static → `UseRouting` → optional
  `UseAuthentication`/`UseAuthorization` → `UseEndpoints(MapControllers)` → SignalR. There is no
  extension point after routing.
- `ApplicationStartUp.ConfigureServices` wraps its whole body in a `try/catch` that swallows the
  exception and logs it if `IToolkitLogger` happens to resolve. A consumer hook invoked **inside**
  that `try` would fail silently — see ADR-2.
- `ToolkitWebApplication.Run` calls `applicationStartUp.ConfigureServices(builder.Services)` before
  `builder.Build()`, so an `IServiceCollection` hook placed there is registered in time. The Autofac
  factory (`ToolkitServiceProviderFactory.CreateBuilder`) does `containerBuilder.Populate(services)`,
  so anything the consumer registers is visible in the Autofac container too.
- `Tests.ToolKit` (assembly `Tests.FatCat.Toolkit`) references `Toolkit.WebServer` and already has
  `WebServer/SignalR/ToolkitWebApplicationSettingsTests.cs` — a real, existing seam for
  settings-level tests. `ApplicationStartUp` itself is `internal sealed`, reads settings from the
  static `ToolkitWebApplication.Settings` (`private set`, assigned only by `Run`), and drives
  framework extension methods — it has no test seam (ADR-5).
- `OneOff` is the runnable sample host (`Program.RunServer` → `Old/ServerWorker` builds the settings
  and calls `ToolkitWebApplication.Run`); its `ServerWorker` already wires the `ConfigureLogging` and
  `ConfigureMiddleware` samples. It is the smoke vehicle for all three phases.
- The solution is **`ToolKit.slnx`** (renamed from `ToolKit.sln` in commit `f84d241`). Build/test
  commands must target the `.slnx`. The overview's original acceptance criteria said `ToolKit.sln` —
  superseded.
- Current source version is **1.0.343**; this work publishes as **1.0.344**. Versioning and publish
  are out of plan scope (ADR-6).

## Acceptance Criteria → Phase Map

| Acceptance criterion | Proven by |
|---|---|
| A consumer can call `services.AddRateLimiter(...)` (or any `IServiceCollection` extension) without editing the toolkit or replacing `ToolkitWebApplication.Run` | Phase 1 (`ConfigureServices` hook + OneOff registers a named fixed-window policy) |
| A consumer can add middleware that observes endpoint metadata — an action carrying `[EnableRateLimiting("name")]` is limited by that named policy | Phase 2 (`ConfigureRoutedMiddleware` hook + OneOff smoke: 4th request in the window returns `429`) |
| `OneOff` demonstrates both hooks (a request over the limit returns `429`) | Phases 1 + 2, wiring committed as living sample documentation |
| Both hooks unset ⇒ byte-for-byte current behaviour; the whole existing suite stays green | Phases 1–2 (null-conditional invocation; existing suite green; OneOff re-run with the sample hooks commented out) |
| A secret in a query string is no longer written to the log sink on the exception path | Phase 3 (`HttpRequestExtensions` unit tests + both `CaptureMiddlewareExceptions` branches) |
| No breaking change for `C:\Code\Fog` or `C:\Code\Apostil` | `consumer-compatibility.md` (both verified) + every phase's Definition of Done |
| `dotnet build` / `dotnet test` clean | Every phase's Definition of Done (`ToolKit.slnx`) |
| Package publishable by the human afterwards | Out of plan scope by design (ADR-6) |

## Phases & Dependency Graph

| Phase | File | Risk | Depends on | Depended on by |
|---|---|---|---|---|
| 1 — `ConfigureServices` host hook | `01-configure-services-hook.md` | **Medium** — new public NuGet API surface; touches service registration every consumer runs. Mitigated: additive, null-default, invoked outside the swallowing `try/catch`. | — | 2 (OneOff's policy registration) |
| 2 — `ConfigureRoutedMiddleware` post-routing hook | `02-configure-routed-middleware-hook.md` | **Medium** — new public NuGet API surface; inserts an invocation into the pipeline composition every consumer runs. Mitigated: additive, null-default, existing `ConfigureMiddleware` position untouched. | 1 | — |
| 3 — Drop the query string from exception-path request logging | `03-redact-request-url-logging.md` | **Low** — log-content change only; no API change, nothing to compile against. Mitigated: no consumer parses these lines (verified in `consumer-compatibility.md`). | — | — |

Phase 3 is independent of 1 and 2 and could run first; it is ordered last so the two hooks (the
motivating, consumer-blocking work) land first and could be published alone if phase 3 stalls.
Revert cascade: phase 3 reverts alone; phase 2 reverts alone; reverting phase 1 requires reverting
phase 2's OneOff wiring first (its sample uses phase 1's hook).

## Orchestrator

`orchestrator.md` (this folder) is the runbook. Usage: open a Claude session in
`C:\Code\FatCat.Toolkit` and say **"run rate-limiting-hook"** — the session reads the runbook and
drives the phases in order, each in a **fresh, isolated context** (one subagent per phase; the phase
file is the entire handoff), verifying exactly one new commit and a clean working tree after each
phase, halting on failure. It never squashes, amends, rebases, pushes, or publishes.

## Consumer compatibility

`consumer-compatibility.md` records the verification for both consuming repositories — what each
builds against, what it touches in every changed file, and why each phase is non-breaking.

| Repo | Version | Verdict |
|---|---|---|
| `C:\Code\Fog` | 1.0.339 | Safe. Uses no existing hook. Phase 3 closes a live passphrase leak into its Mongo log sink. Three releases behind, so its 339 → 344 upgrade deserves its own build-and-smoke pass — a Fog-side task, not part of this plan. |
| `C:\Code\Apostil` | 1.0.343 | Safe. The **only** consumer using `ConfigureMiddleware`/`ConfigureLogging`, and its shipped `RequestLoggingMiddleware` depends on the documented pre-routing position — which is why ADR-1 freezes it. Clean single-release bump; no code change required. |

**No phase in this plan is a breaking change for either repository, and no plan amendment is
required.**

## Publish flow (human-owned, after the plan completes)

1. Review the three commits on `rate-limiting-hook`; merge to `main` (your call how — the plan never
   pushes or merges).
2. From `src/`, run `PushNugetPackages.ps1` — `Submit-NugetPackage` steps each project's version
   (next: 1.0.344), commits the step, and pushes both packages.
3. Downstream: `Fog` bumps `FatCat.Toolkit.WebServer` to 1.0.344 and unblocks
   `tasks/todo/email_opt_in/07-passphrase-endpoint-hardening.md` — it adds
   `ConfigureServices = services => services.AddRateLimiter(...)` and
   `ConfigureRoutedMiddleware = app => app.UseRateLimiter()` to
   `Common/Common.WebServer/Infrastructure/ApplicationRunner.cs`, and puts
   `[EnableRateLimiting("…")]` on its two anonymous passphrase endpoints. Ask for that update once
   the package is published. `Apostil` (already on 1.0.343) needs nothing — a version bump alone
   picks up the safer exception logging.

## Decisions (lightweight ADRs)

### ADR-1 — A second, later middleware hook rather than moving the existing one
**Decision:** add `public Action<IApplicationBuilder> ConfigureRoutedMiddleware { get; set; }`,
invoked immediately after `app.UseRouting()` and before the authentication/authorization block.
`ConfigureMiddleware` keeps its current position exactly.
**Context:** `UseRateLimiter()` is documented to sit after `UseRouting` and before
`UseAuthorization`, so that position serves the motivating case and is a sensible general
"post-routing" seam — middleware there sees `HttpContext.GetEndpoint()` and therefore endpoint
metadata. Moving the existing hook would be a silent behaviour change for every current consumer
(their middleware would stop seeing static-file and unmatched-route 404 traffic) and would break the
contract `src/task/done/logging/00-overview.md` ADR-2 published one release ago. This is not
hypothetical: Apostil (1.0.343) registers `RequestLoggingMiddleware` through that hook and its own
phase file quotes the position as "the contract this middleware relies on", with a 404-logging smoke
assertion that only holds while the hook stays upstream of routing.
**Alternatives rejected:** parameterising the existing hook with a stage/position enum (invents an
abstraction the toolkit does not have, and complicates a contract consumers already depend on);
moving the existing hook (breaking change, see above); a hook after `UseAuthorization` as well
(YAGNI — add it when a real consumer needs it).

### ADR-2 — `ConfigureServices` is invoked outside `ApplicationStartUp.ConfigureServices`'s `try/catch`
**Decision:** invoke `ToolkitWebApplication.Settings.ConfigureServices?.Invoke(services)` as the last
statement of the method, **after** the existing `try/catch` block closes.
**Context:** the existing `catch` swallows the exception and only logs if `IToolkitLogger` resolves —
which, this early in startup, it may not. A consumer delegate that throws (a bad limiter option, a
missing configuration value) must fail startup loudly; a silently half-registered container is worse
than a crash. This matches `src/task/done/logging/` phase 1, which deliberately did not wrap the
`ConfigureMiddleware` invocation in a `try/catch`.
**Alternatives rejected:** invoking inside the `try` (failures vanish); invoking from
`ToolkitWebApplication.Run` after the `ConfigureServices` call (equivalent behaviour, but splits the
hook away from the registrations it extends).

### ADR-3 — The hook takes `IServiceCollection`, not `WebApplicationBuilder`
**Decision:** `public Action<IServiceCollection> ConfigureServices { get; set; }`.
**Context:** it is the narrowest type that satisfies the requirement — every
`services.AddXxx(...)` extension works. Consumers that need configuration build it themselves before
constructing the settings (Fog already does exactly that in `ApplicationRunner.Run`) and capture it
in the closure.
**Alternatives rejected:** `Action<WebApplicationBuilder>` (hands the consumer the entire host —
`Host`, `Configuration`, `WebHost`, `Environment` — a much larger surface to keep compatible, and it
invites consumers to reconfigure the service-provider factory out from under the toolkit);
`Action<IServiceCollection, IConfiguration>` (a second parameter nobody has asked for; add it later
without breaking anyone if a consumer needs it).

### ADR-4 — Exception-path logging drops the query string rather than gaining a formatting hook
**Decision:** add `HttpRequest.DisplayPath()` to the existing
`src/Toolkit.WebServer/HttpRequestExtensions.cs`, returning `$"{request.PathBase}{request.Path}"`,
and use it on both `catch` branches in `CaptureMiddlewareExceptions` in place of
`GetDisplayUrl()`.
**Context:** the leak is a defect, not a preference — a hook defaulting to today's behaviour would
leave every consumer leaking until it opts in, which inverts the safe default. The path alone
identifies the failing endpoint, which is what these two lines exist to say. `PathBase` is included
so the line stays complete when a consumer sets `Settings.BasePath`; with no base path it is exactly
`Request.Path`.
**Alternatives rejected:** a `Func<HttpRequest, string>` formatting hook (unsafe default, more
public surface); redacting query **values** while keeping keys (more code, still a judgement call
about which keys are secret, and the keys themselves — `first`, `second`, `third` — carry no
diagnostic value here); keeping scheme + host as well (recoverable from the sink's own metadata;
drop it now, and it is trivially re-added if operations ask).

### ADR-5 — Verification is settings-level unit tests + `HttpRequestExtensions` unit tests + OneOff smoke
**Decision:** unit-test what has a real seam — the new settings properties (defaults, and that a set
delegate is the one invoked) in the existing
`Tests.ToolKit/WebServer/SignalR/ToolkitWebApplicationSettingsTests.cs` style, and the new
`DisplayPath` extension against a `DefaultHttpContext`. Prove the wiring itself with the `OneOff`
sample host and keep that wiring committed as living documentation.
**Context:** `ApplicationStartUp` is `internal sealed`, has no `InternalsVisibleTo`, reads
`ToolkitWebApplication.Settings` (a static with a `private set` assigned only by `Run`), and drives
framework extension methods (`UseRouting`, `GetAutofacRoot`). Faking `IApplicationBuilder` deep
enough to survive that tests the framework, not the change. This is the same call `logging` ADR-4
made, and the repo's own precedent — the bootstrap classes have zero tests today.
**Alternatives rejected:** refactoring the bootstrap for testability (scope creep in a published
package for three small changes); adding `InternalsVisibleTo` and testing `ConfigureServices`
directly (the static `Settings` cannot be set from a test, so the test would assert nothing);
a mock-lattice test of framework plumbing dressed up as TDD.

### ADR-6 — No version-step or publish phase
**Decision:** the plan ends with three code commits on the task branch. Stepping to 1.0.344 and
pushing to NuGet is the human running `PushNugetPackages.ps1` after merge.
**Context:** `Submit-NugetPackage` already owns version stepping (`Step-ProjectVersion` — the
repo's "Stepped project to version X" commits are its output). A plan phase editing `VersionPrefix`
by hand would double-step. Same doctrine as `logging` ADR-5.

### ADR-7 — Names: `ConfigureServices` and `ConfigureRoutedMiddleware`
**Decision:** `ConfigureServices` matches ASP.NET's own vocabulary and the method it extends;
`ConfigureRoutedMiddleware` says what distinguishes it from `ConfigureMiddleware` — it runs where
routing has already matched.
**Alternatives rejected:** `ConfigureEndpointMiddleware` (reads as "middleware on the endpoint",
which is what a filter is); `ConfigurePostRoutingMiddleware` (accurate, clumsy);
`AddServices`/`AddMiddleware` (implies a registry — see `logging` ADR-1).

## Assumptions

- The rules in `src/.claude/rules/csharp/*` govern this repo's C# (TDD, xUnit + FakeItEasy +
  FatCat.Testing with `.Not.` negation, CSharpier owns formatting, warnings are errors, block bodies
  only, no expression-bodied members anywhere including tests). `src/CLAUDE.md`'s "Haivision /
  Command 360" header text is a stale copy — the rule files are the operative standards.
- Build/test entry points from `src/`: `dotnet build ToolKit.slnx` and `dotnet test ToolKit.slnx`.
- `Microsoft.AspNetCore.RateLimiting` ships in the ASP.NET Core shared framework (`net10.0`,
  `FrameworkReference Microsoft.AspNetCore.App`) — **no new package reference** in the toolkit or in
  `OneOff`. If `OneOff` cannot see `AddRateLimiter`/`EnableRateLimiting`, add
  `<FrameworkReference Include="Microsoft.AspNetCore.App" />` to `OneOff.csproj` (sample project
  only — never add a package reference to `Toolkit.WebServer` for this).
- The task branch is `rate-limiting-hook` (already checked out). The commit policy forbids working
  on `main`.
- `OneOff` is the smoke host. Its `ServerWorker` points `TlsCertificate` at
  `C:\DevelopmentCert\DevelopmentCert.pfx`; if that file is absent on the machine and `OneOff` will
  not start, use `SampleDocker/Program.cs` as the fallback smoke host and **record the substitution
  in the phase report** — never report a smoke result that was not observed.

## Open Questions

None blocking. Flagged for the human reviewer:

- **Pre-existing pipeline oddity, deliberately not fixed here.** `ToolkitWebApplication.Run` calls
  `app.UseCors(...)`, `app.UsePathBase(...)` and `app.MapControllers()` *after*
  `applicationStartUp.Configure` has already run `UseEndpoints(...)`. Middleware registered after the
  endpoint middleware does not execute for matched endpoints, so those two `Use` calls are at best
  inert for controller traffic. Phase 2 must not "fix" this — it is a separate work item with its own
  consumer-impact analysis.
- **`EqualObject` equality:** `ToolkitWebApplicationSettings : EqualObject` — new delegate properties
  may participate in equality comparison. No known consumer compares settings instances; if a phase
  sees equality-related test fallout, log it and handle per `EqualObject`'s conventions.
- **Fog's version gap (339 → 344)** spans the `logging` hooks (340–342), the SignalR hub improvements
  (343) and this work. Non-breaking on paper (see `consumer-compatibility.md`), but the upgrade
  deserves its own build-and-smoke pass in Fog rather than being assumed. Apostil is on 343 and bumps
  cleanly.
