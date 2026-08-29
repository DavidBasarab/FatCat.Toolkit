# web-caller-request-headers (FatCat.Toolkit) — Overview

> **Origin:** raised by a consumer. Written from `C:\Code\Apostil` while planning that repo's
> `claude_backend` work item (`tasks/todo/claude_backend/`), whose **ADR-8 records that the work item cannot be
> built without this change** and whose orchestrator **stops before phase 1** if `IWebCaller` cannot send
> arbitrary request headers.
>
> **Status:** planned (2026-08-29). One phase; runbook in `orchestrator.md`; consumer verification in
> `consumer-compatibility.md`.

## Work Item

Add a way to set **arbitrary request headers** on a call made through `IWebCaller`, so a consumer can send a
header the toolkit does not already model — specifically an **API-key header** and a **version header** that are
neither `Authorization` nor `Accept`.

```csharp
// src/ToolKit/Web/WebCaller.cs — the one new interface member
void AddHeader(string name, string value);
```

Every request the caller subsequently sends carries the added header(s). That is the whole change: one new member
on `IWebCaller`, its implementation on `WebCaller`, applying the stored headers in `SendWebRequest`, and a spec.

## Why — the consumer's problem, stated concretely

`C:\Code\Apostil` is adding a **Claude (Anthropic) generation backend** over `IWebCallerFactory` (its
`claude_backend` work item, ADR-2 — mirroring how its `OllamaTextEmbedder` already posts through `IWebCaller`).
The Anthropic Messages API (`POST https://api.anthropic.com/v1/messages`) requires two headers on **every**
request:

| Header | Value | Why `IWebCaller` cannot send it today |
|---|---|---|
| `x-api-key` | the Anthropic API key | It is **not** `Authorization: Bearer` — a standard `sk-ant-…` key goes in the custom `x-api-key` header. `IWebCaller` offers only `UseBasicAuthorization` and `UserBearerToken`, both of which set `request.Headers.Authorization`. |
| `anthropic-version` | `2023-06-01` | A custom header with no `IWebCaller` setter. |

`IWebCaller`'s current header surface (verified against `src/ToolKit/Web/WebCaller.cs`, 2026-08-29) is exactly:
`Accept` (a property), `UseBasicAuthorization`/`UserBearerToken`/`ClearAuthorization` (all touching
`request.Headers.Authorization`), and the `contentType` argument on `Post`/`Put`. **There is no path to an
arbitrary header.**

The only in-tree workaround, `SetClient(HttpClient)` with pre-set `DefaultRequestHeaders`, is **not acceptable to
the consumer**: Apostil resolves its `ILanguageModel` **per request** (not a singleton), so a per-instance
`HttpClient` means socket exhaustion, and it bypasses the toolkit's own `HttpClientFactory.Get()` pooling
(`WebCaller.SendWebRequest`, line 290). Apostil's plan (ADR-2) rejected it explicitly.

**There is direct precedent** for a consumer-raised, additive `IWebCaller`/`IMongoRepository` change gated as a
hard precondition of the consumer's work item — `CountByFilter`/`DistinctByFilter` (`mongo-count-and-distinct`)
and `QueryByFilter`/`DeleteByFilter` (`mongo-paged-and-delete`). This is the same move for the web client: a
small, additive capability the consumer needs, blocking its work item until it ships.

## Scope — and what is deliberately left out

**In scope:** `AddHeader(string name, string value)` on `IWebCaller` / `WebCaller`; storing added headers on the
instance; applying them to the outgoing `HttpRequestMessage` in `SendWebRequest`; a spec that proves an added
header reaches the server.

**Explicitly out of scope:**

- **Streaming HTTP.** `IWebCaller` has no streaming call; the Claude backend buffers (`stream:false`) exactly as
  the Ollama backend does. A streaming-HTTP `IWebCaller` call is a **separate, larger, future** work item that
  would upgrade both backends — named by the consumer (its ADR-8) but **not** this task. Do not add it here.
- **Anthropic-specific anything.** This is a generic "send an arbitrary header" capability, not an Anthropic
  client. No `x-api-key`/`anthropic-version` constants, no Claude wire shapes — those live in the consumer.
