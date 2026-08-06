# Phase 1 — `ConfigureServices` Host Hook

- **Work item:** rate-limiting-hook (see `src/task/todo/rate-limiting-hook/00-overview.md`)
- **Depends on:** — (first phase)
- **Depended on by:** Phase 2 (its OneOff sample enables the policy this phase registers)
- **Risk:** **Medium** — adds public API surface to a published NuGet package
  (`FatCat.Toolkit.WebServer`) and inserts an invocation into the service registration every
  consumer app runs. Mitigations: the property defaults to null and the invocation is
  null-conditional, so unset = byte-for-byte current behaviour; the invocation sits outside the
  existing swallowing `try/catch` (overview ADR-2); the full existing test suite must stay green.

## Context (complete handoff — read before coding)

Read `src/CLAUDE.md` with all `src/.claude/rules/csharp/*.md` first — mandatory (TDD, xUnit +
FakeItEasy + FatCat.Testing with `.Not.` negation, CSharpier owns formatting, warnings are errors,
**block bodies only — no expression-bodied members anywhere, including tests**). Ignore the stale
"Haivision" header text in `CLAUDE.md` — the rule files are the operative standards.

Current state you will find:

- `src/Toolkit.WebServer/ToolkitWebApplicationSettings.cs` — the settings POCO (`: EqualObject`).
  It already carries optional bootstrap delegates: `Action<ILoggingBuilder> ConfigureLogging` and
  `Action<IApplicationBuilder> ConfigureMiddleware`, plus `Action<string> OnLogEvent` and
  `Action OnWebApplicationStarted`. The new property matches that style and sits immediately after
  `ConfigureMiddleware`.
- `src/Toolkit.WebServer/ApplicationStartUp.cs` — `internal sealed`. Its
  `ConfigureServices(IServiceCollection services)` method wraps its entire body in a `try/catch`
  that swallows the exception and logs it only if `SystemScope.Container.TryResolve<IToolkitLogger>`
  succeeds. Inside the `try`: `AddControllers`, `AddEndpointsApiExplorer`, `AddCors`,
  `AddHttpContextAccessor`, `ConfigureControllers`, `AddAuthentication`, `AddSignalR`, and
  `AddLogging` (which is where the `ConfigureLogging` hook is invoked).
- `src/Toolkit.WebServer/ToolkitWebApplication.cs` — `Run` calls
  `applicationStartUp.ConfigureServices(builder.Services)` **before** `builder.Build()`, so anything
  registered by the hook is in the collection when
  `ToolkitServiceProviderFactory.CreateBuilder` runs `containerBuilder.Populate(services)`. Consumer
  registrations therefore resolve through Autofac exactly like the toolkit's own.
- Settings are reached statically via `ToolkitWebApplication.Settings` throughout
  `ApplicationStartUp` — follow that existing pattern.
- `Tests.ToolKit` (assembly `Tests.FatCat.Toolkit`, root namespace `Tests.FatCat.Toolkit`)
  references `Toolkit.WebServer`. `Tests.ToolKit/WebServer/SignalR/ToolkitWebApplicationSettingsTests.cs`
  is an existing, working example of testing this settings class directly — read it before writing
  tests, and match its style (constructor-built `sut`, one assertion per `[Fact]`, verb/behaviour
  names, block bodies).
- `ApplicationStartUp` itself has **no test seam** — it is `internal sealed`, there is no
  `InternalsVisibleTo`, and it reads the static `ToolkitWebApplication.Settings` whose setter is
  `private` and assigned only inside `Run`. Do not try to test it (overview ADR-5).
- `OneOff` is the runnable sample host: `Program.RunServer` → `Old/ServerWorker.DoWork` constructs
  the `ToolkitWebApplicationSettings` and calls `ToolkitWebApplication.Run`. It already wires
  `ConfigureLogging` and `ConfigureMiddleware` samples — add the new one alongside them.
- The solution is **`ToolKit.slnx`**, not `ToolKit.sln`.

## Design (build exactly this shape)

