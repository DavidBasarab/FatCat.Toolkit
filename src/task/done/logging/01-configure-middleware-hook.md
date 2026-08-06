# Phase 1 — `ConfigureMiddleware` Pipeline Hook

- **Work item:** logging (see `src/task/logging/00-overview.md`)
- **Depends on:** — (first phase)
- **Depended on by:** — (Phase 2 is independent; executed after this one for linear history)
- **Risk:** **Medium** — adds public API surface to a published NuGet package
  (`FatCat.Toolkit.WebServer`) and inserts an invocation into the request-pipeline composition
  that every consumer app runs. Mitigations: the property defaults to null and the invocation is
  null-conditional, so unset = byte-for-byte current behavior; the insertion point is fixed and
  documented (overview ADR-2); the full existing test suite must stay green.

## Context (complete handoff — read before coding)

Read `src/CLAUDE.md` with all `src/.claude/rules/csharp/*.md` first — mandatory (TDD, xUnit +
FakeItEasy + FatCat.Testing with `.Not.` negation, CSharpier owns formatting, warnings are
errors, block bodies only). Ignore the stale "Haivision" header text in `CLAUDE.md` — the rule
files are the operative standards.

Current state you will find:

- `src/Toolkit.WebServer/ToolkitWebApplicationSettings.cs` — the settings POCO
  (`: EqualObject`). It already carries optional bootstrap delegates (`Action<string>
  OnLogEvent`, `Action OnWebApplicationStarted`) — the new property matches that style.
- `src/Toolkit.WebServer/ApplicationStartUp.cs` — `internal sealed`; its `Configure` method
  builds the pipeline in fixed order:
  `UseForwardedHeaders` → optional `UseHttpsRedirection` → optional `UseCors` →
  `app.Use(CaptureMiddlewareExceptions)` → `UseFileServer` / `SetUpStaticFiles` → `UseRouting`
  → optional auth → `UseEndpoints` → SignalR. There is no extension point in the chain.
- Settings are reached statically via `ToolkitWebApplication.Settings` throughout the class —
  follow that existing pattern.
- `ApplicationStartUp` and `ToolkitWebApplication` have **no existing unit tests** (bootstrap
  infrastructure — overview ADR-4). `Tests.ToolKit` references `Toolkit.WebServer` if a test
  seam is found.
- `OneOff` is the runnable sample host: `Program.RunServer` → `Old/ServerWorker` constructs
  the `ToolkitWebApplicationSettings` and calls `ToolkitWebApplication.Run`. This is the smoke
  vehicle.

## Design (build exactly this shape)

**`ToolkitWebApplicationSettings`** gains (alongside the existing delegates; keep the class's
member ordering conventions):

```csharp
public Action<IApplicationBuilder> ConfigureMiddleware { get; set; }
```

(`IApplicationBuilder` is `Microsoft.AspNetCore.Builder` — the project already references the
ASP.NET framework; add the using if not present.)

**`ApplicationStartUp.Configure`** — invoke the hook immediately after
`app.Use(CaptureMiddlewareExceptions)` and before the static-files block (ADR-2):

```csharp
app.Use(CaptureMiddlewareExceptions);

ToolkitWebApplication.Settings.ConfigureMiddleware?.Invoke(app);

// Static files/file server typically early
app.UseFileServer();
```

Notes that matter:

- **Position is the contract** (ADR-2): after forwarded headers (consumer middleware sees
  proxy-corrected scheme/host), inside the toolkit's exception capture (consumer middleware
  that throws is still logged), before static files and routing (consumer middleware sees ALL
  downstream traffic — static, unmatched-route 404s, endpoints, the SignalR path). Do not move
  it.
- Null-conditional invoke only — no flag, no option enum. Unset means untouched pipeline.
- Do not wrap the invocation in try/catch: a consumer delegate that throws at startup should
  fail startup loudly (a silently half-configured pipeline is worse).

**`OneOff` sample wiring (committed — living documentation, ADR-4):** in
`OneOff/Old/ServerWorker.cs`, at the `ToolkitWebApplicationSettings` construction, add:

```csharp
ConfigureMiddleware = applicationBuilder =>
	applicationBuilder.Use(
		async (context, next) =>
		{
			ConsoleLog.WriteCyan($"ConfigureMiddleware hook: {context.Request.Method} {context.Request.Path}");

			await next();
		}
	),
```

(Adapt to the file's actual construction shape and using directives; `ConsoleLog` is already
the OneOff idiom. Match the surrounding style — this is sample code, keep it minimal.)

