# Phase 1 — Arbitrary request headers on `IWebCaller`

- **Work item:** web-caller-request-headers (see `task/todo/web-caller-request-headers/00-overview.md`)
- **Depends on:** nothing.
- **Depended on by:** nothing in this repo. **Blocks** `C:\Code\Apostil`'s `claude_backend` work item (a hard
  precondition; its orchestrator stops if `IWebCaller.AddHeader` is absent).
- **Risk:** **Low–Medium.** Additive to the **published `IWebCaller` interface** — nothing inside this repository
  can break at compile time; the only compile break is an `IWebCaller` implementation living in a consumer (see
  `consumer-compatibility.md`). The care is applying arbitrary/custom header names correctly
  (`TryAddWithoutValidation`, not `Add`) and never logging the value (ADR-4).

## Context (complete handoff — read before coding)

You have no context from any prior session. Read, in this order:

1. **`README.md`** at the repo root in full (per `CLAUDE.md`) — `FatCat.Toolkit` is a **published NuGet library**;
   every public member is a promise to consumers you cannot see.
2. `task/todo/web-caller-request-headers/00-overview.md` — the ADRs are **binding**, especially:
   - **ADR-1** — one flat `void AddHeader(string name, string value)` on `IWebCaller`.
   - **ADR-2** — apply stored headers in `SendWebRequest` via `TryAddWithoutValidation`; leave `Accept`/auth alone.
   - **ADR-3** — added headers **persist** on the instance (like `Accept`/Basic auth), not single-use like the
     bearer token. `ClearHeaders()` optional — default omit.
   - **ADR-4** — the new path logs **no** header name→value; a secret header must not reach a log.
3. **The production file you change — read every line:** `src/ToolKit/Web/WebCaller.cs`. Note:
   - the `IWebCaller` interface (top of the file) and where to add the member;
   - `EnsureAccept(request)` and `EnsureAuthorization(request)` — the two existing "apply headers" helpers your
     `EnsureHeaders(request)` sits beside;
   - `SendWebRequest(...)` — where `EnsureAccept`/`EnsureAuthorization` are called and where the request is built;
   - that the bearer token is reset after a successful send (line ~325) — you do **not** copy that for headers
     (ADR-3), and you do **not** log header values (ADR-4; contrast the existing bearer Debug log).
4. **The spec you extend — read every line:** `src/Tests.ToolKit/Web/Api/WebCallerSpecs/WebCallerTests.cs` (the
   abstract base) and its concrete derivations (there is a Post/Get pairing per HTTP verb). Note:
   - the specs make **real calls to `https://httpbin.org`** and assert against the echoed response
     (`VerifyBearerToken` asserts `response.AuthorizationHeader`; the query-string facts assert
     `response.QueryParameters`);
   - the `HttpBinResponse` type it deserialises into — check whether it already exposes the **echoed request
     headers** (httpbin returns them under a `headers` object). If it does not, add a minimal `Headers` accessor
     to `HttpBinResponse` so the new fact can read the echoed custom header (read `HttpBinResponse` first).
5. `src/.claude/rules/csharp/*` — TDD, xUnit + FakeItEasy + FatCat.Testing (`.Not.` negation), CSharpier owns
   formatting, warnings are errors, **block bodies only (including tests)**.

## What to build

> Write the spec first (TDD), watch it fail, then add production code until green.

### 1. `src/ToolKit/Web/WebCaller.cs` — the interface member (production)

Add to the `IWebCaller` interface, near the other header controls (`Accept`, `UseBasicAuthorization`,
`UserBearerToken`):

```csharp
void AddHeader(string name, string value);
```

### 2. `src/ToolKit/Web/WebCaller.cs` — the implementation (production)

- A backing store on the `WebCaller` class, e.g. `private readonly Dictionary<string, string> headers = new();`
  (initialise per the repo's collection idiom — check a neighbouring field; the toolkit uses collection
  expressions where the target allows).
- `AddHeader`:

  ```csharp
  public void AddHeader(string name, string value)
  {
      headers[name] = value;
  }
  ```

  (Last-write-wins on a repeated name — keep it simple. **No logging of `name` or `value`** — ADR-4.)
- A private `EnsureHeaders(HttpRequestMessage request)` that applies each stored header with
  `request.Headers.TryAddWithoutValidation(name, value)` — **not** `request.Headers.Add(...)`, which throws on
  custom/validated names like `x-api-key` (ADR-2). At most a content-free Debug line ("adding N request headers");
  **never** the name→value pairs.
- Call `EnsureHeaders(requestMessage)` in `SendWebRequest`, right beside the existing
  `EnsureAccept(requestMessage);` / `EnsureAuthorization(requestMessage);` calls.
- **Do not** reset `headers` after the send (ADR-3 — persist, unlike the bearer token). **Do not** touch
  `EnsureAccept`, `EnsureAuthorization`, the auth fields, or any existing signature.
- `ClearHeaders()` is **optional** (ADR-3) — default omit unless you want symmetry with `ClearAuthorization`; if
  you add it, add it to both the class and the interface and cover it with a fact.

### 3. `src/Tests.ToolKit/Web/Api/WebCallerSpecs/` — the specs (test)

Mirror the existing WebCaller facts (real httpbin call, assert the echoed response). Add facts (place them on the
abstract base so both the Get and Post derivations exercise them, or on whichever concrete spec matches the
existing style — follow what the file does):

