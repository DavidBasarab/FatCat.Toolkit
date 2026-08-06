# Test-Driven Development

## TDD Is Non-Negotiable
- All production code is written test-first. No exceptions (other than logging).
- Tests define the contract. Implementation satisfies the tests.
- Tests are not written after the fact — they define behavior before implementation begins.
- This applies to `ToolKit` and `Toolkit.WebServer`. The `OneOff*`, `ProxySpike`, and `SampleDocker` hosts are hand-run samples and are not held to TDD.

## One Test, One Assertion
- Each test verifies exactly one thing.
- A failing test must tell you precisely what broke without investigation.
- Test names are verb phrases describing the expected behavior.

```csharp
[Fact] public void UseFileSystemToAppendText() { ... }
[Fact] public void DeleteDirectoryIfFound() { ... }
[Fact] public void ReturnOk() { ... }
```

## Test Stack
- Framework: xUnit
- Faking: FakeItEasy (`A.Fake<T>()`, `A.CallTo()`)
- Assertions: **FatCat.Testing** (`.Should()`, `.Be()`, `.BeEquivalentTo()`, `.BeTrue()`, `.BeLessThan()`). Negation is the `Not` property — `result.Should().Not.BeNull()`, never a `NotXxx` method. This codebase does **not** reference FluentAssertions.
- Thread substitute: `FakeThread` (`FatCat.Toolkit.Testing`) — runs `IThread` operations synchronously
- Test data: `Faker.Create<T>()` and `Faker.RandomString()` (`FatCat.Fakes`) — do not hard-code values
- Everything lives in one test project: `Tests.ToolKit`, assembly and root namespace `Tests.FatCat.Toolkit`

## The Test Project Is Also Documentation
`FatCat.Toolkit.Testing` and `FatCat.Toolkit.WebServer.Testing` are shipped, public API — `FakeThread`, `MongoFakeRepository<T>`, `FakeFatCatCache<T>`, `EasyCapture<T>`, and the assertion extensions exist so *consumers* can test against the toolkit. The tests in this repo are the reference for how those helpers are meant to be used. Use them the way a consumer would; do not reach around them with hand-rolled fakes.

## Test Class Layout — Abstract Base + Specs Folder
When a class under test has several methods worth their own scenarios, give it a `<Class>Specs` folder containing:
- An `abstract class <Class>Tests` holding all shared setup (fakes, the system under test, default fake configurations, helper `Verify*` methods)
- One concrete class per method-under-test (`GetAllTests`, `DeleteFileTests`, `AppendToFileTests`) deriving from that base and holding the `[Fact]` methods for that scenario

Setup uses `protected readonly` fields populated inline with `A.Fake<T>()` or `Faker.Create<T>()`. The system under test is constructed in the protected constructor of the abstract base:

```csharp
namespace Tests.FatCat.Toolkit.FileSystemToolsSpecs;

public abstract class FileToolsTests
{
    protected readonly string directoryPath = Faker.RandomString();
    protected readonly IFileSystem fileSystem = A.Fake<IFileSystem>();
    protected readonly FileSystemTools fileTools;

    protected bool directoryExists = true;

    protected FileToolsTests()
    {
        A.CallTo(() => fileSystem.Directory.Exists(directoryPath)).ReturnsLazily(() => directoryExists);

        fileTools = new FileSystemTools(fileSystem);
    }

    protected void VerifyDirectoryExistsWasCalled()
    {
        A.CallTo(() => fileSystem.Directory.Exists(directoryPath)).MustHaveHappened();
    }
}
```

There is no `BddBase` and no per-project base class — the abstract class is plain.

A single flat `<Class>Tests.cs` file is correct for small, stateless types where a folder would be ceremony: extension methods, comparers, and pure utilities (`ByteToolTests.cs`, `CollectionExtensionsTests.cs`, `GeneratorTests.cs`). The moment a class needs shared setup across more than one scenario, move it to a `<Class>Specs` folder.

## Global Usings
The test project has a single `GlobalUsings.cs`. Production projects do not use one.

```csharp
// Tests.ToolKit/GlobalUsings.cs
global using FakeItEasy;
global using FatCat.Fakes;
global using FatCat.Testing;
global using Xunit;
```

Add a namespace here only when it appears in nearly every test file.

## MongoFakeRepository — Use the Concrete Fake
For tests against code that depends on `IMongoRepository<T>`, use the concrete `MongoFakeRepository<T>` (`FatCat.Toolkit.Testing`) directly as a `protected readonly` field. Do NOT fake the interface with `A.Fake<IMongoRepository<T>>()` — the concrete fake carries state and captures that a hand-rolled fake loses.

```csharp
protected readonly MongoFakeRepository<UserData> mongo = new();

// Arrange — what queries return
mongo.Item = expectedUser;
mongo.Items = expectedUsers;

// Assert — what the code under test did
mongo.CreatedItem.Should().BeEquivalentTo(expectedUser);
mongo.UpdatedItem.Should().BeEquivalentTo(expectedUser);
mongo.DeletedItem.Should().Be(expectedUser);
mongo.CreatedCapture.Value.Should().BeEquivalentTo(expectedUser);
mongo.FilterCapture.Value.Should().Not.BeNull();
```

