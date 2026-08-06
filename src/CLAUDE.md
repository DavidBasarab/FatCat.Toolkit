# FatCat.Toolkit Coding Standards

This file defines the coding standards for the FatCat.Toolkit codebase.
All code you generate — in any context — must follow the rules for the relevant language below.
The goal is that AI-generated code is indistinguishable from code written by a senior member of this team.

## ⚠️ READ THIS FIRST — Before Any Work

**Before doing anything else in this repository — reading, planning, or writing code — read [`README.md`](../README.md) at the repository root in full.**
It is the single source of truth for what this library does and how consumers use it. Do not skip it, even for a "small" change.

The things you must know before touching a line of code:

- **Product:** `FatCat.Toolkit` is a **published NuGet library**, not an application. Its consumers are other products that take a package reference. Every public type, member, and signature is a promise to a caller you cannot see — renaming or re-signing one breaks them at compile time.
- **Tech stack:** **.NET 10 / C# 14**, Autofac for dependency injection, MongoDB for persistence, SignalR for real-time messaging, xUnit + FakeItEasy + FatCat.Testing + FatCat.Fakes for tests. There is no Serilog, no AutoMapper, no FluentAssertions, and no TypeScript or React in this repository.
- **Layout:** the git root is `C:/Code/FatCat.Toolkit`; all code and tooling config live in `src/`. The solution is `src/ToolKit.slnx`.

| Project | Assembly | Role |
|---|---|---|
| `ToolKit` | `FatCat.Toolkit` | Core library — caching, cryptography, data/Mongo, threading, messaging, JSON, logging, web client. Shipped. |
| `Toolkit.WebServer` | `FatCat.Toolkit.WebServer` | Server side — `Endpoint`, `WebResult`, SignalR hubs, token handling. Shipped. |
| `Tests.ToolKit` | `Tests.FatCat.Toolkit` | The single test project covering both libraries. |
| `OneOff`, `OneOffLib`, `OneOffToolkitOnly`, `OneOffBlazor`, `ProxySpike`, `SampleDocker` | — | Hand-run sample and spike hosts. Not shipped; not held to TDD. |

Where the README and the actual code disagree, **the code is authoritative** — trust the code and flag the stale README.

---

## C# Rules

Apply these rules to all C# code.

@.claude/rules/csharp/naming-and-structure.md
@.claude/rules/csharp/types-and-di.md
@.claude/rules/csharp/toolchain.md
@.claude/rules/csharp/async.md
@.claude/rules/csharp/errors-and-logging.md
@.claude/rules/csharp/testing.md
@.claude/rules/csharp/not-allowed.md

## PowerShell Rules

Apply these rules to all PowerShell scripts. Do not apply them to C# or any other language.

@.claude/rules/powershell/powershell.md