- `CanSendACustomHeader` — `webCaller.AddHeader("x-custom-header", "the-value")`, make the call, assert the echoed
  request headers contain `x-custom-header` → `the-value` (read via `HttpBinResponse`'s headers accessor).
- `CanSendMultipleCustomHeaders` — add two headers (e.g. `x-api-key`-shaped and a version-shaped one), assert
  **both** are echoed. This is the consumer's real case (`x-api-key` + `anthropic-version`).
- `CustomHeaderCoexistsWithBearerToken` — set a bearer token **and** add a custom header; assert the echoed
  response carries **both** the `Authorization: Bearer …` (via `AuthorizationHeader`) and the custom header —
  neither clobbers the other (ADR-2).
- (If you add `ClearHeaders()`) `CanClearCustomHeaders` — add a header, clear, call, assert it is **not** echoed
  (`.Not.` negation, FatCat.Testing).

Use FatCat.Testing assertions with `.Not.` negation; `A<T>._` matchers if any fake is involved; block bodies. If
`HttpBinResponse` lacks a headers accessor, add a minimal one (echoed request headers) and note it in the report.

## Steps

1. **Baseline:** `dotnet build ToolKit.slnx` clean at 0 warnings; `dotnet test ToolKit.slnx` green — record the
   count. (The WebCaller specs hit httpbin.org; confirm the baseline suite is green in this environment.)
2. **Red:** write `CanSendACustomHeader` / `CanSendMultipleCustomHeaders` / `CustomHeaderCoexistsWithBearerToken`
   first; watch them fail (no `AddHeader` yet). Record the red.
3. **Green:** add the interface member, the backing store, `AddHeader`, and the `EnsureHeaders` call — until the
   facts pass.
4. **Secret-safety read-through (ADR-4):** confirm the new code logs no header name→value; a secret header value
   never reaches any log line.
5. `dotnet format` (style/analyzers only — CSharpier owns whitespace) and `dotnet build ToolKit.slnx`.
6. **One commit** on the `web-caller-request-headers` branch referencing this phase file. **Do not push, do not
   publish** (the human runs `PushNugetPackages.ps1`).

## Verification

- `IWebCaller` now declares `void AddHeader(string name, string value)`; `WebCaller` implements it and applies the
  stored headers in `SendWebRequest`.
- `grep -n "TryAddWithoutValidation" src/ToolKit/Web/WebCaller.cs` → present (custom names applied correctly).
- The new code path logs no header value — read-through-confirmed.
- `git diff --name-only`: `src/ToolKit/Web/WebCaller.cs`, the WebCaller spec file(s), and (only if needed) the
  `HttpBinResponse` type. **No other production file changes** — `EnsureAccept`, `EnsureAuthorization`, the auth
  setters, and every Get/Post/Put/Delete overload are unchanged.
- `dotnet build ToolKit.slnx` clean at 0 warnings; `dotnet test ToolKit.slnx` green; count up by the new facts.

## Definition of Done (all mandatory)

- [ ] **Red observed before green** (reported).
- [ ] `IWebCaller` exposes `void AddHeader(string name, string value)`; `WebCaller` stores added headers and
      applies them in `SendWebRequest` via `TryAddWithoutValidation`, beside `EnsureAccept`/`EnsureAuthorization`.
- [ ] A custom header, multiple custom headers, and a custom header alongside bearer auth all reach the server —
      asserted against httpbin's echo.
- [ ] Added headers **persist** on the instance (not reset after send, ADR-3); the new path **logs no header
      value** (ADR-4) — read-through-confirmed.
- [ ] No existing member changes behaviour or signature (`git diff` is `WebCaller.cs` + the spec(s) + optionally
      `HttpBinResponse`).
- [ ] Namespaces match folders; 0 warnings; `dotnet format` clean; `dotnet build` + `dotnet test` green.
- [ ] One local commit on `web-caller-request-headers` referencing this file; **nothing pushed, nothing
      published.**

Suggested commit message:

```
web-caller-request-headers phase 1: IWebCaller.AddHeader — arbitrary per-request headers via TryAddWithoutValidation (task/todo/web-caller-request-headers/01-arbitrary-request-headers.md)
```

## Rollback Procedure

- `git revert <phase-1-commit>` removes the `AddHeader` member, the backing store, and the `EnsureHeaders` call,
  and the specs. `IWebCaller` returns to its prior surface; nothing else is affected. **Data step:** none.
  **Config step:** none.
- If the package was already published from this commit (it should not be — publishing is a separate human step),
  the consumer's reference bump would need reverting too; the plan never publishes.

## Hand-off (the contract the consumer records)

- **`IWebCaller.AddHeader(string name, string value)`** — sets an arbitrary request header carried on every
  subsequent call from that caller (persistent for the caller's lifetime). Applied via `TryAddWithoutValidation`,
  so custom names (`x-api-key`, `anthropic-version`) work. The value is never logged.
- **For `C:\Code\Apostil` (`claude_backend`):** after the human publishes and Apostil bumps its
  `FatCat.Toolkit.WebServer` reference, `ClaudeLanguageModel` sets `x-api-key` = `Generation:Claude:ApiKey` and
  `anthropic-version` via `AddHeader`, and its specs assert the calls on the FakeItEasy `IWebCaller` fake
  (`A.CallTo(() => webCaller.AddHeader("x-api-key", key)).MustHaveHappened()`). The consumer's orchestrator checks
  for `AddHeader` before its phase 1 and stops if it is absent.