**`ToolkitWebApplicationSettings`** gains, immediately after `ConfigureMiddleware`:

```csharp
public Action<IServiceCollection> ConfigureServices { get; set; }
```

Add `using Microsoft.Extensions.DependencyInjection;` if the compiler asks for it.

**`ApplicationStartUp.ConfigureServices`** — invoke the hook as the **last statement of the method,
after the `try/catch` block closes** (overview ADR-2 — a consumer delegate that throws must fail
startup loudly, not be swallowed by the existing `catch`):

```csharp
public void ConfigureServices(IServiceCollection services)
{
	try
	{
		// ... unchanged toolkit registrations ...
	}
	catch (Exception ex)
	{
		// ... unchanged ...
	}

	ToolkitWebApplication.Settings.ConfigureServices?.Invoke(services);
}
```

Notes that matter:

- **Position is the contract:** after every toolkit registration, so a consumer can add to — or
  deliberately override — them; before `builder.Build()`, so registrations reach the container.
- Null-conditional invoke only — no flag, no option enum. Unset means an untouched collection.
- Do **not** move the invocation inside the `try`, and do **not** add a `try/catch` of your own.
- Do not rename or reorder anything else in the file.

**`OneOff` sample wiring (committed — living documentation, ADR-5):** in `OneOff/Old/ServerWorker.cs`,
in the `ToolkitWebApplicationSettings` initializer next to the existing `ConfigureLogging` /
`ConfigureMiddleware` samples, register a named fixed-window policy:

```csharp
ConfigureServices = services =>
{
	ConsoleLog.WriteCyan("ConfigureServices hook invoked");

	services.AddRateLimiter(options =>
	{
		options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

		options.AddFixedWindowLimiter(
			"oneoff-fixed",
			limiterOptions =>
			{
				limiterOptions.PermitLimit = 3;
				limiterOptions.Window = TimeSpan.FromSeconds(10);
				limiterOptions.QueueLimit = 0;
			}
		);
	});
},
```

- `AddRateLimiter` and `AddFixedWindowLimiter` come from the ASP.NET Core shared framework
  (`Microsoft.Extensions.DependencyInjection` / `Microsoft.AspNetCore.RateLimiting`;
  `StatusCodes` from `Microsoft.AspNetCore.Http`). Add usings as the compiler requires — do not guess
  them up front. **No new package reference.** If `OneOff` cannot see these APIs, add
  `<FrameworkReference Include="Microsoft.AspNetCore.App" />` to `OneOff/OneOff.csproj` only.
- The policy name `"oneoff-fixed"` is consumed verbatim by Phase 2's `[EnableRateLimiting]`
  attribute — do not change it without updating that phase file.
- Registering the limiter without `UseRateLimiter()` is **inert**: nothing is throttled until Phase 2
  adds the middleware. That is the point — this phase proves the registration path alone.
- Match the file's existing sample style; keep it minimal.

## Steps

