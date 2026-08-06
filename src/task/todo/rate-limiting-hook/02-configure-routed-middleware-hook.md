# Phase 2 — `ConfigureRoutedMiddleware` Post-Routing Hook

- **Work item:** rate-limiting-hook (see `src/task/todo/rate-limiting-hook/00-overview.md`)
- **Depends on:** Phase 1 (`01-configure-services-hook.md`) — its OneOff sample registers the
  `"oneoff-fixed"` policy this phase enables
- **Depended on by:** —
- **Risk:** **Medium** — adds public API surface to a published NuGet package
  (`FatCat.Toolkit.WebServer`) and inserts an invocation into the request-pipeline composition every
  consumer app runs. Mitigations: the property defaults to null and the invocation is
  null-conditional, so unset = byte-for-byte current behaviour; the existing `ConfigureMiddleware`
  hook does **not** move (overview ADR-1); the full existing test suite must stay green.

## Context (complete handoff — read before coding)

Read `src/CLAUDE.md` with all `src/.claude/rules/csharp/*.md` first — mandatory (TDD, xUnit +
FakeItEasy + FatCat.Testing with `.Not.` negation, CSharpier owns formatting, warnings are errors,
**block bodies only — no expression-bodied members anywhere, including tests**). Ignore the stale
"Haivision" header text in `CLAUDE.md` — the rule files are the operative standards.

Current state you will find:

- `src/Toolkit.WebServer/ToolkitWebApplicationSettings.cs` — carries `ConfigureLogging`,
  `ConfigureMiddleware` and (from Phase 1) `ConfigureServices`. The new property joins them.
- `src/Toolkit.WebServer/ApplicationStartUp.cs` — `internal sealed`. `Configure` builds the pipeline
  in this fixed order:

  ```
  UseForwardedHeaders
  → optional UseHttpsRedirection
  → optional UseCors
  → app.Use(CaptureMiddlewareExceptions)
  → Settings.ConfigureMiddleware?.Invoke(app)      <-- existing hook, DO NOT MOVE
  → UseFileServer + SetUpStaticFiles
  → UseRouting
  → optional UseAuthentication / UseAuthorization
  → UseEndpoints(MapControllers)
  → SystemScope.Container.LifetimeScope = ...
  → SetUpSignalR
  ```

- **Why a second hook exists at all:** ASP.NET Core's `RateLimitingMiddleware` reads its policy from
  `HttpContext.GetEndpoint()`, which is `null` until `UseRouting` has run. Anything added through the
  existing `ConfigureMiddleware` hook is upstream of routing and therefore cannot see
  `[EnableRateLimiting("name")]`. Moving the existing hook would silently change behaviour for every
  current consumer (their middleware would stop seeing static-file and unmatched-route 404 traffic)
  and break the contract published in `src/task/done/logging/00-overview.md` ADR-2. Hence: a second,
  later hook.
- **A real consumer depends on that position right now.** `C:\Code\Apostil` (on 1.0.343) registers
  `RequestLoggingMiddleware` through `ConfigureMiddleware` and its own phase file
  (`Apostil/tasks/done/api_logging/02-request-logging.md`) quotes the pre-routing position as "the
  contract this middleware relies on", with a smoke assertion that a 404 on a nonsense route still
  produces a completion log line. Moving the hook breaks that. **Do not move it.**
- `Tests.ToolKit/WebServer/SignalR/ToolkitWebApplicationSettingsTests.cs` is the working example of
  testing this settings class directly — match its style. `ApplicationStartUp` has no test seam
  (`internal sealed`, no `InternalsVisibleTo`, reads the static `ToolkitWebApplication.Settings`
  whose setter is `private`) — do not try to test it (overview ADR-5).
- `OneOff` is the smoke host: `Program.RunServer` → `Old/ServerWorker.DoWork`. `OneOff` endpoints
  derive from `FatCat.Toolkit.WebServer.Endpoint` (a `Controller`), e.g. `OneOff/BadRequestEndpoint.cs`
  with `[HttpGet("request/good")]`.
- **Pre-existing oddity — do not fix it here:** `ToolkitWebApplication.Run` calls `app.UseCors(...)`,
  `app.UsePathBase(...)` and `app.MapControllers()` *after* `Configure` already ran `UseEndpoints`.
  It is noted in the overview's Open Questions as a separate work item. Touching it in this phase is
  out of scope.