## Steps

1. **Test seam check (TDD honesty — before implementation):** look for a clean way to
   red-green the hook invocation. The known obstacles: `ApplicationStartUp` is
   `internal sealed` (check for `InternalsVisibleTo` to `Tests.FatCat.Toolkit` — none is
   expected), `Configure` drives framework extension methods (`UseRouting`,
   `GetAutofacRoot`) that defeat interface fakes, and settings are static
   (`ToolkitWebApplication.Settings`). If a genuine seam exists, write the failing test first
   (in `Tests.ToolKit`, following the repo's `*Specs`/verb-first conventions), then implement.
   If not — per overview ADR-4 — proceed implementation-first and record the TDD deviation in
   the phase report citing ADR-4. Do **not** write a mock-lattice test that asserts framework
   plumbing.
2. **Implement** the settings property and the `Configure` invocation.
3. **Wire the OneOff sample** as above.
4. **Build + full test suite:** from `src/`: `dotnet build ToolKit.sln` (zero warnings — they
   are errors) and `dotnet test ToolKit.sln` (everything green; the unchanged suite passing is
   the "unset hook changes nothing" regression check).
5. **Smoke-check with OneOff** (from `src/OneOff`): `dotnet run` —
   - Startup succeeds exactly as before.
   - `curl http://localhost:<OneOff port>/api/<an existing OneOff endpoint>` → the cyan
     `ConfigureMiddleware hook: GET /api/...` line prints per request. (Find the port/route in
     `ServerWorker` / the OneOff endpoints, e.g. `BadRequestEndpoint`; do not guess — read
     them.)
   - `curl` a nonsense path → the hook line prints for the 404 too (proves the
     before-routing/static position).
   - Temporarily comment the sample `ConfigureMiddleware` out, run again, confirm behavior is
     exactly current-package behavior (no hook line, nothing else changed), then restore it.
6. **Formatting/style:** run the repo's standard passes per
   `src/.claude/rules/csharp/toolchain.md` (CSharpier + `dotnet format` style/analyzers as that
   file prescribes), then build again.

## Definition of Done (all mandatory)

- [ ] TDD honored where a genuine seam exists; any deviation recorded with the ADR-4 rationale
      (red observed before green for whatever tests were written)
- [ ] `dotnet build ToolKit.sln` — zero warnings (warnings are errors)
- [ ] `dotnet test ToolKit.sln` — entire existing suite plus any new tests green
- [ ] Formatting/style passes per the repo's toolchain rules run; build re-run so CSharpier
      applies
- [ ] No banned patterns (block bodies only, no expression-bodied members, naming rules per
      `src/.claude/rules/csharp/`)
- [ ] Smoke checks observed: hook line per request **including a 404**, and unchanged behavior
      with the hook unset
- [ ] Review pass: run `unit-test-review` → `code-review` → `code-security-review` if those
      skills are available in the session (restarting the loop after any fix); otherwise
      perform and document a manual review covering the same concerns (behavior coverage,
      standards conformance, new-public-surface security)
- [ ] Exactly one commit on branch `task/logging`, message referencing this file; **no push**

Suggested commit message:

```
logging phase 1: ConfigureMiddleware pipeline hook on ToolkitWebApplicationSettings (src/task/logging/01-configure-middleware-hook.md)
```

## Rollback Procedure

- `git revert <phase-1-commit>` (Phase 2 does not depend on it; no cascade).
- No data/config steps. If the package was already published with the hook and consumers
  adopted it, reverting is a breaking change — at that point roll **forward** instead (the
  publish gate is the human's; pre-publish, revert freely).

## Phase Report (produce before finishing)

Files added/changed/deleted; test counts (new/total/passing); deviation log (**most
importantly**: whether a real test seam was found or the ADR-4 implementation-first path was
taken, and the exact OneOff construction site/port/route used); open questions or risks for the
human reviewer.

## Hand-off

- **Public contract (ships in the next package version):**
  `ToolkitWebApplicationSettings.ConfigureMiddleware : Action<IApplicationBuilder>` — null by
  default; when set, invoked exactly once during startup, positioned after forwarded-headers
  processing and the toolkit's exception-capture middleware, before static files and routing.
  Consumer middleware added here sees all downstream traffic (static, 404s, endpoints, SignalR
  path).
- **First consumer:** Apostil `api_logging` phase 2 (`RequestLoggingMiddleware` — replaces its
  `IStartupFilter` design once the package is published).
