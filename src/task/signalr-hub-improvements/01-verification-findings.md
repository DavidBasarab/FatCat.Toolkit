# Verification of `00-overview.md`

Verified against working tree on branch `SignalRChanges` (`ce7aab1`), `VersionPrefix` **1.0.342**,
`net10.0`, `Nullable disable`, `Microsoft.AspNetCore.SignalR.Client` 10.0.10.

**Verdict: all eight items are real and every line reference is accurate.** Four items are
under-scoped — the same defect exists on a sibling code path the overview does not mention. One
proposed compatibility option (item 5, default-interface methods) does not work. One cross-cutting
blocker (testability) is not mentioned at all and gates Phase 1–3.

---

## Item-by-item

### 1 — Register receive handlers before `StartAsync` — **CONFIRMED, exact**

[ToolkitHubClientConnection.cs:42-56](../../ToolKit/Web/Api/SignalR/ToolkitHubClientConnection.cs#L42-L56).
`await connection.StartAsync()` is line 53; `RegisterForServerMessages()` is line 55.
`RegisterForServerMessages` ([:223-232](../../ToolKit/Web/Api/SignalR/ToolkitHubClientConnection.cs#L223-L232))
does nothing but three `connection.On(...)` calls, so moving it above `StartAsync` is safe and is the
documented SignalR order. `RegisterForServerMessages` is `private` and `connection` is a private field,
so the overview is correct that this cannot be fixed from outside the class.

### 2 — Expose the options callback — **CONFIRMED**, but the plan misses the factory

Line 44 is literally `.WithUrl(hubUrl, options => { })`. Confirmed unreachable by callers.

**Gap:** `IToolkitHubClientFactory` is the registered, DI-resolvable way to get a connection
([ToolkitHubClientFactory.cs:20-34](../../ToolKit/Web/Api/SignalR/ToolkitHubClientFactory.cs#L20-L34)
and [:49-76](../../ToolKit/Web/Api/SignalR/ToolkitHubClientFactory.cs#L49-L76)) — `SignalRModule`
registers `ToolkitHubClientFactory` but **not** `IToolkitHubClientConnection` itself, which is resolved
from the scope inside the factory. If `configureOptions` is added only to `Connect`/`TryToConnect`, no
consumer using the factory can reach it. `ConnectToClient` and `TryToConnectToClient` need matching
pass-through parameters.

**Note:** `HttpConnectionOptions` lives in `Microsoft.AspNetCore.Http.Connections.Client`. Putting it on
the public `IToolkitHubClientConnection` signature adds that namespace to the toolkit's public surface.
It is already a transitive dependency of the SignalR client package, so this costs nothing at runtime,
but it is a deliberate public-API decision.

### 3 — Optional automatic reconnect — **CONFIRMED**, with a behavioral caveat the plan omits

No `.WithAutomaticReconnect()` anywhere in the repository. Confirmed.

**Caveat:** with automatic reconnect enabled, `connection.Closed` no longer fires on transient drops —
`Reconnecting`/`Reconnected` fire instead, and `Closed` only fires after reconnect attempts are
exhausted. `ToolkitHubClientFactory.TryToConnectToClient` uses the `onConnectionLost` callback to evict
the connection from its cache ([:58-66](../../ToolKit/Web/Api/SignalR/ToolkitHubClientFactory.cs#L58-L66)).
That eviction is *correct* under reconnect (you want the cached connection kept while it self-heals),
but it means `onConnectionLost` silently changes meaning from "socket dropped" to "gave up" for opt-in
callers. Document it; do not try to preserve the old firing pattern.

### 4 — Configurable `RequireAuthorization()` — **CONFIRMED, exact**

[ApplicationStartUp.cs:214-230](../../Toolkit.WebServer/ApplicationStartUp.cs#L214-L230).
`RequireAuthorization()` at line 227, gated only on `WebApplicationOptions.Authentication`. No opt-out.
`WebApplicationOptions` ([WebApplicationOptions.cs](../../ToolKit/Web/Api/WebApplicationOptions.cs)) is a
`[Flags]` enum whose next free bit is **32**.

Recommendation: prefer a `SignalRRequireAuthorization` bool on `ToolkitWebApplicationSettings`
(default `true`) over a new enum flag. A new flag would read as "turn something on", but the semantics
are "turn something off", and `CommonOptions` composition makes negative flags confusing.

### 5 — Group support — **CONFIRMED**, but option (b) in the plan does not work

`SendToAllClients` throws `NotImplementedException`
([ToolkitHubServer.cs:138-141](../../Toolkit.WebServer/SignalR/ToolkitHubServer.cs#L138-L141)). No
`Groups` reference exists anywhere in the source. Interface is
[:11-39](../../Toolkit.WebServer/SignalR/ToolkitHubServer.cs#L11-L39). All confirmed.

**Correction:** the overview offers "(b) add them as default-interface methods" as a
compatibility-preserving option. That is not viable — a default interface method has no access to
`ToolkitHubServer`'s primary-constructor `hubContext`, so it cannot implement group routing. It could
only `throw`, which is the `SendToAllClients` problem again. **Option (a), a separate
`IToolkitHubGroups` interface, is the only workable choice.**

**Gap:** `SignalRModule` registers the server as
`builder.RegisterType<ToolkitHubServer>().As<IToolkitHubServer>().SingleInstance()`
([SignalRModule.cs](../../Toolkit.WebServer/SignalR/SignalRModule.cs)). A second interface must be
chained onto the *same* registration — `.As<IToolkitHubServer>().As<IToolkitHubGroups>().SingleInstance()`
— or Autofac hands out two distinct singletons with two distinct connection dictionaries.

### 6 — Null-check the inbound `ClientMessage` invoke — **CONFIRMED, and larger than described**

[ToolkitWebApplicationSettings.cs:61-64](../../Toolkit.WebServer/ToolkitWebApplicationSettings.cs#L61-L64)
is exactly as quoted. Confirmed.

**The overview's claim that "every sibling hook in the same file is null-safe" is wrong.** The `?.` on
the siblings guards the *delegate*, not the *returned Task*. With no subscriber they return a `null`
`Task`, and the caller awaits it:

| Hook | Returns when no subscriber | Awaited at | Result today |
|---|---|---|---|
| `OnClientHubMessage` (:61) | throws `NullReferenceException` immediately | [ToolkitHub.cs:73](../../Toolkit.WebServer/SignalR/ToolkitHub.cs#L73) | crash |
| `OnOnClientDataBufferMessage` (:66) | `null` Task | [ToolkitHub.cs:43](../../Toolkit.WebServer/SignalR/ToolkitHub.cs#L43) | **crash — `await null`** |
| `OnClientConnected` (:51) | `null` Task | [ToolkitHub.cs:109](../../Toolkit.WebServer/SignalR/ToolkitHub.cs#L109) — **discarded** | benign today |
| `OnClientDisconnected` (:56) | `null` Task | [ToolkitHub.cs:127](../../Toolkit.WebServer/SignalR/ToolkitHub.cs#L127) — **discarded** | benign today |

So the data-buffer path has the identical crash, and the two connect hooks are latent crashes that
**item 7 would activate** the moment they are awaited. All four hooks must be fixed together; fixing
only `OnClientHubMessage` leaves half the hazard live.

Same shape exists in `ToolkitHubServer.InvokeClientConnected`/`InvokeClientDisconnected`
([:163-171](../../Toolkit.WebServer/SignalR/ToolkitHubServer.cs#L163-L171)) — currently discarded by
their callers, so benign, but worth normalizing while in the file.

### 7 — `await` the connect hook — **CONFIRMED**, and `OnDisconnectedAsync` has it too

[ToolkitHub.cs:97-117](../../Toolkit.WebServer/SignalR/ToolkitHub.cs#L97-L117). Line 109 discards the
`Task`; line 116 returns `base.OnConnectedAsync()`. Confirmed.

**Gap:** [`OnDisconnectedAsync`:119-135](../../Toolkit.WebServer/SignalR/ToolkitHub.cs#L119-L135) discards
its hook the same way at line 127. Awaiting connect but not disconnect leaves the mirror-image race
(the app can be told a client left after the connection object is already gone). Fix both.

**Dependency:** this phase is unsafe until item 6's null-Task guards land — see the table above.

### 8 — Polling and `DateTime.UtcNow` — **CONFIRMED**

`ToolkitHubServer.WaitForClientResponse`
([:185-211](../../Toolkit.WebServer/SignalR/ToolkitHubServer.cs#L185-L211)) and
`ToolkitHubClientConnection.WaitForResponse`
([:239-269](../../ToolKit/Web/Api/SignalR/ToolkitHubClientConnection.cs#L239-L269)) both use
`while (true)` + `await Task.Delay(35)` + `DateTime.UtcNow`. Confirmed, plus one the overview does not
call out: raw `Task.Delay` violates this codebase's own rule that threading and sleep go through
`IThread`. That makes item 8 a house-rules fix as well as a hygiene one, and it is what makes both
`WaitFor*` methods deterministically testable.

---

## Cross-cutting finding: none of this is currently testable (not in the overview)

This gates phases 1–3 and is the single biggest cost the overview does not price in.

**1. `ToolkitHubClientConnection` has no seam.** `Connect` calls `new HubConnectionBuilder()` inline
(line 44). `HubConnection` is a concrete sealed-in-practice class with no interface. Per the project's own
rule — *"If something cannot be faked in a test, it is not properly abstracted"* — and *"TDD is
non-negotiable"*, items 1, 2 and 3 cannot be written test-first as the class stands. The class also
cannot take the `[ExcludeFromCodeCoverage]` low-level-wrapper exemption, because it holds real branching
logic (response correlation, timeout bookkeeping, three concurrent dictionaries).

**2. `ToolkitHub` reaches through two statics.** `SystemScope.Container.Resolve<...>()` (lines 11-25)
and `ToolkitWebApplication.Settings` (lines 43, 73, 109, 127). Item 7 is testable only by driving those
statics from the test.

**3. There is no test project for `Toolkit.WebServer`.** `Tests.ToolKit` already references it
(`Tests.ToolKit.csproj:31`), so server-side tests belong there under `Tests.FatCat.Toolkit.WebServer.SignalR`.

**4. Existing SignalR test coverage is one file** — `Tests.ToolKit/Web/Api/SignalR/GetUserClaimTests.cs`.
There is effectively no regression net under any of this code.

---

## Corrections to carry into the plan

1. Item 5's default-interface-method option is not viable — use a separate `IToolkitHubGroups`.
2. Item 6 is four hooks, not one; the data-buffer path crashes identically today.
3. Item 7 must include `OnDisconnectedAsync`, and must land **after** item 6.
4. Items 2 and 3 must thread through `IToolkitHubClientFactory` or they are unreachable in practice.
5. A test seam for `HubConnection` construction is a prerequisite, not a nicety.
6. `SignalRModule` must chain any new interface onto the existing singleton registration.
