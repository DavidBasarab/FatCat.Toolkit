# web-caller-cancellation (FatCat.Toolkit)

> **Origin:** raised by a consumer — `C:\Code\Apostil`, security review
> `vulnerabilities/unresolved/2026-11-16-141444-curate-to-fix.md` finding 1 (**High**), while closing that
> repository's `corrections_authoring` plan.
> **Status:** done (2026-11-16), branch `web-caller-cancel`. See **Outcome** at the end.
>
> **Blocking:** Apostil's `corrections_authoring` phase 11
> (`tasks/todo/corrections_authoring/11-ask-and-transport-hardening.md`) cannot start until this ships. It is
> the only toolkit change Apostil is waiting on (see **Related** below). Apostil's other security phases (8–10)
> do not depend on it.

## Problem

`IWebCaller` has no way to cancel a request from the caller's side. Every overload takes at most a `TimeSpan`,
and `SendWebRequest` builds its own token from it:

```csharp
// src/ToolKit/Web/WebCaller.cs (v1.0.357)
using var tokenSource = new CancellationTokenSource(timeout);
...
var response = await httpClient.SendAsync(requestMessage, tokenSource.Token);
...
catch (TaskCanceledException)
{
	logger.Debug($"Request to {requestUri} timed out");

	return FatWebResponse.Timeout();
}
```

Two consequences for a consumer that is itself serving a request:

1. **An abandoned inbound request cannot stop the outbound call.** Apostil's anonymous `POST api/ask` passes
   `HttpContext.RequestAborted` down to its language model, but the model can only call
   `webCaller.Post("v1/messages", request, GenerationTimeout)`. A visitor who drops the connection leaves a
   generation call of up to 120 s (Claude) or 240 s (Ollama) running to completion. Looped, that is an
   unauthenticated denial of wallet on a paid model, or GPU exhaustion on a local one.
2. **Cancellation and timeout are indistinguishable.** Even if a caller's token were linked in, the
   `catch (TaskCanceledException)` arm would turn a caller's cancellation into `FatWebResponse.Timeout()`, so the
   consumer would treat "the visitor left" as "the model timed out" and carry on (log a timeout, push a failure
   to a client that is gone).

## Proposed change

### 1. Overloads that take a `CancellationToken`

Add, to `IWebCaller` and `WebCaller`, an overload with a trailing `CancellationToken cancellationToken` for every
overload that already takes a `TimeSpan timeout`:

- `Get(string url, TimeSpan timeout, CancellationToken cancellationToken)`
- `Delete(string url, TimeSpan timeout, CancellationToken cancellationToken)`
- `Post<T>(string url, T data, TimeSpan timeout, CancellationToken cancellationToken)` — **the one Apostil needs**
- `Post<T>(string url, List<T> data, TimeSpan timeout, CancellationToken cancellationToken)`
- `Post(string url, TimeSpan timeout, CancellationToken cancellationToken)`
- `Post(string url, string data, TimeSpan timeout, string contentType, CancellationToken cancellationToken)`
- the matching `Put` overloads

The existing overloads keep their signatures and delegate with `CancellationToken.None`, so their behaviour is
byte-for-byte unchanged. If the full set is more surface than wanted, the minimum that unblocks Apostil is the
`Post<T>(url, T data, TimeSpan timeout, CancellationToken)` overload — but a partial set will be asked for again
by the next consumer, so the full, symmetric set is recommended.

### 2. `SendWebRequest` links the caller's token and tells the two apart

```csharp
using var timeoutSource = new CancellationTokenSource(timeout);
using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token, cancellationToken);

try
{
	var response = await httpClient.SendAsync(requestMessage, linkedSource.Token);
	...
}
catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
{
	throw;                               // the caller cancelled: propagate, never a Timeout response
}
catch (TaskCanceledException)
{
	return FatWebResponse.Timeout();     // unchanged: only the timeout lands here now
}
```

- **Caller cancelled → `OperationCanceledException` propagates** (the caller's own token, so
  `exception.CancellationToken == cancellationToken`). This is the .NET convention, and it is what lets a consumer's
  `catch (OperationCanceledException)` tell "the client left" from a failure.
- **Timeout → `FatWebResponse.Timeout()`, exactly as today.**
- `HttpClient.SendAsync` aborts the socket when the token fires, so the outbound request really stops on the
  toolkit's side. (Whether the remote service stops work on a dropped connection is that service's behaviour:
  Ollama stops generating; a hosted model API may still bill a request it already accepted. A consumer still needs
  rate limiting — this change makes abandonment free on the consumer's side, not on the provider's.)

