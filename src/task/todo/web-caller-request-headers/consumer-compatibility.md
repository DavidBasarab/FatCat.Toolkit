# web-caller-request-headers — Consumer compatibility

Verification for both consuming repositories, taken from their source on **2026-08-29** rather than assumed.

## Summary

| Repo | Implements `IWebCaller` itself? | Verdict |
|---|---|---|
| `C:\Code\Apostil` | **No** — every reference injects it; the only textual match is a code comment | **Safe. It is the requester**, and its `claude_backend` work item is blocked until this ships. |
| `C:\Code\Fog` | **No** — no `: IWebCaller` implementation found | **Unaffected.** No code change required; its version gap deserves its own build-and-smoke pass. |

**The change is additive.** Adding a member to an interface only breaks code that *implements* it. Neither
consumer implements `IWebCaller` — both inject it and (in tests) fake it with FakeItEasy, which **auto-implements
new interface members**, so a new `AddHeader` breaks no fake either.

## `C:\Code\Apostil`

`grep -rn ":\s*IWebCaller\b|,\s*IWebCaller\b"` across `Apostil.Api`, `Apostil.Common`, `Apostil.Site`, and the
test/end-to-end projects returns **no implementation** — the sole match is a comment in
`EndToEnd/Tests.Apostil.EndToEnd/Helpers/ApostilSystemAccess.cs` ("*IWebCaller has no multipart overload…*").
Every real use injects `IWebCaller` (via `IWebCallerFactory`) and, in unit tests, fakes it with
`A.Fake<IWebCallerFactory>()` / a faked `IWebCaller`. Adding `AddHeader` to the interface changes none of that.

**What Apostil gets, and why it asked:** its `claude_backend` work item adds a Claude (Anthropic) generation
backend over `IWebCallerFactory` (mirroring `OllamaTextEmbedder`). The Anthropic Messages API requires the
`x-api-key` and `anthropic-version` request headers, which `IWebCaller` cannot send today. `AddHeader` is the one
capability that unblocks it — and its `ClaudeLanguageModelSpecs` will assert the calls on the FakeItEasy fake
(`A.CallTo(() => webCaller.AddHeader("x-api-key", key)).MustHaveHappened()`), which only exists because `AddHeader`
is on the interface.

**What Apostil must do:** bump `Api/Apostil.Api/Apostil.Api.csproj` from its current `FatCat.Toolkit.WebServer`
version to whatever the human publishes this change as. Nothing else — its new code is written against `AddHeader`
from the start.

**Blocking relationship, stated plainly:** `tasks/todo/claude_backend/` in that repository declares this a **hard
precondition** (its ADR-8). Its orchestrator's *Toolkit gate* reads `WebCaller.cs` (or the referenced package's
`IWebCaller`) for a per-request arbitrary-header method **before phase 1** and **stops without touching the
repository** if it is absent.

## `C:\Code\Fog`

`grep -rn ":\s*IWebCaller\b|,\s*IWebCaller\b"` across Fog returns **no matches** — Fog implements no `IWebCaller`
of its own. It neither sends custom headers today nor is affected by the new member; it gains nothing and loses
nothing.

**Version gap.** Fog trails Apostil's toolkit version and would span several intervening work items on any
upgrade. Non-breaking on paper — and the caveat every prior toolkit task recorded still applies: **Fog's upgrade
deserves its own build-and-smoke pass as a Fog-side task**, not an assumption made from this repository.
Re-verify Fog's `IWebCaller`-implementation count before publishing if any doubt remains.

## The one way this could break a consumer

**A consumer that implements `IWebCaller` in its own code** — a bespoke web caller, a hand-rolled test double, a
decorator — stops compiling until it implements `AddHeader`. **Neither consumer does.** A third consumer that did
would fail at compile time, loudly, which is the failure mode to prefer over a silent runtime surprise.

Everything else is preserved by construction: no existing signature, body, or return value changes; `Accept`, the
Basic/Bearer auth setters, `ClearAuthorization`, and every Get/Post/Put/Delete overload behave exactly as before;
and the added headers are applied in `SendWebRequest` alongside the existing `EnsureAccept`/`EnsureAuthorization`
without touching either.