- **A typed header abstraction / auth scheme.** No `IHeaderProvider`, no per-scheme helpers. One `name`/`value`
  method, matching the flat shape of `Accept` and the auth setters already on the interface.
- **Reworking the existing auth setters or `Accept`.** They stay exactly as they are; `AddHeader` is additive and
  independent of them.
- **Fixing the pre-existing Debug-level logging of the bearer token** (`WebCaller.EnsureAuthorization`, line 262
  logs the token value at Debug). Out of scope here — but **flag it for the human** (see Open Questions): the new
  header code must **not** log header values, so a secret header like `x-api-key` never reaches a log through the
  new path.
- **Version stepping and publishing.** Same doctrine as `rate-limiting-hook` ADR-6, `mongo-batch-writes`,
  `mongo-count-and-distinct`, and `mongo-paged-and-delete` — **the plan never publishes.** The human reviews the
  commit and runs `PushNugetPackages.ps1`.

## Acceptance Criteria → Phase Map

| Acceptance criterion | Proven by |
|---|---|
| `IWebCaller` exposes `void AddHeader(string name, string value)` | Phase 1 |
| A header added via `AddHeader` is present on the outgoing request (server echoes it back) | Phase 1 |
| **Multiple** added headers are all sent (e.g. `x-api-key` *and* `anthropic-version` together) | Phase 1 |
| An added custom header coexists with `Accept` and with Basic/Bearer auth — none clobbers another | Phase 1 |
| The header **value is never written to a log** by the new code path (secret-safe; D27 on the consumer side) | Phase 1 |
| No existing member (`Accept`, the auth setters, the Get/Post/Put/Delete overloads) changes behaviour or signature | Phase 1 (`git diff` covers one production file) |
| No breaking change for `C:\Code\Apostil` or `C:\Code\Fog` | `consumer-compatibility.md` |
| `dotnet build ToolKit.slnx` / `dotnet test ToolKit.slnx` clean (0 warnings) | Phase 1's Definition of Done |

## Phases

| Phase | File | Risk | Depends on |
|---|---|---|---|
| 1 — Arbitrary request headers on `IWebCaller` | `01-arbitrary-request-headers.md` | **Low–Medium** — additive to a **published interface**, so nothing existing can break at compile time *inside* this repository; the only compile break is an `IWebCaller` implementation living in a consumer (see `consumer-compatibility.md`). The care is: apply arbitrary/custom header names correctly (use `TryAddWithoutValidation`, since `x-api-key`/`anthropic-version` are not typed header names) and never log the value. | — |

One phase: a single member, its implementation, and a spec. Nothing here decomposes into more.

## Current state that shapes the design (verified against source, 2026-08-29)

- **`IWebCaller`** (`src/ToolKit/Web/WebCaller.cs:11`) exposes `Accept`, `BaseUri`, `Timeout`, the Get/Post/Put/
  Delete overloads, `SetClient(HttpClient)`, `UseBasicAuthorization`, `UserBearerToken`. (`ClearAuthorization` is
  public on the `WebCaller` class but is **not** on the interface.) **There is no arbitrary-header member.**
- **`WebCaller.SendWebRequest`** (line 280) builds the `HttpRequestMessage`, then calls `EnsureAccept` (adds the
  `Accept` header if set) and `EnsureAuthorization` (sets `request.Headers.Authorization` from the bearer/basic
  fields), sets `request.Content` if there is a body, and sends. **The new work adds an `EnsureHeaders(request)`
  call alongside `EnsureAccept`/`EnsureAuthorization`, applying the stored headers.**
- **Header lifecycle today:** `Accept` and Basic auth **persist** on the instance (cleared via
  `ClearAuthorization`); the bearer token is **single-use** (reset to null after a successful send, line 325).
  See ADR-3 for the choice this task makes for custom headers.
- **The WebCaller specs hit real `httpbin.org`** (`src/Tests.ToolKit/Web/Api/WebCallerSpecs/WebCallerTests.cs`) —
  httpbin echoes the request headers in its JSON response (the existing `VerifyBearerToken` asserts
  `response.AuthorizationHeader`). **The new spec follows this pattern**: add a header, call httpbin, assert it was
  echoed. `HttpBinResponse` may need a `Headers` accessor (echoed request headers) if it does not already expose
  one — extend it minimally if so (read it first).
