# Phase 2 — `ConfigureLogging` Bootstrap Hook

- **Work item:** logging (see `src/task/logging/00-overview.md`)
- **Depends on:** — (independent of Phase 1; executed second for linear history)
- **Depended on by:** —
- **Risk:** **Medium** — adds public API surface to a published NuGet package and touches the
  logging bootstrap every consumer runs. Mitigations: null default + null-conditional
  invocation, `ClearProviders()` behavior untouched when unset, full existing suite must stay
  green.

## Context (complete handoff — read before coding)

Read `src/CLAUDE.md` with all `src/.claude/rules/csharp/*.md` first — mandatory. Ignore the
stale "Haivision" header text; the rule files are the operative standards.

Current state you will find:

- `src/Toolkit.WebServer/ApplicationStartUp.cs` → `ConfigureServices` contains:

  ```csharp
  services.AddLogging(options =>
  {
      options.ClearProviders();
  });
  ```

  Every Microsoft logging provider is cleared unconditionally — framework logs (Kestrel
  startup/bind errors, routing, TLS failures) are discarded and no consumer can get them back.
  This is the pain the hook removes: a consumer can add a provider that bridges framework logs
  into its own sinks (Apostil: Serilog console + rolling file — its "troubleshooting on remote
  Azure" story depends on seeing container-startup and Kestrel failures).
- `src/Toolkit.WebServer/ToolkitWebApplicationSettings.cs` — if Phase 1 landed, it now has
  `ConfigureMiddleware` beside the older delegates; this phase adds a sibling property.
- Settings are reached statically via `ToolkitWebApplication.Settings` — follow that pattern.
- Note `ConfigureServices` wraps its body in a broad try/catch that logs through
  `IToolkitLogger` — a consumer delegate that throws will currently be swallowed into that
  catch. Leave that boundary as-is (changing the toolkit's exception policy is not this work
  item), but be aware of it when smoke-checking.
- `OneOff/Old/ServerWorker.cs` constructs the sample host settings (Phase 1 documented the
  exact site).

## Design (build exactly this shape)

**`ToolkitWebApplicationSettings`** gains:

```csharp
public Action<ILoggingBuilder> ConfigureLogging { get; set; }
```

(`ILoggingBuilder` is `Microsoft.Extensions.Logging`; add the using if not present.)

**`ApplicationStartUp.ConfigureServices`** — invoke after `ClearProviders()` (ADR-3):

```csharp
services.AddLogging(options =>
{
	options.ClearProviders();

	ToolkitWebApplication.Settings.ConfigureLogging?.Invoke(options);
});
```

Notes that matter:

- **After `ClearProviders()` is the contract:** what the consumer adds is exactly what exists.
  Unset = cleared providers, identical to today.
- No toolkit opinion about which logger: the toolkit gains **no** Serilog (or any provider)
  reference. The consumer brings its own provider/bridge in the delegate.

**`OneOff` sample wiring (committed — living documentation):** in `ServerWorker`'s settings
construction, beside the Phase 1 sample:

```csharp
ConfigureLogging = loggingBuilder => ConsoleLog.WriteCyan("ConfigureLogging hook invoked"),
```

(Deliberately minimal: it proves invocation without adding a logging-provider package to
`OneOff`. Do not add `Microsoft.Extensions.Logging.Console` or similar just for the sample.)

## Steps

1. **Test seam check (TDD honesty — before implementation):** unlike `Configure`,
   `ConfigureServices` takes a plain `IServiceCollection` — a real
   `new ServiceCollection()` may make an honest test possible: arrange
   `ToolkitWebApplication.Settings` (it has a public static setter path via `Run`? — verify;
   `Settings` is `{ get; private set; }` set only in `Run`, so a direct unit test may be
   blocked by the static; check for any existing test precedent that sets it, e.g. via
   reflection — if only reflection reaches it, that is not an honest seam). If a clean
   arrangement exists: failing test first — build the service provider from the collection,
   resolve `ILoggerFactory`... — but weigh honestly whether the assertion proves the hook ran
   (e.g. a flag set inside a `ConfigureLogging` delegate after building the provider and
   creating a logger). If the static-settings obstacle makes this contrived, proceed
   implementation-first per overview ADR-4 and record the deviation.
2. **Implement** the settings property and the `ConfigureServices` invocation.
3. **Wire the OneOff sample** as above.
4. **Build + full test suite:** from `src/`: `dotnet build ToolKit.sln` (zero warnings) and
   `dotnet test ToolKit.sln` (all green — the unchanged suite is the unset-hook regression
   check).
5. **Smoke-check with OneOff** (from `src/OneOff`): `dotnet run` —
   - The cyan `ConfigureLogging hook invoked` line prints during startup (before/around the
     server-started output).
   - Requests still serve; Phase 1's per-request hook line still prints (no interference).
   - Temporarily comment the sample `ConfigureLogging` out, run again, confirm
     current-package behavior exactly, restore.
6. **Formatting/style** per `src/.claude/rules/csharp/toolchain.md`; build again.

## Definition of Done (all mandatory)

- [ ] TDD honored where a genuine seam exists; any deviation recorded with the ADR-4 rationale
- [ ] `dotnet build ToolKit.sln` — zero warnings (warnings are errors)
- [ ] `dotnet test ToolKit.sln` — entire suite green
- [ ] Formatting/style passes run; build re-run so CSharpier applies
- [ ] No banned patterns; no new package references anywhere (the toolkit stays
      provider-agnostic — this is also a security/supply-chain gate)
- [ ] Smoke checks observed: hook invoked at startup, unchanged behavior when unset, Phase 1
      hook unaffected
- [ ] Review pass: `unit-test-review` → `code-review` → `code-security-review` if available in
      the session (loop restarts after any fix); otherwise a documented manual review covering
      the same concerns
- [ ] Exactly one commit on branch `task/logging`, message referencing this file; **no push**

Suggested commit message:

```
logging phase 2: ConfigureLogging bootstrap hook on ToolkitWebApplicationSettings (src/task/logging/02-configure-logging-hook.md)
```

## Rollback Procedure

- `git revert <phase-2-commit>` (nothing depends on it; no cascade).
- Pre-publish, revert freely; post-publish with consumer adoption, roll forward instead (same
  note as Phase 1).

## Phase Report (produce before finishing)

Files added/changed/deleted; test counts (new/total/passing); deviation log (test-seam outcome
per step 1 — especially whether the static `Settings` blocked an honest test; the swallowed-
exception observation if a throwing delegate was probed); open questions or risks for the human
reviewer.

## Hand-off

- **Public contract (ships in the next package version):**
  `ToolkitWebApplicationSettings.ConfigureLogging : Action<ILoggingBuilder>` — null by default;
  when set, invoked once during service configuration, immediately after the host's
  `ClearProviders()`, so the providers the consumer adds are the complete provider set.
- **First consumer:** Apostil `api_logging` phase 1 — bridge framework logs into its Serilog
  console + rolling-file sinks (Kestrel/startup visibility for remote-Azure troubleshooting).
- **Work item complete after this phase:** the human merges `task/logging` → `main` and runs
  `src/PushNugetPackages.ps1` (steps versions and publishes — overview "Publish flow"); then
  ask for the Apostil `api_logging` plan update to consume the new package.
