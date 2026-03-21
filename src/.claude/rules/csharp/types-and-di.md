# Types & Dependency Injection

## var
- Use `var` as the default for local variable declarations.
- Small methods and good naming make the type obvious from context.
- Use explicit types only when the type is not clear from the right-hand side.

## Nullable Reference Types — BANNED
- Nullable reference types (`?`) are banned. Do not use nullable annotations on any type.
- Do not write `string?`, `ILogger?`, `OutputContext?`, or any other nullable annotation.

## Records — BANNED
- Records are banned. Use classes only.

## Access Modifiers
- Public is the default. Do not add access modifiers to restrict visibility unless there is a specific reason.
- ReSharper enforces readonly and auto-properties — follow its guidance.

## Constructor Injection Only
- All dependencies are injected via the constructor. No property injection. No setter injection.
- Use primary constructors (C# 12+) as the standard form for all new code. Do not write explicit constructor bodies with `this.field = param` assignments.
- Never use `new` inside a class to instantiate a dependency — ask for it via the constructor.

```csharp
// Correct — primary constructor
public class FactoryResetEndpoint(IRunExecuteMacrium executeMacrium,
                                   IThread thread,
                                   ILogger logger) : HaivisionApiEndpoint
{
    // executeMacrium, thread, logger are available directly
}

// Wrong — traditional explicit constructor
public class FactoryResetEndpoint : HaivisionApiEndpoint
{
    private readonly IRunExecuteMacrium executeMacrium;
    private readonly IThread thread;

    public FactoryResetEndpoint(IRunExecuteMacrium executeMacrium, IThread thread)
    {
        this.executeMacrium = executeMacrium;
        this.thread = thread;
    }
}
```

## Autofac Module Registration
All dependency registration uses Autofac `Module` classes. Each service project has one `*Module : Module` class with a `Load(ContainerBuilder builder)` override.

### When to register in the module
Only add a registration to the module when there are **multiple implementations of the same interface** and you need to override the default. Autofac automatically resolves a single implementation of an interface — no module entry is required for one-to-one mappings.

Add to the module when:
- A service project provides its own implementation of an interface that already has a default implementation elsewhere (e.g. `IDoSomeAction` is implemented in Common as `DoSomeAction`, but this service needs `DoOtherAction` instead — register `DoOtherAction` in the module to override)
- The type requires `.SingleInstance()` lifetime that cannot be inferred automatically
- The type requires a factory method for construction (`.Factory` pattern)
- The type is an open generic requiring `RegisterGeneric`

Do NOT add to the module when:
- There is exactly one implementation of the interface in the container — Autofac resolves it automatically

### Rules
- Always register as the interface: `builder.RegisterType<MyClass>().As<IMyCapability>()`
- Add `.SingleInstance()` only when the type is genuinely stateless and safe to share across all requests
- Use `RegisterGeneric` for open generic types: `builder.RegisterGeneric(typeof(CreateOperation<,,>)).As(typeof(ICreateOperation<,,>))`
- Mark the module class `[ExcludeFromCodeCoverage]` — it contains no testable logic
- Do not register the concrete type without `.As<IInterface>()` unless there is an explicit reason
- For classes that require a factory method for construction, use a static `.Factory` method on the class and register it via `builder.Register(MyClass.Factory)`:

```csharp
// Common project — default implementation, resolved automatically (no module entry needed)
public class DoSomeAction : IDoSomeAction { ... }

// This service project — overrides the default; must be registered in the module
public class DoOtherAction : IDoSomeAction { ... }

[ExcludeFromCodeCoverage]
public class ManagerModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Required: overrides the Common default for IDoSomeAction
        builder.RegisterType<DoOtherAction>().As<IDoSomeAction>();

        // Required: SingleInstance lifetime cannot be inferred
        builder.RegisterType<PermissionGroup>().As<IPermissionGroup>().SingleInstance();

        // Required: open generic
        builder.RegisterGeneric(typeof(CreateOperation<,,>)).As(typeof(ICreateOperation<,,>));

        // Required: factory method construction
        builder.Register(CineAgentCache.Factory).SingleInstance();
    }
}
```

## AutoMapper — IConfigureMappings
Each service project has exactly one `*MapperInitialization` class that implements `IConfigureMappings`. It is the single place where all AutoMapper profiles for that service are configured.

Rules:
- Mark the class `[ExcludeFromCodeCoverage]` — it contains no testable logic
- Implement `SetMappings(IProfileExpression configuration)` as the single public method
- Organize mappings into private `Configure*` methods grouped by domain area
- Use `.ConvertUsing<TConverter>()` for complex type conversions that need their own class
- Use `[UsedImplicitly]` on event consumer classes that are invoked via reflection — this suppresses the ReSharper unused-type warning

```csharp
[ExcludeFromCodeCoverage]
public class ManagerMapperInitialization : IConfigureMappings
{
    public void SetMappings(IProfileExpression configuration)
    {
        ConfigureAssetMappings(configuration);
        ConfigureCanvasMappings(configuration);
    }

    private void ConfigureAssetMappings(IProfileExpression configuration)
    {
        configuration.CreateMap<AssetData, AssetServiceModel>();
        configuration.CreateMap<AssetRequest, AssetData>().ConvertUsing<AssetRequestConverter>();
    }

    private void ConfigureCanvasMappings(IProfileExpression configuration)
    {
        configuration.CreateMap<CanvasData, CanvasServiceModel>();
    }
}
```

## LINQ
- Use LINQ for querying and transforming collections. Prefer it over imperative loops.
- Always use method chaining syntax. Never use query syntax (`from x in y where...`).
- CSharpier handles formatting — write readable code and let it format.

## IThread — Threading Abstraction
- Threading and sleep operations use `IThread`. Never use `Task` or `Thread` directly.
- `IThread` is injected via constructor like all other dependencies.
- `FakeThread` provides a synchronous substitute for unit tests.