- **`IWebCallerFactory`** returns `IWebCaller` and needs **no** change — the new member is on `IWebCaller`.
- Solution is **`ToolKit.slnx`**; build and test from `src/`. Warnings are errors; block bodies only; CSharpier
  owns formatting; tests use xUnit + FakeItEasy + FatCat.Testing (`.Not.` negation).
- Current source version: **check `src/` and the queued `task/todo` items** (`mongo-paged-and-delete` was planning
  1.0.349 against an Apostil reference of 1.0.348). Do **not** hard-code a version — the human picks it at publish
  and the consumer bumps its reference to whatever ships this change.

## Decisions (lightweight ADRs)

### ADR-1 — One flat `AddHeader(string name, string value)` on `IWebCaller`, matching the existing header surface

**Decision:** add exactly `void AddHeader(string name, string value)` to `IWebCaller`, implemented on `WebCaller`.

**Context:** the interface's existing header controls are flat and imperative (`Accept` a property,
`UseBasicAuthorization(user, pass)`, `UserBearerToken(token)`). A `name`/`value` method matches that idiom, and is
exactly what the consumer needs to assert in its own tests (`A.CallTo(() => webCaller.AddHeader("x-api-key",
key)).MustHaveHappened()` on the FakeItEasy fake). It is the smallest possible surface that solves the problem.

**Consequence:** the consumer sets `x-api-key` and `anthropic-version` with two `AddHeader` calls before its
`Post`. Any future consumer that needs a custom header uses the same one method. `IWebCallerFactory` is untouched.

**Rejected:** an `IHeaderProvider`/typed abstraction (over-built for a name/value pair); a `Headers`
dictionary property on the interface (exposes and invites mutation of the backing store, and is a heavier
member to fake than a method); a bespoke `UseApiKey`/`anthropic`-flavoured helper (the toolkit is provider-neutral
— Anthropic specifics belong in the consumer).

### ADR-2 — Apply stored headers in `SendWebRequest` via `TryAddWithoutValidation`; do not disturb `Accept`/auth

**Decision:** store added headers in a `Dictionary<string, string>` on the `WebCaller` instance; add a private
`EnsureHeaders(HttpRequestMessage request)` called in `SendWebRequest` alongside `EnsureAccept`/`EnsureAuthorization`,
applying each with `request.Headers.TryAddWithoutValidation(name, value)`.

**Context:** `x-api-key` and `anthropic-version` are **not** typed/known HTTP header names, and
`request.Headers.Add(name, value)` throws `InvalidOperationException` for names it considers content headers or
validates unexpectedly. `TryAddWithoutValidation` is the standard way to attach an arbitrary custom request header
and never throws on the name. Applying in `SendWebRequest` (not the constructor) means every send carries the
headers, exactly as `EnsureAccept`/`EnsureAuthorization` already work.

**Consequence:** custom headers, `Accept`, and `Authorization` are independent — none clobbers another (a spec
asserts an added header coexists with bearer auth). The change touches one production file (`WebCaller.cs`): the
new interface member, the backing dictionary, the `AddHeader` method, and the `EnsureHeaders` call.

**Rejected:** `request.Headers.Add` (throws on custom/validated names — fragile for exactly the headers the
consumer needs); folding custom headers into `SetClient`'s `HttpClient.DefaultRequestHeaders` (per-instance
client, the socket-exhaustion path the consumer rejected).

### ADR-3 — Added headers **persist** on the instance (like `Accept`/Basic auth), not single-use like the bearer token

**Decision:** stored headers persist across sends on the same `WebCaller` instance until the instance is
discarded. Do **not** reset them after a send the way the bearer token is reset (line 325). Optionally add a
`ClearHeaders()` for symmetry with `ClearAuthorization` — but it is not required by the consumer and may be left
out to keep the surface minimal.

**Context:** the consumer builds a **fresh `IWebCaller` per request** (its ADR-3 — mirroring `OllamaTextEmbedder`
building its caller at call time), so single-use vs persistent makes no difference to it. Between the two,
persistent matches `Accept` and Basic auth (the common case for a header like a version or an API key that should
ride every call the caller makes), and is the least surprising. The bearer token's single-use reset is a special
case for a one-shot token, not the model to copy for a general header.

