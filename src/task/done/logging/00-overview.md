# logging (FatCat.Toolkit) — Overview

## Work Item

Add two opt-in host hooks to `FatCat.Toolkit.WebServer` so consuming applications (first
consumer: Apostil's `api_logging` work item) can participate in the request pipeline and the
logging bootstrap without relying on incidental framework behavior:

1. **`ConfigureMiddleware`** — a delegate on `ToolkitWebApplicationSettings` invoked at a
   deliberate, documented point in the pipeline, so an app can add middleware (e.g. request
   logging) without the `IStartupFilter`-through-Autofac trick.
2. **`ConfigureLogging`** — a delegate on `ToolkitWebApplicationSettings` invoked inside the
   host's `AddLogging` call after `ClearProviders()`, so an app can route framework logs
   (Kestrel, routing, startup errors) into its own sinks (e.g. Serilog) instead of losing them.

Both hooks are **null by default and change nothing when unset** — every existing consumer
(Fog, SampleDocker, OneOff, Apostil at 1.0.341 behavior) is unaffected until it opts in.

Current state that shapes the design (verified against the source):

- `ToolkitWebApplicationSettings` (`src/Toolkit.WebServer/ToolkitWebApplicationSettings.cs`)
  already carries optional delegates (`OnLogEvent`, `OnWebApplicationStarted`) — the new hooks
  match that existing style.
- `ApplicationStartUp.Configure` (`src/Toolkit.WebServer/ApplicationStartUp.cs`) builds the
  pipeline in fixed order: forwarded headers → https redirect → CORS →
  `CaptureMiddlewareExceptions` → file server/static → routing → auth → endpoints. There is no
  extension point anywhere in that chain today.
- `ApplicationStartUp.ConfigureServices` calls
  `services.AddLogging(options => { options.ClearProviders(); })` — framework logs are
  discarded unconditionally, with no way for a consumer to add a provider.
- `Tests.ToolKit` (assembly `Tests.FatCat.Toolkit`) references **both** `ToolKit` and
  `Toolkit.WebServer`, so any new unit tests live there. `ApplicationStartUp` and
  `ToolkitWebApplication` have **no existing tests** (bootstrap infrastructure; note
  `ApplicationStartUp` is `internal sealed`).
- `OneOff` is the runnable sample host (`Program.RunServer` → `Old/ServerWorker` builds
  `ToolkitWebApplicationSettings` and calls `ToolkitWebApplication.Run`) — the smoke-check
  vehicle for both phases.
- Versioning + publish are **outside this plan**: `PushNugetPackages.ps1` runs
  `Submit-NugetPackage` per project, which itself does `Step-ProjectVersion` (creates the
  "Stepped project to version X" commit), cleans, and pushes to NuGet. The human runs it after
  merging; this plan ends with code committed on the task branch.

## Acceptance Criteria → Phase Map

| Acceptance criterion | Proven by |
|---|---|
| A consumer can add middleware via `ToolkitWebApplicationSettings.ConfigureMiddleware`, running after forwarded-headers/exception-capture and before static files/routing | Phase 1 (implementation + OneOff smoke: hook line printed per request, including a 404/static request) |
| A consumer can add logging providers via `ToolkitWebApplicationSettings.ConfigureLogging`, invoked after `ClearProviders()` | Phase 2 (implementation + OneOff smoke: delegate observed running at startup) |
| Unset hooks change nothing | Phases 1–2 (null-conditional invocation; full existing test suite stays green; OneOff runs unchanged with hooks removed) |
| Everything builds warning-free and all tests pass | Every phase's Definition of Done (`dotnet build` / `dotnet test` on `ToolKit.sln`) |
| Package publishable by the human afterwards | Out of plan scope by design (see "Publish flow" below) |

## Phases & Dependency Graph

| Phase | File | Risk | Depends on | Depended on by |
|---|---|---|---|---|
| 1 — `ConfigureMiddleware` pipeline hook | `01-configure-middleware-hook.md` | **Medium** (adds public NuGet API surface and touches the pipeline composition every consumer runs; mitigated: additive, null-default, position fixed and documented) | — | — |
| 2 — `ConfigureLogging` bootstrap hook | `02-configure-logging-hook.md` | **Medium** (public NuGet API surface; touches the logging bootstrap every consumer runs; mitigated: additive, null-default, `ClearProviders` behavior unchanged when unset) | — | — |

The phases are independent (different methods of `ApplicationStartUp`, different settings
properties) but are executed in order 1 → 2 for a linear history. Revert cascade: either phase
can be reverted alone; revert both to remove the work item entirely.

## Orchestrator

`orchestrator.md` (this folder) is the runbook. Usage: open a Claude session in
`C:\Code\FatCat.Toolkit` and say **"run logging"** (or "run the toolkit logging plan") — the
session reads the runbook and drives the phases in order, each in a **fresh, isolated context**
(one subagent per phase; the phase file is the entire handoff), verifying exactly one new commit
and a clean working tree after each phase, halting on failure. It never squashes, amends,
rebases, or pushes.

## Publish flow (human-owned, after the plan completes)

1. Review the two commits on `task/logging`; merge to `main` (your call how — the plan never
   pushes or merges).
2. From `src/`, run `PushNugetPackages.ps1` — `Submit-NugetPackage` steps each project's
   version (next: 1.0.342), commits the step, and pushes both packages. No version-step phase
   exists in this plan **because the publish script owns stepping** (verified in
   `PersonalPowershell/Coding/Submit-NugetPackage.ps1`).
3. Downstream: Apostil bumps `FatCat.Toolkit.WebServer` to the new version and its
   `api_logging` plan simplifies (phase 2 drops the `IStartupFilter` spike/fallback; phase 1
   can route framework logs through `ConfigureLogging`). That is a separate update to
   `C:\Code\Apostil\tasks\todo\api_logging\` — ask for it once the package is published.

## Decisions (lightweight ADRs)

### ADR-1 — Hooks are `Action<>` delegates on `ToolkitWebApplicationSettings`, not middleware/provider registries
**Decision:** `public Action<IApplicationBuilder> ConfigureMiddleware { get; set; }` and
`public Action<ILoggingBuilder> ConfigureLogging { get; set; }`, both defaulting to null,
invoked null-conditionally by the host.
**Context:** The settings class already carries optional bootstrap delegates (`OnLogEvent`,
`OnWebApplicationStarted`) — this is the established extension style. A delegate hands the
consumer the real builder, so ordering, `UseWhen`, conditional registration, and multiple
middleware all work with zero new toolkit abstractions.
**Alternatives rejected:** a `List<Type>` of middleware types (loses ordering control,
arguments, and conditional composition; invents an abstraction); an interface the consumer
implements and the toolkit discovers (reflection magic for no gain over a delegate); events
(no meaningful multi-subscriber story for pipeline composition).

### ADR-2 — `ConfigureMiddleware` runs after `CaptureMiddlewareExceptions`, before static files and routing
**Decision:** In `ApplicationStartUp.Configure`, invoke the hook immediately after
`app.Use(CaptureMiddlewareExceptions)` and before `app.UseFileServer()`.
**Context:** This position means: forwarded headers are already processed (correct
scheme/host visible), the toolkit's exception capture wraps the consumer's middleware (its
logging still fires if consumer middleware throws), and consumer middleware sees **all**
downstream traffic — static files, unmatched-route 404s, and endpoints. This is exactly what a
request-logging middleware wants.
**Alternatives rejected:** first in the pipeline (runs before forwarded headers — a logger
would see pre-proxy values; nothing needs to be outermost); after routing (misses static and
404 traffic); multiple hooks at multiple positions (YAGNI — one deliberate position; a second
hook is a future decision if a real consumer needs it).

### ADR-3 — `ConfigureLogging` is invoked inside the existing `AddLogging` call, after `ClearProviders()`
**Decision:** `services.AddLogging(options => { options.ClearProviders();
ToolkitWebApplication.Settings.ConfigureLogging?.Invoke(options); })`.
**Context:** Default behavior is byte-for-byte unchanged (providers cleared, nothing added).
A consumer that wants framework logs adds its provider in the delegate (Apostil will bridge to
Serilog). Running after `ClearProviders()` means what the consumer adds is exactly what exists.
**Alternatives rejected:** removing `ClearProviders()` (a behavior change for every existing
consumer — console noise returns everywhere); the toolkit referencing Serilog and wiring it
itself (an opinion the toolkit deliberately doesn't hold; consumers choose their logger);
a boolean `KeepDefaultProviders` flag (less capable than the delegate and would coexist
awkwardly with it).

### ADR-4 — Verification is sample-host smoke plus targeted tests; the bootstrap itself stays untested
**Decision:** Each phase proves its hook with the `OneOff` sample host (delegate wired in
`ServerWorker`'s settings; observable console output) and keeps that wiring committed as living
sample documentation. Unit tests are added only where a real seam exists (none is expected on
`ApplicationStartUp` — it is `internal sealed`, drives framework extension methods, and touches
`SystemScope` static state; it has zero tests today). If the implementing session finds a
clean, honest test seam, it writes the test; otherwise it records the TDD deviation in the
phase report with this ADR as the reason.
**Context:** The repo's own precedent: bootstrap classes (`ApplicationStartUp`,
`ToolkitWebApplication`) are verified by the sample hosts, not unit tests. Faking
`IApplicationBuilder` deep enough to survive `UseRouting`/`GetAutofacRoot` tests the
framework, not the change.
**Alternatives rejected:** refactoring the bootstrap for testability (scope creep in a
published package for a two-line hook); pretending a mock-heavy test of framework extension
methods is TDD (it asserts nothing real).

### ADR-5 — No version-step or publish phase
**Decision:** The plan ends with the two code commits on the task branch. Stepping to 1.0.342
and pushing NuGet is the human running `PushNugetPackages.ps1` after merge.
**Context:** `Submit-NugetPackage` = `Step-ProjectVersion` + `Invoke-GitClean` +
`Push-ToNuget` — the script already owns version stepping (the repo's "Stepped project to
version X" commits are its output). A plan phase duplicating that would fight the tooling.
**Alternatives rejected:** a phase that edits `VersionPrefix` by hand (the script would
double-step); the orchestrator publishing (publishing is outward-facing and human-gated, same
doctrine as never pushing git remotes).

### ADR-6 — Names: `ConfigureMiddleware` and `ConfigureLogging`
**Decision:** Match ASP.NET's own vocabulary (`Configure*`) and the hosting concepts consumers
already know.
**Alternatives rejected:** `OnConfigureMiddleware`-style event naming (these are not events);
`AddMiddleware` (implies a registry, see ADR-1).

## Assumptions

- The rules in `src/.claude/rules/csharp/*` govern this repo's C# (TDD, xUnit + FakeItEasy +
  FatCat.Testing with `.Not.` negation, CSharpier + `dotnet format`, warnings-as-errors, block
  bodies only). Note: `src/CLAUDE.md`'s header text says "Haivision / Command 360" — a stale
  copy; the rule files themselves are the operative standards. Flagged for the human below.
- Build/test entry points: `dotnet build ToolKit.sln` and `dotnet test ToolKit.sln` from
  `src/` (the solution contains the sample/spike projects; they are expected to build).
- `Tests.ToolKit` is the home for any new tests (it references `Toolkit.WebServer`); follow its
  existing folder/naming conventions (`*Specs` folders, verb-first facts, `BddBase` per the
  repo's testing rules — verify the local precedent before writing).
- `OneOff/Old/ServerWorker.cs` constructs the `ToolkitWebApplicationSettings` used by
  `OneOff` — the phase locates the exact construction site and wires the sample hooks there.
  If `OneOff` proves awkward to run, `SampleDocker/Program.cs` is the fallback smoke host.
- The Apostil review skills (`unit-test-review`, `code-review`, `code-security-review`) may or
  may not be available to sessions opened in this repo. Phases run them when available;
  otherwise the implementing session performs and documents a manual review pass covering the
  same concerns (tests match behavior; standards conformance; no security regressions in the
  new public surface).
- The task branch is `task/logging`, created from `main` (the repo is currently on `main` with
  a clean tree; the commit policy forbids working on `main`).

## Open Questions

None blocking. Flag for the human reviewer:

- **Stale `src/CLAUDE.md` header** — it introduces itself as another product's standards file.
  Worth a one-line fix someday; not part of this work item.
- **Second consumer check:** Fog builds against these packages. The hooks are additive and
  null-default, so Fog is unaffected — but the 1.0.342 bump is the moment to confirm nothing
  else in flight rides along in the same publish.
- **`EqualObject` equality:** `ToolkitWebApplicationSettings : EqualObject` — adding delegate
  properties may participate in equality comparison. No known consumer compares settings
  instances; if the implementing session sees equality-related test fallout, exclude or accept
  per `EqualObject`'s conventions and log it.
