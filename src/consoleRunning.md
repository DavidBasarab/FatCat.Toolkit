# Getting a Console Application Running with FatCat.Toolkit

This guide uses the `OneOffToolkitOnly` project as the reference template for a minimal console application backed by the toolkit.

---

## Project File

Create an `Exe` project targeting `net10.0`. Reference `ToolKit` at minimum. Add `Toolkit.WebServer` if you need the web server.

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>disable</Nullable>
        <LangVersion>default</LangVersion>
    </PropertyGroup>

    <PropertyGroup>
        <DisableImplicitSystemNetHttpPackage>true</DisableImplicitSystemNetHttpPackage>
        <NoWarn>$(NoWarn);NETSDK1206</NoWarn>
    </PropertyGroup>

    <ItemGroup>
        <RuntimeHostConfigurationOption Include="System.Runtime.Loader.UseRidGraph" Value="true"/>
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\ToolKit\ToolKit.csproj"/>
    </ItemGroup>

</Project>
```

---

## Program.cs

The entry point follows a consistent three-step pattern: configure logging, initialize the DI container, resolve and run a worker.

```csharp
using System.Reflection;
using Autofac;
using FatCat.Toolkit.Console;
using FatCat.Toolkit.Injection;

namespace MyConsoleApp;

public static class Program
{
    public static async Task Main(params string[] args)
    {
        await Task.CompletedTask;

        ConsoleLog.LogCallerInformation = true;

        try
        {
            SystemScope.Initialize(
                new ContainerBuilder(),
                new List<Assembly> { typeof(Program).Assembly, typeof(ConsoleLog).Assembly },
                ScopeOptions.SetLifetimeScope
            );

            var worker = SystemScope.Container.Resolve<MyWorker>();

            await worker.DoWork(args);
        }
        catch (Exception ex)
        {
            ConsoleLog.WriteException(ex);
        }
    }
}
```

### Key points

- `await Task.CompletedTask` at the top keeps the method signature async without forcing an immediate await.
- `ConsoleLog.LogCallerInformation = true` adds caller file/line info to every log line — useful during development.
- The `try/catch` at the top level catches anything that escapes the worker and writes it to the console via `ConsoleLog.WriteException`.

---

## SystemScope.Initialize

```csharp
SystemScope.Initialize(
    new ContainerBuilder(),
    new List<Assembly> { typeof(Program).Assembly, typeof(ConsoleLog).Assembly },
    ScopeOptions.SetLifetimeScope
);
```

| Parameter | Purpose |
|---|---|
| `new ContainerBuilder()` | The Autofac builder. Always pass a fresh instance. |
| `List<Assembly>` | Every assembly whose types should be registered. Include the toolkit assembly via `typeof(ConsoleLog).Assembly` and your own via `typeof(Program).Assembly`. Add a lib assembly via its module or any type in that assembly. |
| `ScopeOptions.SetLifetimeScope` | Builds the container and sets `LifetimeScope` so `SystemScope.Container.Resolve<T>()` works. Always pass this for console apps. |

Autofac scans each assembly and registers all public classes that implement a public interface. No manual registration is needed for the default one-to-one case. See `types-and-di.md` for when a `Module` is required.

---

## Writing a Worker

The convention is a class with a `DoWork` method. No base class or interface is required — just a plain class that the container can resolve.

```csharp
namespace MyConsoleApp;

public class MyWorker(IConsoleUtilities consoleUtilities)
{
    public async Task DoWork(string[] args)
    {
        ConsoleLog.WriteGreen("Worker started.");

        // do your work here

        consoleUtilities.WaitForExit();
    }
}
```

`IConsoleUtilities.WaitForExit()` blocks until the user presses a key or the process receives a termination signal. Use it for long-running apps that should stay alive after the initial setup.

---

## ConsoleLog

`ConsoleLog` is a static logger for console output. It does not require injection.

```csharp
ConsoleLog.Write("plain white");
ConsoleLog.WriteGreen("success");
ConsoleLog.WriteCyan("info");
ConsoleLog.WriteYellow("warning");
ConsoleLog.WriteMagenta("highlight");
ConsoleLog.WriteException(ex);
```

For permanent application logging inject `ILogger` instead. `ConsoleLog` is always available and does not need the container to be initialized first.

---

## Adding a Library Assembly

If your worker logic lives in a separate lib project, include that assembly in the `Initialize` call. The simplest anchor is any type in that assembly — the module class is a good choice if one exists.

```csharp
SystemScope.Initialize(
    new ContainerBuilder(),
    new List<Assembly>
    {
        typeof(Program).Assembly,
        typeof(ConsoleLog).Assembly,
        typeof(MyLibModule).Assembly,   // pulls in MyLib types
    },
    ScopeOptions.SetLifetimeScope
);
```

---

## Long-Running App Pattern

For apps that start background work and then wait:

```csharp
var consoleUtilities = SystemScope.Container.Resolve<IConsoleUtilities>();
var worker = SystemScope.Container.Resolve<MyBackgroundWorker>();

worker.Start();

consoleUtilities.WaitForExit();
```

`WaitForExit` keeps the process alive. `MyBackgroundWorker.Start` kicks off the background task without blocking.

---

## Reference Projects

| Project | What it shows |
|---|---|
| `OneOffToolkitOnly` | Minimal toolkit-only console app — the base template |
| `OneOff` | Console app that also starts the web server via `ServerWorker` |
| `ProxySpike` | Console app with `CommandLineParser` for subcommand routing and `IThread` for background workers |
