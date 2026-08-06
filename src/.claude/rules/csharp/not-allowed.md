# What NOT to Do

These are hard stops. Do not do any of the following under any circumstances.

## Type System
- Do NOT add nullable annotations (`?`) or `null!` in files that are not `#nullable enable` — the shipped projects have nullable disabled, so the annotation is noise
- Do NOT flip a file's `#nullable` state as a drive-by change
- Do NOT use records — use classes only

## Async
- Do NOT use `async void` — always return `Task` or `Task<T>`
- Do NOT use `ConfigureAwait(false)` — we do not use it
- Do NOT block on tasks with `.Result` or `.Wait()`
- Do NOT use `Task` or `Thread` directly for threading — use `IThread`. No `Task.Delay`, no `Thread.Sleep`, no `new Thread(...)`
- Do NOT call `DateTime.UtcNow` or `DateTime.Now` in production code — inject `IDateTimeUtilities` and call `dateTimeUtilities.UtcNow()`

## Code Style
- Do NOT use expression-bodied members (`=>` syntax for methods or properties) — this applies to ALL access levels (public, private, protected, internal) and ALL projects including the test project
- Do NOT use query syntax LINQ (`from x in y where...`) — method chaining only
- Do NOT use string concatenation with `+` — use string interpolation. Write `$"Some string with data {theData}"`, never `"Some string with data " + theData`. (No analyzer enforces this; it is caught by code review.)
- Do NOT abbreviate names — write them out fully
- Do NOT write comments explaining what code does — rename until obvious
- Do NOT use `new List<T>()`, `new T[0]`, or `new Dictionary<K, V>()` for empty or inline-populated collections — use collection expressions (`[]`)
- Do NOT construct durations with `TimeSpan.FromMilliseconds(...)`, `TimeSpan.FromSeconds(...)`, `TimeSpan.FromMinutes(...)`, etc. from a constant — use Humanizer fluent extensions (`500.Milliseconds()`, `3.Seconds()`, `10.Minutes()`)

## Architecture
- Do NOT use property injection or setter injection — constructor only
- Do NOT use `new` inside a class to instantiate a dependency
- Do NOT name a file after an interface — always name after the class
- Do NOT add abstractions or patterns that do not exist in the surrounding codebase
- Do NOT introduce over-engineering — match the abstraction level of the existing code
- Do NOT use `ISystemScope` (or the static `SystemScope.Container`) as a service locator to avoid constructor injection — only use it for genuine runtime resolution
- Do NOT add AutoMapper, Serilog, MediatR, or any other framework this repo does not already reference. A NuGet package added here becomes a transitive dependency for every consumer of the toolkit.

## Library API Surface
- Do NOT rename or change the signature of an existing public type or member as part of an unrelated change — it breaks consumers at compile time
- Do NOT make something public that only the toolkit needs
- Do NOT bump `VersionPrefix` as a side effect — versioning is a deliberate release step

## Errors & Logging
- Do NOT throw exceptions for predictable, known failure states — return an enum
- Do NOT swallow exceptions silently
- Do NOT inject `Microsoft.Extensions.Logging.ILogger` or add Serilog — use `ISimpleLogger` / `IToolkitLogger` from `FatCat.Toolkit.Logging`
- Do NOT use `ConsoleLog` or `ToolkitLogger` as a scratch debugger — if you add a temporary trace, remove it before merging
- Do NOT log secrets, tokens, connection strings, or encryption keys

## Testing
- Do NOT use FluentAssertions — assertions come from `FatCat.Testing`, and negation is `.Should().Not.X`, never `.Should().NotX()`
- Do NOT use `A<T>.Ignored` in FakeItEasy argument matchers — always use `A<T>._`
- Do NOT fake `IMongoRepository<T>` with `A.Fake<...>()` — use the concrete `MongoFakeRepository<T>`
- Do NOT write a unit test that touches a real database, file system, network, clock, or thread

## Formatting
- Do NOT manually fight CSharpier formatting — it is the final authority, including over the `csharp_*` keys in `.editorconfig`
- Do NOT silence a build warning by widening `NoWarn` — fix the warning
- Do NOT suppress `dotnet format` / analyzer / ReSharper warnings without a comment explaining why
