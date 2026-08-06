# Naming & Structure

## Core Philosophy
- Follow Clean Code principles (Robert C. Martin) and SOLID.
- Methods do one thing. Classes have one responsibility.
- Code reads like prose. Names make intent obvious without reading the implementation.
- Prefer interfaces and polymorphism over if/switch chains.
- Do NOT over-engineer. Do NOT introduce abstractions that do not already exist in this codebase.
- Match the abstraction level and style of the surrounding code.

## This Is a Published Library
`FatCat.Toolkit` and `FatCat.Toolkit.WebServer` ship as NuGet packages that other products consume. Every public type, member, and signature is a promise to a caller you cannot see.

- Treat public API changes as breaking until proven otherwise. Renaming a public type or changing a public signature breaks consumers at compile time.
- Prefer adding an overload over changing an existing one.
- Anything that only the toolkit needs should not be public — but note that `public` remains the default for genuine API surface (see `types-and-di.md`).
- New capabilities belong in the namespace of the feature area they extend, not in a new top-level namespace invented for one class.

## Naming Rules
- Avoid abbreviations. Prefer full words so readers never have to guess meaning.
- Acceptable abbreviations: widely recognized acronyms (e.g. `HTTP`, `URL`, `ID`) and any abbreviation that appears among the top 3 Google results for that term. When in doubt, use the full word.
- Names reveal intent. A method name makes it unnecessary to read the body.
- No comments explaining what code does — rename until it is obvious.
- PascalCase: classes, interfaces, methods, properties, constants
- camelCase: local variables, parameters, private fields — no leading underscore
- Private fields prefer `readonly` for dependencies where applicable
- Boolean names read as questions or states: `isReady`, `hasOutputs`, `canRestore`
- String interpolation required — never string concatenation with `+`
- Do NOT suffix method names with `Async` just because they return a `Task`. Name the method after what it does: `Save`, not `SaveAsync`. Only use the `Async` suffix when a non-async overload with the same name already exists and both must coexist.

## Discards
- Use `_` to discard outputs you intentionally do not need — `out _` for ignored out parameters, `using var _ = ...` for disposables acquired only for their side effect.

## Method Size
- Methods should be as short as possible.
- ~10 lines is a signal to evaluate refactoring — not an automatic rule.
- No method should require a comment to explain what it does. Refactor or rename instead.

## Spacing
- Leave a blank line between method definitions.
- Leave a blank line after variable declarations in a method before logic begins.
- Leave a blank line before return statements.

## Control Flow
- Avoid deep if/else nesting. Prefer guard clauses and early returns to keep the main flow readable.
- Avoid complex nested ternary expressions — prefer clear `if` statements or extract into a well-named method.
- If you need to explain what code does with a comment, first ask whether a better name makes the comment unnecessary.
- Use switch expressions (not if/else chains) when branching on an enum or type. Always include a discard arm `_` that throws `ArgumentOutOfRangeException` for unhandled cases:

```csharp
// Correct — switch expression
var result = logLevel switch
{
    LogLevel.Debug => WriteDebug(message),
    LogLevel.Error => WriteError(message),
    _ => throw new ArgumentOutOfRangeException(nameof(logLevel)),
};

// Wrong — if/else chain
if (logLevel == LogLevel.Debug) result = WriteDebug(message);
else if (logLevel == LogLevel.Error) result = WriteError(message);
```

## Files & Namespaces
- One class per file. File named after the class, never the interface.
- When a class directly implements a single interface, the interface and class live in the same file — named after the class. Do not create a separate file for the interface. This is the dominant pattern in this codebase (`Thread.cs` holds `IThread` + `Thread`, `Generator.cs` holds `IGenerator` + `Generator`, `MongoRepository.cs` holds `IMongoRepository<T>` + `MongoRepository<T>`).
- Only create a standalone interface file when the interface has multiple implementations or is consumed without a single obvious implementation (`ICacheItem.cs`).
- Namespace must exactly match the folder path within the project. No exceptions.
- Production namespaces start with `FatCat.Toolkit.*` — `FatCat.Toolkit.Caching`, `FatCat.Toolkit.Data.Mongo`, `FatCat.Toolkit.Threading`, `FatCat.Toolkit.WebServer.SignalR`. The `ToolKit` project's root namespace is `FatCat.Toolkit`; the `Toolkit.WebServer` project's is `FatCat.Toolkit.WebServer`.
- The test project mirrors the source project: same folder structure, same namespace with `Tests.` prepended — `FatCat.Toolkit.Data.Mongo` → `Tests.FatCat.Toolkit.Data.Mongo`.
- Always use file-scoped namespaces. Never use block-style `namespace X { }`.

```csharp
// Correct — file-scoped
namespace FatCat.Toolkit.Caching;

public class FatCatCache<T> { }

// Wrong — block-scoped
namespace FatCat.Toolkit.Caching
{
    public class FatCatCache<T> { }
}
```

