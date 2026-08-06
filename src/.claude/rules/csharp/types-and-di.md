# Types & Dependency Injection

## var
- Use `var` as the default for local variable declarations.
- Small methods and good naming make the type obvious from context.
- Use explicit types only when the type is not clear from the right-hand side.

## Nullable Reference Types
- Both shipped projects set `<Nullable>disable</Nullable>`. Nullable reference annotations are **off by default** — do not add `?` to reference types out of habit, and do not add `null!` to silence a warning that does not exist.
- A file may opt in with `#nullable enable` at the top when the API genuinely needs to express nullability (`SimpleLogger.cs`, `DataModule.cs`, and others do this). When you opt a file in, annotate it honestly and completely — a half-annotated file is worse than an unannotated one.
- Inside a `#nullable enable` file: use `?` only where a value is genuinely optional (generic return types, reflection code, extension methods designed to accept null input). Never annotate injected dependencies or values that are always populated.
- Do not switch an existing file's nullable state as a drive-by change.

## Collection Initialization
- Use collection expressions (`[]`) to initialize collections. Do not use `new List<T>()`, `new T[0]`, or `new Dictionary<K, V>()` when an empty or inline-populated collection is needed.
- The target type drives the actual collection — `List<string> Names { get; set; } = [];` produces an empty list, just like `new List<string>()`, but is shorter and consistent.

```csharp
// Correct — collection expressions
public List<string> Names { get; set; } = [];
public byte[] Payload { get; set; } = [];
List<Task> pending = [firstTask, secondTask];

// Wrong — explicit constructor calls
public List<string> Names { get; set; } = new List<string>();
public byte[] Payload { get; set; } = new byte[0];
var pending = new List<Task> { firstTask, secondTask };
```

This applies to property initializers, field initializers, local variables, and method arguments. The exception is when you need a specific concrete type that the target cannot infer (e.g. assigning to `IEnumerable<T>` and needing a `HashSet<T>` specifically) — in that case, name the type explicitly.

## Thread-Safe Collections
- Use `ConcurrentDictionary<TKey, TValue>` for shared mutable state that is accessed across threads.
- Never use a plain `Dictionary` with manual locking for this purpose.
- This library is consumed by multi-threaded hosts (web servers, SignalR hubs, background queues). Shared state inside a `SingleInstance` type must be thread-safe — assume concurrent access unless the type is provably request-scoped.

## Lazy Initialization
- For thread-safe singleton initialization that must run its factory exactly once, use `Lazy<T>` with the factory constructor overload — this is the pattern `SystemScope` uses:

```csharp
private static readonly Lazy<SystemScope> instance = new(() => new SystemScope());
```

- For ordinary deferred initialization with no concurrency concern, the C# `field` keyword with null-coalescing assignment in a property getter is accepted:

```csharp
public IReadOnlyList<string> AllWords
{
    get { return field ??= LoadWords(); }
}
```

## Records — BANNED
- Records are banned. Use classes only.

## Access Modifiers
- Public is the default. Do not add access modifiers to restrict visibility unless there is a specific reason.
- Remember this is a published library — see the API-surface note in `naming-and-structure.md` before making something public that only the toolkit uses.
- `dotnet format` (via `.editorconfig`) enforces readonly and auto-properties — follow its guidance.

## Constructor Injection Only
- All dependencies are injected via the constructor. No property injection. No setter injection.
- Use primary constructors as the standard form for all new code. Do not write explicit constructor bodies with `this.field = param` assignments.
- Never use `new` inside a class to instantiate a dependency — ask for it via the constructor.

```csharp
// Correct — primary constructor
public class Messenger(IThread thread, IJsonOperations jsonOperations, IGenerator generator) : IMessenger
{
    // thread, jsonOperations, generator are available directly
}

// Wrong — traditional explicit constructor
public class Messenger : IMessenger
{
    private readonly IThread thread;
    private readonly IJsonOperations jsonOperations;

    public Messenger(IThread thread, IJsonOperations jsonOperations)
    {
        this.thread = thread;
        this.jsonOperations = jsonOperations;
    }
}
```

## Autofac Module Registration
Dependency registration uses Autofac `Module` classes. Each feature area that needs non-default wiring has one `*Module : Module` class with a `Load(ContainerBuilder builder)` override — `DataModule`, `FileSystemModule`, `ToolkitWebServerModule`, `SignalRModule`, `ToolkitModule`.

### When to register in the module
Only add a registration when Autofac cannot resolve the type on its own. A single implementation of an interface is resolved automatically — no module entry is required for one-to-one mappings.

Add to the module when:
- There are multiple implementations of the same interface and a default must be chosen
- The type requires `.SingleInstance()` lifetime that cannot be inferred automatically
- The type requires a factory method or a pre-built instance (`RegisterInstance`)
- The type is an open generic requiring `RegisterGeneric`
- The type needs post-resolution initialization (`.OnActivated(...)`)

Do NOT add to the module when there is exactly one implementation of the interface in the container.

### Rules
- Always register as the interface: `builder.RegisterType<MyClass>().As<IMyCapability>()`
- Add `.SingleInstance()` only when the type is genuinely safe to share across all consumers — this is a library, and a wrongly-shared instance becomes a consumer's race condition
- Use `RegisterGeneric` for open generic types: `builder.RegisterGeneric(typeof(MongoRepository<>)).As(typeof(IMongoRepository<>))`
- Mark the module class `[ExcludeFromCodeCoverage]` (with a short `Justification`) when it contains no testable logic
- Do not register the concrete type without `.As<IInterface>()` unless there is an explicit reason
- Use `.OnActivated(handler)` only when a type needs initialization that genuinely cannot happen in its constructor. It is a temporal-coupling trap: the object exists in a half-built state between construction and activation, and it only runs when Autofac builds the object. Prefer constructor injection.

