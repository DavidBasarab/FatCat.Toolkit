# FatCat.Toolkit C# Coding Standards

## Field Naming

Private and protected fields use camelCase with no underscore prefix (e.g., `logger`, `bufferSize`, not `_logger`, `_bufferSize`).

## Nullable Reference Types

Nullable reference types are disabled project-wide. Use `#nullable enable` at the top of individual files only where nullable analysis is explicitly needed.

## Constructors

Prefer primary constructor syntax (C# 12+) for classes that take constructor parameters. Assign parameters to `protected readonly` or `private readonly` fields inline in the class body.

## Async Methods

Do not use the `Async` suffix on method names. The only exception is when two methods share the same name and one returns a `Task` while the other does not — in that case, append `Async` to the `Task`-returning overload to disambiguate.

## Testing Stack

Tests use xUnit + FakeItEasy + FluentAssertions + FatCat.Fakes. Never use Moq or NSubstitute. Fake objects are created via `Faker.Create<T>()`. Common test usings are registered globally in `GlobalUsings.cs`.

## Namespaces

Always use file-scoped namespace declarations (`namespace X.Y;`), never block-scoped namespaces.

## Result and Response Types

Prefer static factory methods on result and response types over direct constructor calls (e.g., `WebResult.Ok(...)`, `WebResult.BadRequest(...)`).

## Extension Methods

Extension methods live in the `Extensions` sub-namespace of their module. Each static class is named after the type being extended (e.g., `StringExtensions`, `ListExtensions`).

## Object Initialization

Use target-typed `new()` expressions for object and collection initialization where the type is already declared (e.g., `List<string> items = new();`, `Headers = new()`).

## `var` Usage

Use `var` for local variables when the type is evident from the right-hand side. Use explicit types when the type is not immediately obvious from context.