**Consequence:** a caller reused for several sends carries its added headers on all of them. If the human wants an
explicit reset, `ClearHeaders()` is a one-liner; whether to include it is a judgment call recorded here, not a
blocker.

**Rejected:** single-use headers reset after each send (surprising for a version/API-key header; forces a re-add
per call for no consumer benefit).

### ADR-4 — The new code path never logs header names or values

**Decision:** `AddHeader`/`EnsureHeaders` log **nothing** about the header value (and, to be safe, not the value
under any level). At most a content-free Debug line ("adding N request headers") — never the name→value pairs.

**Context:** the consumer's D27 forbids its Claude API key from reaching any log. The value passed to `AddHeader`
can be a secret (`x-api-key`). The toolkit already logs the **bearer token** value at Debug
(`EnsureAuthorization`, line 262) — a pre-existing behaviour this task does **not** fix but **flags** (Open
Questions) so a reviewer knows the new path deliberately does not repeat it.

**Consequence:** a secret custom header cannot leak through the new path regardless of the consumer's log level.

**Rejected:** logging the header name→value at Debug for parity with the bearer-token line (would leak the very
secret the consumer is protecting).

## Consumer compatibility

See `consumer-compatibility.md`. Summary: the change is **additive**; adding a member to `IWebCaller` breaks only
code that **implements** `IWebCaller` itself, and the requester (`C:\Code\Apostil`) does not — it injects the
interface and fakes it with FakeItEasy (which auto-implements new members). **`C:\Code\Apostil` cannot start its
`claude_backend` work item until this ships.**

## Publish flow (human-owned, after the phase completes)

1. Review the single commit; merge to `main` (your call how — the plan never pushes or merges).
2. From `src/`, run `PushNugetPackages.ps1` — the human picks the next version.
3. Bump `Api/Apostil.Api/Apostil.Api.csproj` in `C:\Code\Apostil` to the published version, build, and confirm
   `IWebCaller.AddHeader` resolves. **Only then does `run claude_backend` do anything** — its orchestrator checks
   for the arbitrary-header member first and **stops without touching the repository** if it is absent.
4. `Fog` needs nothing (re-verify it implements no `IWebCaller` of its own before publishing if any doubt — see
   `consumer-compatibility.md`).

## Assumptions

- The rules in `src/.claude/rules/csharp/*` govern this repo's C# (TDD, xUnit + FakeItEasy + FatCat.Testing with
  `.Not.` negation, CSharpier owns formatting, warnings are errors, block bodies only — including tests). **Match
  the repo's existing idiom; read `WebCaller.cs` and `WebCallerTests.cs` before writing.**
- Build/test entry points from `src/`: `dotnet build ToolKit.slnx` and `dotnet test ToolKit.slnx`.
- The task branch is `web-caller-request-headers`; the commit policy forbids working on `main`.
- **The WebCaller specs make real calls to `httpbin.org`** and depend on network access (existing behaviour — the
  suite already does this). The new spec follows suit. If httpbin is unreachable in the run environment, that is a
  pre-existing suite condition, not something this task introduces.
- `HttpMessageRequest.Headers.TryAddWithoutValidation(string, string)` is available (standard `System.Net.Http`).
  **No new package reference.**

## Open Questions

None blocking. Flagged for the human reviewer:

- **`ClearHeaders()` — include it or not?** (ADR-3.) The consumer does not need it (fresh caller per request).
  Included, it mirrors `ClearAuthorization`; omitted, the surface stays minimal. Your call; the phase can go either
  way — default to **omit** unless you want the symmetry.
- **Pre-existing: the bearer token is logged at Debug** (`WebCaller.EnsureAuthorization`, line 262). This task
  deliberately does **not** log header values (ADR-4), but the existing bearer-token Debug line is untouched and
  is a separate secret-in-logs concern you may want to address in its own change.
- **httpbin dependency in the suite.** The new spec asserts against a real echo from `httpbin.org`, consistent
  with every existing WebCaller spec. If you would prefer the toolkit not depend on an external service for this,
  that is a broader test-infrastructure decision beyond this task.