```csharp
public class DataModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Pre-built instance shared across the container
        builder
            .RegisterInstance(new MongoConnection(SystemScope.ContainerAssemblies.ToList()))
            .As<IMongoConnection>()
            .SingleInstance();

        // Open generic
        builder.RegisterGeneric(typeof(MongoRepository<>)).As(typeof(IMongoRepository<>));

        // Default implementation of an interface with more than one candidate
        builder.RegisterType<EnvironmentConnectionInformation>().As<IMongoConnectionInformation>();
    }
}
```

Use `[UsedImplicitly]` on classes that are constructed by Autofac or reflection but have no compile-time references — this suppresses the unused-type warning.

## ISystemScope — Late Resolution
- `ISystemScope` (`FatCat.Toolkit.Injection`) wraps the Autofac `ILifetimeScope` and is the toolkit's own escape hatch for resolving a type at runtime — a dispatcher choosing a handler by inbound message type, a factory picking an implementation from configuration, or a background loop wanting a fresh scope per tick.
- Do NOT use `ISystemScope` as a service locator to dodge constructor injection. If a class always needs the same dependency, inject it directly.
- The static `SystemScope.Container` exists for entry points that run before any container-built object does. Do not reach for it from inside an injectable class.

## LINQ
- Use LINQ for querying and transforming collections. Prefer it over imperative loops.
- Always use method chaining syntax. Never use query syntax (`from x in y where...`).
- CSharpier handles formatting — write readable code and let it format.

## IThread — Threading Abstraction
- Threading and sleep operations use `IThread` (`FatCat.Toolkit.Threading`). Never use `Task.Delay`, `Thread.Sleep`, or raw `Thread` directly.
- `IThread` is injected via constructor like all other dependencies.
- `FakeThread` (`FatCat.Toolkit.Testing`) provides a synchronous substitute for unit tests — see `testing.md`.
- This applies to the toolkit's own code first. Shipping a `Thread.Sleep` inside the library takes the choice away from every consumer.

## IDateTimeUtilities — Time Abstraction
- Always read the current time via `IDateTimeUtilities.UtcNow()` (or `LocalNow()`) injected into the class. Never call `DateTime.UtcNow` or `DateTime.Now` in production code.
- The only acceptable direct uses are inside `DateTimeUtilities` itself (the wrapper), test data builders, and fake generators.
- Injecting the abstraction is what makes `Faker`-generated dates and time-sensitive assertions deterministic.

## TimeSpan — Humanizer Fluent Durations
- Express durations with Humanizer's fluent extensions, not `TimeSpan.From*` factory calls. Write `500.Milliseconds()`, `3.Seconds()`, `10.Minutes()`, `1.Days()` — never `TimeSpan.FromMilliseconds(500)`, `TimeSpan.FromSeconds(3)`, etc.
- Humanizer reads as prose and keeps duration literals consistent across the codebase. `Humanizer.Core` is already referenced by both shipped projects — add `using Humanizer;` where needed.
- This applies everywhere a `TimeSpan` is constructed from a constant: cache expirations, retry delays, `IThread.Sleep`, and closeness tolerances in tests.

```csharp
// Correct — Humanizer fluent durations
await thread.Sleep(3.Seconds());
cache.Add(item, 15.Minutes());

// Wrong — TimeSpan factory calls
await thread.Sleep(TimeSpan.FromSeconds(3));
cache.Add(item, TimeSpan.FromMinutes(15));
```

## Global Usings
The shipped projects (`ToolKit`, `Toolkit.WebServer`) do **not** use a `GlobalUsings.cs` — they have `ImplicitUsings` enabled and declare the rest per file. Do not add one. The test project does have one; see `testing.md`.

## C# 14 / .NET 10
- Both shipped projects target `net10.0` with `LangVersion` at the SDK default, so C# 14 features are available.
- The `field` keyword is accepted in property getters for backing-field initialization (`field ??= ...`).
- Extension blocks (`extension(TargetType target) { ... }`) are accepted for grouping multiple extension methods on the same type.
- Do not adopt a new language feature just because it exists — it must make the call site clearer than what it replaces, and it must not change the public API shape for consumers.

## What the Toolkit Already Provides
Before writing a new helper, check whether the toolkit already ships one — duplicating a capability inside the library that supplies it is the worst version of this mistake:
- `IFatCatCache<T>` (`FatCat.Toolkit.Caching`) — typed in-memory caches
- `IJsonOperations` (`FatCat.Toolkit.Json`) — JSON serialisation
- `IThread` / `FakeThread` (`FatCat.Toolkit.Threading`, `FatCat.Toolkit.Testing`) — threading abstraction
- `IGenerator` (`FatCat.Toolkit`) — id and value generation
- `IDateTimeUtilities` (`FatCat.Toolkit`) — time abstraction
- `IMongoRepository<T>` (`FatCat.Toolkit.Data.Mongo`) — Mongo persistence
- `IFileSystemTools` (`FatCat.Toolkit`) — file system access over `System.IO.Abstractions`
- `ISimpleLogger` (`FatCat.Toolkit.Logging`) — file logging; `ConsoleLog` (`FatCat.Toolkit.Console`) for console writes
- `ISystemScope` (`FatCat.Toolkit.Injection`) — runtime resolution
- `Faker.Create<T>()` (`FatCat.Fakes`) — random test data generation
