# hub-client-connection-lost-event — Consumer Compatibility

Written on 2026-08-16 from the consumers' source, not from assumption. Every claim below was checked by
reading `C:\Code\Apostil` and `C:\Code\Fog` at that date.

## What shipped

| # | Change | Public surface touched |
|---|---|---|
| 1 | `event ToolkitHubConnectionLost ConnectionLost` | new delegate `ToolkitHubConnectionLost` (`FatCat.Toolkit.Web.Api`), new event on `IToolkitHubClientConnection` and `ToolkitHubClientConnection` |
| 2 | `DisposeAsync` null-guards `connection` | none — behaviour only |
| 3 | `TimeSpan[] retryDelays = null` on `Connect` and `TryToConnect` | trailing optional parameter on both members of `IToolkitHubClientConnection` |

`Action onConnectionLost` is unchanged in name, position and behaviour. It is invoked first; the event is
raised after it and its Task is returned to SignalR, so `connection.Closed` now awaits the subscriber the
same way `Reconnecting` and `Reconnected` already did.

## Apostil

**Production code compiles unchanged.**

- `Site/Apostil.Site/Program.cs:37` registers
  `AddTransient<IToolkitHubClientConnection, ToolkitHubClientConnection>()`. It registers the toolkit's own
  class, so the new interface event is satisfied by the shipped implementation. **No consumer type
  implements `IToolkitHubClientConnection`** — the new event breaks no implementer.
- `Common/Apostil.Common/Client/ApostilSignalListener.cs:91` calls `connection.TryToConnect(...)`. The new
  parameter is trailing and optional, so the call site is unaffected and the retry policy is unchanged
  unless `retryDelays` is passed.

**Test code needs a one-line edit in nine places.** FakeItEasy argument matchers live in expression trees,
and C# forbids an expression tree from omitting an optional argument (CS0854). Every `A.CallTo` that spells
out all four current arguments of `TryToConnect` must gain `A<TimeSpan[]>._`:

| File | Lines |
|---|---|
| `Common/Tests.Apostil.Common/Client/ApostilSignalListenerSpecs/ApostilSignalListenerTests.cs` | 27, 79, 134, 140 |
| `Common/Tests.Apostil.Common/Client/ApostilSignalListenerSpecs/ListenTests.cs` | 77, 155 |
| `Site/Tests.Apostil.Site/Views/Admin/LibraryPageSpecs/LibraryPageTests.cs` | 49 |
| `Site/Tests.Apostil.Site/Views/Admin/LibraryPageSpecs/SharedListenerTests.cs` | 25, 35 |

This is a source break in a test project, not in shipped code, and it is mechanical:
`..., A<bool>._)` becomes `..., A<bool>._, A<TimeSpan[]>._)`. The same edit was required inside this
repository — `Tests.ToolKit/Web/Api/SignalR/ToolkitHubClientFactoryTests.cs` — and is part of this commit.

**What Apostil can now delete.** `ApostilSignalListener.HandleConnectionLost` discards a Task because the
toolkit hands it an `Action` that cannot await one. Subscribing `ConnectionLost` instead removes both the
discard and the comment explaining why it is safe.

**The latency Apostil did not choose.** With `retryDelays` it can now shorten SignalR's 0/2/10/30-second
default policy without owning a clock of its own. Item 3 passes the delays through; it adds no FatCat retry
loop or timer.

## Fog

**Unaffected.** Fog reaches SignalR only through the factory —
`Common/Common/SignalMessages/Connections/SignalConnection.cs:62` calls
`hubClientFactory.TryToConnectToClient(...)`. `IToolkitHubClientFactory` and
`ToolkitHubClientFactory`'s constructor are untouched by this work item, and Fog fakes neither
`IToolkitHubClientConnection` nor its methods.

**Consequence:** a caller that only has the factory still cannot express the retry delays. Threading
`retryDelays` through `IToolkitHubClientFactory.ConnectToClient` / `TryToConnectToClient` was deliberately
left out — the work item scopes all three changes to `IToolkitHubClientConnection`. It is the obvious
follow-up if a factory-only consumer ever needs a shorter policy.

## Not done here

**The package version is not bumped.** `VersionPrefix` in both `.csproj` files is untouched and
`PushNugetPackages.ps1` was not run. Versioning and publishing are the human's deliberate release step
(`src/.claude/rules/csharp/toolchain.md`), so the corresponding acceptance-criteria box stays unchecked
until that release happens.
