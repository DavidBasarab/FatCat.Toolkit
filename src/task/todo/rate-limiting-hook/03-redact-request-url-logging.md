# Phase 3 — Drop the Query String from Exception-Path Request Logging

- **Work item:** rate-limiting-hook (see `src/task/todo/rate-limiting-hook/00-overview.md`)
- **Depends on:** — (independent of Phases 1 and 2; run third)
- **Depended on by:** —
- **Risk:** **Low** — log-content change only. No API is removed or changed, nothing a consumer
  compiles against moves. The only observable difference is that two log lines lose the query string.
  Verified: no consumer parses or alerts on these lines (`consumer-compatibility.md`).

## Context (complete handoff — read before coding)

Read `src/CLAUDE.md` with all `src/.claude/rules/csharp/*.md` first — mandatory (TDD, xUnit +
FakeItEasy + FatCat.Testing with `.Not.` negation, CSharpier owns formatting, warnings are errors,
**block bodies only — no expression-bodied members anywhere, including tests**). Ignore the stale
"Haivision" header text in `CLAUDE.md` — the rule files are the operative standards.

**The defect.** `ApplicationStartUp.CaptureMiddlewareExceptions`
(`src/Toolkit.WebServer/ApplicationStartUp.cs`) logs the full display URL — **including the query
string** — on both catch branches:

```csharp
catch (TaskCanceledException)
{
	var displayUrl = context.Request.GetDisplayUrl();

	if (SystemScope.Container.TryResolve<IToolkitLogger>(out var logger))
	{
		logger!.Information($"Could not complete call to {displayUrl}");
	}
}
catch (Exception e)
{
	var displayUrl = context.Request.GetDisplayUrl();

	if (SystemScope.Container.TryResolve<IToolkitLogger>(out var logger))
	{
		logger!.Warning($"Error calling {displayUrl}");
		logger!.Exception(e);
	}

	throw;
}
```

Any consumer carrying a secret in a query string leaks it to the log sink whenever a request throws.
`Fog` has exactly that shape — `GET api/lokr/search?first=…&second=…&third=…` carries a live
access-code passphrase, and Fog's sink is MongoDB. Fog removed every one of its **own** log
statements that echoed those words but cannot remove this one. CWE-532 (Insertion of Sensitive
Information into Log File).

Other current state you will find:

- `src/Toolkit.WebServer/HttpRequestExtensions.cs` — an existing `public static class` on
  `HttpRequest` with one member (`ReadContent`). The new formatting helper belongs here: it is the
  established home, it keeps the change in one place, and unlike `ApplicationStartUp` it is a **real
  test seam** (public, no static state, exercisable with `DefaultHttpContext`).
- `Tests.ToolKit` references `Toolkit.WebServer` and therefore the ASP.NET Core shared framework —
  `Microsoft.AspNetCore.Http.DefaultHttpContext` is available to tests without any package change.
  Follow the conventions of an existing test file such as
  `Tests.ToolKit/WebServer/SignalR/ToolkitWebApplicationSettingsTests.cs`.
- `GetDisplayUrl` comes from `Microsoft.AspNetCore.Http.Extensions` — after this change the `using`
  may become unused in `ApplicationStartUp.cs`. Remove it if the analyzers flag it (warnings are
  errors).
- The solution is **`ToolKit.slnx`**, not `ToolKit.sln`.

## Design (build exactly this shape)

**`HttpRequestExtensions`** gains (keep members ordered as the file/analyzers expect):

```csharp
public static string DisplayPath(this HttpRequest request)
{
	return $"{request.PathBase}{request.Path}";
}
```

- `PathBase` is included so the line stays complete for a consumer that sets `Settings.BasePath`;
  with no base path this is exactly `Request.Path`.
- Scheme, host and query string are all deliberately dropped (overview ADR-4). The path identifies
  the failing endpoint, which is all these two lines exist to say.
- No null handling: `HttpRequest.Path` and `PathBase` are `PathString` value types and interpolate to
  `string.Empty` when unset. Do not add defensive null annotations
  (`src/.claude/rules/csharp/not-allowed.md`).

**`ApplicationStartUp.CaptureMiddlewareExceptions`** — use it on both branches, keeping the local
variable and the message text otherwise unchanged:

```csharp
var displayUrl = context.Request.DisplayPath();
```

Everything else in the method — the `throw;` on the general branch, the `TryResolve` guards, the
`logger!.Exception(e)` call, the message wording — stays exactly as it is.

## Steps