1. **Write the failing tests first** in `Tests.ToolKit`, alongside/mirroring
   `WebServer/SignalR/ToolkitWebApplicationSettingsTests.cs` (put them in a
   `Tests.ToolKit/WebServer/ToolkitWebApplicationSettingsHookTests.cs` with namespace
   `Tests.FatCat.Toolkit.WebServer`, or extend the existing class if that reads better with the
   repo's conventions — decide by looking, and say which you chose in the phase report). Cover, one
   assertion per fact:
   - `ConfigureServices` defaults to null on a new settings instance.
   - A delegate assigned to `ConfigureServices` receives the exact `IServiceCollection` it is
     invoked with (`sut.ConfigureServices.Invoke(collection)` → captured argument is the same
     instance). This is a property-contract test, not a bootstrap test.
   Observe red before green.
2. **Implement** the settings property and the `ApplicationStartUp.ConfigureServices` invocation.
3. **Wire the OneOff sample** as above.
4. **Build + full test suite** from `src/`: `dotnet build ToolKit.slnx` (zero warnings — they are
   errors) and `dotnet test ToolKit.slnx` (everything green; the unchanged suite passing is the
   "unset hook changes nothing" regression check).
5. **Smoke-check with OneOff** (from `src/OneOff`): `dotnet run` —
   - Startup succeeds exactly as before, and the cyan `ConfigureServices hook invoked` line prints
     **once**, during startup.
   - An existing endpoint still behaves identically and is **not** throttled — hammer
     `curl http://localhost:<port>/api/request/good` more than 3 times inside 10 seconds and confirm
     every response is `200`. (Find the port and routes by reading `ServerWorker` / the OneOff
     endpoints such as `BadRequestEndpoint`; do not guess.)
   - Temporarily comment the sample `ConfigureServices` out, run again, confirm behaviour is exactly
     current-package behaviour, then restore it.
   - If `OneOff` will not start because `C:\DevelopmentCert\DevelopmentCert.pfx` is missing, fall
     back to `SampleDocker` and **record the substitution in the phase report**. Never report a
     smoke result you did not observe.
6. **Formatting/style:** run the repo's standard passes per `src/.claude/rules/csharp/toolchain.md`
   (CSharpier, plus `dotnet format` style/analyzers as that file prescribes), then build again.

## Definition of Done (all mandatory)

- [ ] TDD honored for the settings-level tests (red observed before green); the
      `ApplicationStartUp` invocation is covered by the OneOff smoke per ADR-5, and the phase report
      says so explicitly
- [ ] `dotnet build ToolKit.slnx` — zero warnings (warnings are errors)
- [ ] `dotnet test ToolKit.slnx` — entire existing suite plus the new tests green
- [ ] Formatting/style passes run; build re-run so CSharpier applies
- [ ] No banned patterns (block bodies only, no expression-bodied members, no `var`-less locals
      where `var` is obvious, naming rules per `src/.claude/rules/csharp/`)
- [ ] No new `PackageReference` in `Toolkit.WebServer` (framework APIs only)
- [ ] Smoke checks observed: startup hook line printed once; existing endpoints unthrottled;
      unchanged behaviour with the hook unset
- [ ] Review pass: run `unit-test-review` → `code-review` → `code-security-review` if those skills
      are available in the session (restarting the loop after any fix); otherwise perform and
      document a manual review covering the same concerns (behaviour coverage, standards
      conformance, new-public-surface security)
- [ ] Exactly one commit on branch `rate-limiting-hook`, message referencing this file; **no push**

Suggested commit message:

```
rate-limiting-hook phase 1: ConfigureServices host hook on ToolkitWebApplicationSettings (src/task/todo/rate-limiting-hook/01-configure-services-hook.md)
```

## Rollback Procedure

- `git revert <phase-1-commit>`. If Phase 2 has already landed, revert it first — its OneOff sample
  uses this hook.
- No data/config steps. If the package was already published with the hook and consumers adopted it,
  reverting is a breaking change — at that point roll **forward** instead (the publish gate is the
  human's; pre-publish, revert freely).

## Phase Report (produce before finishing)

Files added/changed/deleted; test counts (new/total/passing); where the new tests were placed and
why; the exact OneOff construction site, port and routes used, and whether `OneOff` or the
`SampleDocker` fallback was the smoke host; deviation log; open questions or risks for the human
reviewer.

## Hand-off

- **Public contract (ships in 1.0.344):**
  `ToolkitWebApplicationSettings.ConfigureServices : Action<IServiceCollection>` — null by default;
  when set, invoked exactly once during host construction, **after** all toolkit service
  registrations and **before** the container is built, so a consumer can add to or override them.
  Exceptions thrown by the delegate propagate and fail startup.
- **Consumed by:** Phase 2 (OneOff enables the `"oneoff-fixed"` policy registered here).
- **First external consumer:** `Fog`'s `Common/Common.WebServer/Infrastructure/ApplicationRunner.cs`
  (`tasks/todo/email_opt_in/07-passphrase-endpoint-hardening.md`), once 1.0.344 is published.