- The solution is **`ToolKit.slnx`**, not `ToolKit.sln`.

## Design (build exactly this shape)

**`ToolkitWebApplicationSettings`** gains, immediately after `ConfigureMiddleware` (and before or
after `ConfigureServices` — keep the block of `Configure*` delegates together):

```csharp
public Action<IApplicationBuilder> ConfigureRoutedMiddleware { get; set; }
```

**`ApplicationStartUp.Configure`** — invoke the hook immediately after `app.UseRouting()` and before
the authentication/authorization block:

```csharp
app.UseRouting();

ToolkitWebApplication.Settings.ConfigureRoutedMiddleware?.Invoke(app);

if (ToolkitWebApplication.IsOptionSet(WebApplicationOptions.Authentication))
{
	app.UseAuthentication();
	app.UseAuthorization();
}
```

Notes that matter:

- **Position is the contract** (overview ADR-1): after routing (middleware here sees
  `HttpContext.GetEndpoint()` and its metadata), before authentication/authorization (where
  `UseRateLimiter()` is documented to sit), before `UseEndpoints`. Do not move it, and **do not move
  the existing `ConfigureMiddleware` invocation**.
- Null-conditional invoke only — no flag, no option enum. Unset means an untouched pipeline.
- Do not wrap the invocation in try/catch: a consumer delegate that throws at startup should fail
  startup loudly.
- Note the asymmetry with `Endpoint`-derived controllers: nothing about this hook is MVC-specific —
  any endpoint-metadata-aware middleware (rate limiting, output caching, endpoint-aware logging)
  belongs here.

**`OneOff` sample wiring (committed — living documentation, ADR-5).** Two edits:

1. In `OneOff/Old/ServerWorker.cs`, alongside the existing `ConfigureMiddleware` sample:

   ```csharp
   ConfigureRoutedMiddleware = applicationBuilder =>
   {
       applicationBuilder.UseRateLimiter();
   },
   ```

2. A new endpoint file `OneOff/RateLimitedEndpoint.cs`, matching `BadRequestEndpoint`'s shape:

   ```csharp
   public class RateLimitedEndpoint : Endpoint
   {
       [HttpGet("request/limited")]
       [EnableRateLimiting("oneoff-fixed")]
       public WebResult ReturnLimitedRequest()
       {
           return Ok("this-request-was-allowed");
       }
   }
   ```

   `EnableRateLimiting` is `Microsoft.AspNetCore.RateLimiting`; `UseRateLimiter` is
   `Microsoft.AspNetCore.Builder`. Both ship in the ASP.NET Core shared framework — **no new package
   reference**. The policy name must match Phase 1's registration verbatim (`"oneoff-fixed"`:
   3 permits per 10-second window, `QueueLimit = 0`, rejection status `429`).

## Steps

1. **Write the failing tests first** in `Tests.ToolKit`, in the same place Phase 1 put its settings
   tests. Cover, one assertion per fact:
   - `ConfigureRoutedMiddleware` defaults to null on a new settings instance.
   - A delegate assigned to `ConfigureRoutedMiddleware` receives the exact `IApplicationBuilder` it
     is invoked with (use `A.Fake<IApplicationBuilder>()` and capture the argument).
   - `ConfigureMiddleware` and `ConfigureRoutedMiddleware` are independent — setting one leaves the
     other null.
   Observe red before green.
2. **Implement** the settings property and the `Configure` invocation.
3. **Wire the OneOff sample** — settings delegate plus the new endpoint.
4. **Build + full test suite** from `src/`: `dotnet build ToolKit.slnx` (zero warnings — they are
   errors) and `dotnet test ToolKit.slnx` (everything green).