`EasyCapture<T>` (`CreatedCapture`, `UpdatedCapture`, `FilterCapture`) is how you assert on an argument the code passed in — reach for it instead of a `A.CallTo(...).Invokes(...)` closure.

## Endpoint Test Assertions
Endpoint tests verify both the HTTP shape and the result body:
- Shape: `endpoint.Should().BePost(nameof(CreateSessionEndpoint.CreateSession), "api/Session")` — `BeGet`, `BePost`, `BeDelete`, and the general `HaveHttpAttribute<T>` come from `FatCat.Toolkit.WebServer.Testing` and assert both the HTTP verb attribute and the route template.
- Result: `result.Should().BeOk().Be(expectedValue)` — `BeOk()` narrows a `WebResult` to a 200, then `.Be(...)` asserts the body. Other helpers: `.BeBadRequest()`, `.BeBadRequest(messageId)`, `.BeNotFound()`, `.BeUnauthorized()`, `.BeConflict()`, `.BeEmptyListOf<T>()`, `.BeSuccessful()`, `.BeUnsuccessful()`.
- `Should()` has an overload on `Task<WebResult>`, so an endpoint call can be asserted without a separate await.

## Test Method Naming — Verb-First
`[Fact]` methods are named as bare verb phrases describing the observable behaviour, with no `Should`, no underscores, no Given/When/Then:

```csharp
[Fact] public void BeAPost() { ... }
[Fact] public void ReturnTheCachedItem() { ... }
[Fact] public void CreateTheUserDataInMongo() { ... }
[Fact] public void GetUtcNow() { ... }
```

## Expression-Bodied Members in Tests — BANNED
The expression-bodied member ban applies to test code too. All test methods and constructors must use block bodies:

```csharp
// Wrong
[Fact]
public void ReturnOk() => result.Should().BeOk();

public MyTests() => sut = new MySut(fake);

// Correct
[Fact]
public void ReturnOk()
{
    result.Should().BeOk();
}

public MyTests()
{
    sut = new MySut(fake);
}
```

## Test Setup
- Place common setup in the test class constructor: create fakes, configure default return values, initialize the system under test.
- Keep constructor setup minimal and deterministic. Extract to helper methods if setup becomes large.

## FakeItEasy Patterns
- Use `A<T>._` for argument matchers. Never use `A<T>.Ignored` — they are equivalent, and `A<T>._` is the canonical form in this codebase.
- Use `Returns(...)` for static, unchanging responses.
- Use `ReturnsLazily(...)` when the return value needs to vary between tests:

```csharp
// In the abstract base constructor:
protected bool directoryExists = true;
A.CallTo(() => fileSystem.Directory.Exists(directoryPath)).ReturnsLazily(() => directoryExists);

// In a test — just set the field:
directoryExists = false;
```

- This avoids reconfiguring fakes per test and keeps each test focused on its scenario.
- Document any non-trivial fake behavior so future maintainers understand the intent.

## Test Project Conventions
- The abstract base class name = source class name + `Tests`, placed in a `<Class>Specs` folder. Concrete derived classes are named after the method under test (`GetAllTests`, `WriteAllTextTests`).
- Test namespace mirrors the source namespace with `Tests.` prepended: `FatCat.Toolkit.Data.Mongo` → `Tests.FatCat.Toolkit.Data.Mongo`.
- Example: `FatCat.Toolkit.FileSystemTools` → `Tests.FatCat.Toolkit.FileSystemToolsSpecs.FileToolsTests` (abstract base) plus `Tests.FatCat.Toolkit.FileSystemToolsSpecs.DeleteFileTests` (concrete `[Fact]` class).
- There is always a direct 1-to-1 correspondence between a class under test and its `<Class>Specs` folder (or its flat `<Class>Tests.cs`).

## Testing and IThread
- In tests, inject `FakeThread` instead of a real `IThread` implementation.
- This runs async/threaded operations synchronously, giving deterministic test results.
- You do not need to test that an action runs in a new thread — test the action itself.
- For testing sleep/delay behavior, use `IThread` and `FakeThread` directly.

## Unit Tests Are Deterministic and Offline
- No real database, file system, network, clock, or thread. Every one of those has an abstraction in this toolkit — use it.
- Time comes from `IDateTimeUtilities`; delays from `FakeThread`; the file system from `IFileSystem` (`System.IO.Abstractions`); Mongo from `MongoFakeRepository<T>`.
- Express closeness tolerances with Humanizer (`3.Seconds()`), never `TimeSpan.From*`.
- A test that reaches the network is a finding, not a slow test.

## Low-Level API Implementations — No Unit Tests Required
- Classes that talk directly to a low-level external system do not require unit tests.
- Examples: direct MongoDB driver calls, raw OS APIs, the `DateTimeUtilities` clock wrapper.
- These classes exist to satisfy an interface boundary — the interface is tested via fakes everywhere it is consumed.
- Mark the class with `[ExcludeFromCodeCoverage]` and a `Justification` that explains why.

```csharp
[ExcludeFromCodeCoverage(Justification = "A time wrapper")]
public class DateTimeUtilities : IDateTimeUtilities
{
    // ...
}
```

- The justification must be specific: name the low-level API being wrapped and confirm there is no testable business logic in the class.
- Do not apply this exemption to classes that contain any branching logic or orchestration — extract that logic into a separately tested class first.