## Project Layout
| Project | Assembly | Role |
|---|---|---|
| `ToolKit` | `FatCat.Toolkit` | The core library — caching, cryptography, data, threading, messaging, JSON, logging, web client. Published to NuGet. |
| `Toolkit.WebServer` | `FatCat.Toolkit.WebServer` | ASP.NET Core server side — `Endpoint`, `WebResult`, SignalR hubs, token handling. Published to NuGet. |
| `Tests.ToolKit` | `Tests.FatCat.Toolkit` | The single test project covering both libraries. |
| `OneOff`, `OneOffLib`, `OneOffToolkitOnly`, `OneOffBlazor`, `ProxySpike`, `SampleDocker` | — | Sample and spike hosts used to exercise the toolkit by hand. Not shipped; not held to TDD. |

Inside `ToolKit`, a folder is a feature area (`Caching/`, `Cryptography/`, `Data/Mongo/`, `Threading/`, `Web/Api/`). Put a new type in the existing feature folder it belongs to. Do not create a new top-level folder for a single class.

## Type Suffix Conventions
The codebase uses a consistent vocabulary of type-role suffixes. Pick the existing suffix for the role — do not invent new ones.

| Suffix | Role | Base type |
|---|---|---|
| `*Data` | Persisted Mongo entity | `MongoObject` (which extends `DataObject`) |
| `*CacheItem` | Item stored in an `IFatCatCache<T>` | `ICacheItem` (with `CacheId`) |
| `*Endpoint` | Web endpoint (route handler) | `Endpoint` |
| `*Repository` | Persistence abstraction | `IDataRepository<T>` / `IMongoRepository<T>` |
| `*Tools` | Stateless helper facade over a subsystem | plain class + `I*Tools` interface |
| `*Extensions` | Static extension-method holder | static class |
| `*Module` | Autofac registration module | `Autofac.Module` |
| `*Assertions` | Test assertion helper | `ComparerBase<T, TAssertions>` |

Value objects that need structural equality derive from `EqualObject`.

## Endpoint Pattern

Endpoints inherit from `Endpoint` (from `FatCat.Toolkit.WebServer`) and return `WebResult`.

1. **Return `WebResult`.** All endpoint action methods return `WebResult` or `Task<WebResult>`. Never return raw ASP.NET Core types (`IActionResult`, `Ok<T>()`, etc.). Use the inherited helpers (`Ok(...)`, `BadRequest(...)`, `NotFound()`, etc.) to build the result.

2. **Route via attribute.** Annotate each action with `[HttpGet]` / `[HttpPost]` / `[HttpPut]` / `[HttpDelete]` and an explicit `"api/..."` route (e.g. `[HttpPost("api/Session")]`).

3. **Interface only when reused.** An endpoint does not need an interface by default. Only add one when another part of the codebase needs to call the endpoint's logic directly. When an interface is needed, define it in the same file immediately above the class:

```csharp
// Only add this when something else needs to call the endpoint's logic directly
public interface IChangeSessionStatus
{
    Task<WebResult> Change(ChangeSessionStatusRequest request);
}

public class ChangeSessionStatusEndpoint(IMongoRepository<SessionData> repository)
    : Endpoint, IChangeSessionStatus
{
    ...
}
```

If the endpoint is only ever called over HTTP and nothing injects `IChangeSessionStatus`, no interface is needed.

4. **Mutable state fields for request context.** When an endpoint breaks its logic into multiple private helper methods, it may use non-`readonly` private fields to share working state across those methods within a single request. These fields are intentionally mutable and are not injected — they are populated during the request:

```csharp
public class CreateSessionEndpoint(
    IMongoRepository<SessionData> repository,
    IDateTimeUtilities dateTimeUtilities
) : Endpoint
{
    private SessionData session;   // request working state — intentionally NOT readonly

    [HttpPost("api/Session")]
    public async Task<WebResult> CreateSession([FromBody] CreateSessionRequest request)
    {
        session = new SessionData { CreatedOn = dateTimeUtilities.UtcNow() };
        session = await repository.Create(session);

        return Ok(session);
    }
}
```

This pattern avoids passing many parameters between helper methods. It is only valid within an endpoint class where the lifetime of the object is a single HTTP request.

## Interfaces
- All interfaces use the `I` prefix.
- Interface names describe a capability or action: `IThread`, `IGenerator`, `ISimpleLogger`, `IDateTimeUtilities`, `IMongoConnectionInformation`.
- NOT: `IThreadService`, `IGeneratorHelper`, `ILoggingService` — these describe what something is, not what it does.
- Default to narrow, single-purpose interfaces. One interface = one capability.
- Exception: highly cohesive groups (e.g. all operations against one subsystem, like `IFileSystemTools` or `IMongoRepository<T>`) may be grouped.
- All cross-boundary dependencies must be interfaces: threading, file system, time, external processes, HTTP clients, the Mongo driver.
- If something cannot be faked in a test, it is not properly abstracted. Being the toolkit that *supplies* these abstractions to consumers is exactly why this rule is not negotiable here.