1. **Write the failing tests first**: `Tests.ToolKit/WebServer/HttpRequestExtensionsTests.cs`,
   namespace `Tests.FatCat.Toolkit.WebServer`. Build the request with
   `new DefaultHttpContext().Request` and set `Path`, `PathBase` and `QueryString` directly. One
   assertion per `[Fact]`:
   - Returns the path when there is no query string.
   - **Omits the query string when one is present** — this is the security-relevant fact; assert the
     result does not contain the secret value (`.Should().Not.Contain(...)`) as well as that it
     equals the expected path.
   - Includes the path base when one is set.
   - Returns an empty string for a request with no path set.
   Use `Faker.Create<string>()` for the secret-ish values rather than hard-coding
   (`src/.claude/rules/csharp/testing.md`). Observe red before green.
2. **Implement** `DisplayPath` and switch both catch branches to it.
3. Remove the now-unused `Microsoft.AspNetCore.Http.Extensions` using if nothing else in the file
   needs it.
4. **Build + full test suite** from `src/`: `dotnet build ToolKit.slnx` (zero warnings — they are
   errors) and `dotnet test ToolKit.slnx`.
5. **Smoke-check with OneOff** (from `src/OneOff`): `dotnet run` and call an endpoint that throws,
   with a query string carrying a marker value —
   `curl "http://localhost:<port>/api/<throwing route>?secret=DO-NOT-LOG-THIS"`. Confirm the logged
   line reads `Error calling /api/<route>` with **no `?secret=`**. If no OneOff endpoint throws,
   add a temporary one for the smoke check and **do not commit it** (or commit a deliberate
   `request/throws` sample endpoint if that reads better as living documentation — say which you did
   in the phase report). If `OneOff` will not start because
   `C:\DevelopmentCert\DevelopmentCert.pfx` is missing, fall back to `SampleDocker` and record the
   substitution. Never report a smoke result you did not observe.
6. **Formatting/style:** run the repo's standard passes per `src/.claude/rules/csharp/toolchain.md`,
   then build again.

## Definition of Done (all mandatory)

- [ ] TDD honored (red observed before green) — this phase has a genuine seam, so there is no ADR-5
      exemption for the extension method's tests
- [ ] `dotnet build ToolKit.slnx` — zero warnings (warnings are errors)
- [ ] `dotnet test ToolKit.slnx` — entire existing suite plus the new tests green
- [ ] Both catch branches changed; message wording, `throw;`, and `TryResolve` guards untouched
- [ ] Formatting/style passes run; build re-run so CSharpier applies
- [ ] No banned patterns (block bodies only, no expression-bodied members, naming rules per
      `src/.claude/rules/csharp/`)
- [ ] Smoke check observed: a request with a query string that throws logs the path only
- [ ] Review pass: run `unit-test-review` → `code-review` → `code-security-review` if those skills
      are available in the session (restarting the loop after any fix); otherwise perform and
      document a manual review covering the same concerns
- [ ] Exactly one commit on branch `rate-limiting-hook`, message referencing this file; **no push**

Suggested commit message:

```
rate-limiting-hook phase 3: log request path without query string on the exception path, CWE-532 (src/task/todo/rate-limiting-hook/03-redact-request-url-logging.md)
```

## Rollback Procedure

- `git revert <phase-3-commit>`. Independent of Phases 1 and 2 — no cascade either way.
- Note if you revert: doing so reinstates the query-string leak. Prefer rolling forward with a
  different formatting choice.

## Phase Report (produce before finishing)

Files added/changed/deleted; test counts (new/total/passing); the exact before/after log line from
the smoke check; whether a throwing sample endpoint was committed or used temporarily; deviation
log; open questions for the human reviewer — in particular whether operations needs scheme/host back
in these lines (ADR-4 says the sink's own metadata carries it; confirm with the human before the
package is published if there is any doubt).

## Hand-off

- **Behaviour change (ships in 1.0.344):** the two exception-path log lines in
  `CaptureMiddlewareExceptions` now read `Could not complete call to /some/path` and
  `Error calling /some/path` — path (with path base) only, no scheme, host or query string. No API
  changed; no consumer needs a code change.
- **New public API:** `FatCat.Toolkit.WebServer.HttpRequestExtensions.DisplayPath(this HttpRequest)`
  — available to consumers that want the same safe formatting in their own logging.
- **Consumer benefit:** `Fog`'s passphrase words stop reaching its Mongo log sink on the exception
  path — the last leak its `email_opt_in` AC10 could not close from its own side.