5. **Smoke-check with OneOff** (from `src/OneOff`): `dotnet run`. This is the acceptance evidence for
   the whole work item — capture the actual output.
   - `curl -i http://localhost:<port>/api/request/limited` four times inside 10 seconds → the first
     three return `200` with `this-request-was-allowed`, the fourth returns **`429`**. (Find the
     port/route prefix by reading `ServerWorker` and the endpoints; do not guess. If the routes are
     not under `api/`, use whatever the existing endpoints actually use.)
   - Wait out the window, call once more → `200` again (proves the fixed window resets, i.e. a real
     named policy rather than a one-shot).
   - `curl` `request/good` (no `[EnableRateLimiting]`) more than three times in the same window →
     every response `200`. This is the proof that the **named policy bound to endpoint metadata**,
     which is the entire point of the phase.
   - `curl` a nonsense path → still a `404`, unthrottled; the existing `ConfigureMiddleware` hook
     line still prints for it (proves the pre-routing hook did not move).
   - Temporarily comment the sample `ConfigureRoutedMiddleware` out, run again: `request/limited`
     is no longer throttled and nothing else changes. Restore it.
   - If `OneOff` will not start because `C:\DevelopmentCert\DevelopmentCert.pfx` is missing, fall
     back to `SampleDocker` (add the equivalent sample endpoint there) and **record the substitution
     in the phase report**. Never report a smoke result you did not observe.
6. **Formatting/style:** run the repo's standard passes per `src/.claude/rules/csharp/toolchain.md`,
   then build again.

## Definition of Done (all mandatory)

- [ ] TDD honored for the settings-level tests (red observed before green); the pipeline invocation
      is covered by the OneOff smoke per ADR-5, and the phase report says so explicitly
- [ ] `dotnet build ToolKit.slnx` — zero warnings (warnings are errors)
- [ ] `dotnet test ToolKit.slnx` — entire existing suite plus the new tests green
- [ ] The existing `ConfigureMiddleware` invocation is byte-for-byte where it was (verify in the
      diff — this is a hard requirement, see overview ADR-1)
- [ ] Formatting/style passes run; build re-run so CSharpier applies
- [ ] No banned patterns (block bodies only, no expression-bodied members, naming rules per
      `src/.claude/rules/csharp/`)
- [ ] No new `PackageReference` in `Toolkit.WebServer` (framework APIs only)
- [ ] Smoke checks observed and recorded verbatim: `429` on the 4th call, `200` after the window
      resets, unattributed endpoint unthrottled, 404 path still hits the pre-routing hook, and
      unchanged behaviour with the hook unset
- [ ] Review pass: run `unit-test-review` → `code-review` → `code-security-review` if those skills
      are available in the session (restarting the loop after any fix); otherwise perform and
      document a manual review covering the same concerns
- [ ] Exactly one commit on branch `rate-limiting-hook`, message referencing this file; **no push**

Suggested commit message:

```
rate-limiting-hook phase 2: ConfigureRoutedMiddleware post-routing hook (src/task/todo/rate-limiting-hook/02-configure-routed-middleware-hook.md)
```

## Rollback Procedure

- `git revert <phase-2-commit>` — Phase 1 does not depend on it, so no cascade. Phase 1's OneOff
  policy registration becomes inert again, which is exactly its Phase 1 state.
- No data/config steps. If the package was already published with the hook and consumers adopted it,
  reverting is a breaking change — roll **forward** instead.

## Phase Report (produce before finishing)

Files added/changed/deleted; test counts (new/total/passing); the exact smoke output (status codes
per call, in order) and the port/routes used; whether `OneOff` or the `SampleDocker` fallback was the
smoke host; deviation log; open questions or risks for the human reviewer — in particular, anything
observed about the `Run`-level `UseCors`/`UsePathBase`/`MapControllers` ordering noted in the
overview's Open Questions (**observe and report only — do not change it**).

## Hand-off

- **Public contract (ships in 1.0.344):**
  `ToolkitWebApplicationSettings.ConfigureRoutedMiddleware : Action<IApplicationBuilder>` — null by
  default; when set, invoked exactly once during startup, positioned **after** `UseRouting` and
  **before** authentication/authorization and `UseEndpoints`. Middleware added here observes
  `HttpContext.GetEndpoint()` and its metadata, so named policies and endpoint attributes bind.
  Traffic it does **not** see: nothing — it is still upstream of the endpoint middleware — but it is
  downstream of static files, so requests served by `UseFileServer`/`UseStaticFiles` terminate before
  reaching it. Consumers needing to see static traffic keep using `ConfigureMiddleware`.
- **First external consumer:** `Fog`'s `Common/Common.WebServer/Infrastructure/ApplicationRunner.cs`
  (`tasks/todo/email_opt_in/07-passphrase-endpoint-hardening.md`) — `ConfigureRoutedMiddleware =
  app => app.UseRateLimiter()` plus `[EnableRateLimiting("…")]` on its two anonymous passphrase
  endpoints, once 1.0.344 is published.
