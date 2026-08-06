# Error Handling & Logging

## Error Handling
- Exceptions are for unplanned, unexpected failures only (hardware failures, network timeouts, corrupted state).
- Never throw an exception for a predictable outcome (validation failure, value out of range, known bad state).
- For known failure modes, return a value — an enum is preferred.
- Let exceptions bubble to the boundary where they can be meaningfully handled.
- Do not catch and swallow exceptions silently. The one exception: if a failure is genuinely non-actionable (e.g. a socket error on disconnect, a reflection comparison on an incompatible type), an empty catch with a `// ignored` comment is acceptable. This must be rare and deliberate — never use it to hide logic errors.
- "Log and rethrow at the boundary" is allowed at a top-level entry point (an endpoint action, a hub handler, a background loop). Do not log-and-rethrow at every layer — pick one boundary.

```csharp
// Preferred for known failures:
public enum ConnectResult { Success, UnknownHost, InvalidCredentials }

public ConnectResult TryConnect(ConnectionInformation information)
{
    if (!hostIsKnown)          return ConnectResult.UnknownHost;
    if (!credentialsAreValid)  return ConnectResult.InvalidCredentials;
    OpenConnection();
    return ConnectResult.Success;
}
```

## Throwing From a Library
This code ships as a NuGet package, so an exception thrown here surfaces inside somebody else's application, often far from the call that caused it.

- Throw the framework exception that actually fits — `ArgumentNullException`, `ArgumentOutOfRangeException`, `InvalidOperationException`. Do not invent a toolkit exception type for a case a framework type already describes.
- Exception messages name the parameter or state that was wrong. The caller has no access to this source, so the message is the whole diagnostic.
- Never let an exception escape a background thread or fire-and-forget path unobserved — a consumer cannot catch what they never awaited.
- A method that can fail for an expected reason returns a result the caller can branch on. Reserve throwing for the genuinely exceptional.

## Logging
The toolkit ships its own logging — there is no Serilog dependency, and no Microsoft `ILogger` injection.

| Type | Namespace | Use for |
|---|---|---|
| `ISimpleLogger` / `SimpleLogger` | `FatCat.Toolkit.Logging` | Writes an `{ApplicationName}.log` file next to the executable. Inject `ISimpleLogger` for permanent, file-based logging. Levels: `Debug`, `Information`, `Warning`, `Error`. |
| `StaticSimpleLogger` | `FatCat.Toolkit.Logging` | The same file logging from a static context — entry points and startup paths that run before anything is injectable. |
| `IToolkitLogger` / `ToolkitLogger` | `FatCat.Toolkit.Logging` | Colour-coded console tracing of the toolkit's own internals, gated behind `ToolkitLogger.Enabled` (off by default). Use for library-internal flow that a consumer may want to switch on while diagnosing. |
| `ConsoleLog` | `FatCat.Toolkit.Console` | Direct coloured console writes. Appropriate in the `OneOff*` / spike hosts and for genuine boot-time announcements — not for business logic. |

Rules:
- Prefer injecting `ISimpleLogger` over reaching for a static logger. Static logging is for code that has no container yet.
- Log at the action site, not at the boundary.
- Log thoughtfully — do not add log entries without a clear reason. A library that logs chattily pollutes every consumer's output.
- Anything left behind `ToolkitLogger.Enabled` must be worth a consumer switching on. If it was a scratch trace you added while diagnosing, remove it before merging.
- Never log secrets, tokens, connection strings, or encryption keys — this codebase handles all four.

## Logging and TDD
- Logging is the one area where strict TDD is not enforced.
- Do not block on log string test coverage — test critical entries, use judgment for the rest.