### Specs (test-first)

In `src/Tests.ToolKit/Web/Api/WebCallerSpecs/` (the `FakeHttpBin` already has a `WaitForCancellation` path):

- For a representative overload of each verb: a request whose caller token is cancelled mid-flight **throws
  `OperationCanceledException`** carrying that token.
- The same request **does not return `FatWebResponse.Timeout()`** when the caller cancelled.
- A request that exceeds its `timeout` with an uncancelled caller token **still returns
  `FatWebResponse.Timeout()`** (the existing behaviour, pinned).
- An already-cancelled token throws without sending (no request reaches the handler).
- Each old overload still behaves as before (delegates with `CancellationToken.None`).

## Compatibility

Additive: new interface members only break code that **implements** `IWebCaller`. Neither known consumer does
(verified for `web-caller-request-headers` on 2026-08-29: Apostil and Fog both inject `IWebCaller` and fake it
with FakeItEasy, which auto-implements new members). Re-verify with
`grep -rn ":\s*IWebCaller\b|,\s*IWebCaller\b"` in both repositories before publishing.

The only behavioural change is on the new overloads; the old ones pass `CancellationToken.None`, so the new
`catch ... when` arm can never match for them. Bump the patch version.

**What Apostil does with it:** bump `FatCat.Toolkit.WebServer` in `Api/Apostil.Api/Apostil.Api.csproj`, then
pass the generation `CancellationToken` through in `ClaudeLanguageModel.RequestCompletion` and
`OllamaLanguageModel` (and the embedder, if it adopts it), and let `OperationCanceledException` propagate —
`QuestionAnswerer` already rethrows it.

## Related

- **`rate-limiting-hook`** (`src/task/todo/rate-limiting-hook/`) is the other half of Apostil's finding 1 and all
  of its finding 5 (no rate limit on `api/ask`, `api/session`, `api/authentication/login`). Its phases 1 and 2
  (`ConfigureServices`, `ConfigureRoutedMiddleware`) **have already shipped** in 1.0.354, which Apostil
  references, so Apostil can register `AddRateLimiter` with named per-endpoint policies today. That folder is
  still under `todo/` only because phase 3 (query-string redaction) is outstanding; consider moving phases 1–2's
  record to `done/`.
- **Also noticed, not in scope:** `SendWebRequest` logs the entire request body at `Debug`
  (`logger.Debug(data)`). For Apostil that body is a prompt containing a visitor's question. `ToolkitLogger` is off
  by default and Apostil never enables it, so nothing leaks today, but logging only the length (already logged on
  the line above) would make that safe by construction. Likewise the hub client logs whole payloads when
  `ToolkitLogger.Enabled` is on. Worth a separate small task.

## Outcome (2026-11-16)

- The full symmetric set shipped: 10 new `IWebCaller` / `WebCaller` overloads (`Get`, `Delete`, and 4 each of `Post` / `Put`).
  The old timeout overloads delegate with `CancellationToken.None`; the `Timeout`-property overloads call
  `SendWebRequest` with `CancellationToken.None` directly.
- **Deviation from the sketch above:** the caller-cancelled arm does **not** `throw;`. `HttpClient` rethrows with its
  own linked token, so a bare rethrow would carry the linked token and not the caller's. The arm rethrows
  `new OperationCanceledException(exception.Message, exception, cancellationToken)` so that
  `exception.CancellationToken == cancellationToken` holds. A mutation run confirmed the spec fails with the bare rethrow.
- `SendWebRequest` calls `cancellationToken.ThrowIfCancellationRequested()` first, so an already-cancelled token never
  reaches the handler.
- Specs: `WebCallerTests` (run for all four verbs) now covers mid-flight cancel throwing `OperationCanceledException`,
  the exception carrying the caller's token (mid-flight and pre-cancelled), no request sent when the token is already
  cancelled, timeout still returning `FatWebResponse.Timeout()` with a live token, and success with a live token.
  `PostTests` / `PutTests` cover each remaining new overload: it sends its data and forwards the token.
  `FakeHttpBin` gained `RequestCount` and `DelayStarted`.
- README: new "Cancelling a Request" subsection under the HTTP Client section.
- Consumer re-verify (2026-11-16): the implementer grep across `C:\Code\Apostil`, `C:\Code\Fog` and `C:\Code\Fog-wt`
  found no class implementing `IWebCaller` (only an ADR comment in Apostil's end-to-end helpers).
- **Not done here:** the patch version bump, a deliberate release step.
