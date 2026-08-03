# Consumer Compatibility — Fog and Apostil

Checked `C:\Code\Fog` and `C:\Code\Apostil` against every phase in [02-phase-plan.md](02-phase-plan.md).

| Repo | Package | Version | Role |
|---|---|---|---|
| Fog | `FatCat.Toolkit` / `FatCat.Toolkit.WebServer` | **1.0.339** | server **and** client — heaviest consumer |
| Apostil | `FatCat.Toolkit` / `FatCat.Toolkit.WebServer` | **1.0.342** | server, plus a hand-rolled client |

**Verdict: the plan is non-breaking for both, but only after two mandatory additions.** As written, Phase 1b
introduces a `NullReferenceException` in Fog, and a careless Phase 0/2 breaks Fog's compile. Both are
cheap to avoid. Everything else is clean.

---

## Blocker 1 — Phase 1b crashes Fog unless the client hooks are null-guarded too

**This is the one real breaking change in the plan.**

`ToolkitHubClientConnection` has the *identical* null-Task defect as the server hooks in item 6, and the
overview never mentions it:

```csharp
private Task<string> InvokeServerMessage(ToolkitMessage message)
{
    return ServerMessage?.Invoke(message)!;      // null Task when no subscriber
}

// OnServerOriginatedMessage, line 192:
var response = await InvokeServerMessage(message);   // await null -> NullReferenceException
```

Same shape in `InvokeDataBufferMessage` (:140-143), awaited at :174.

Today this is unreachable: `connection.On(...)` is registered *after* `StartAsync`, so any message arriving
before a subscriber exists is silently dropped by SignalR and never reaches `InvokeServerMessage`.
**Phase 1b removes exactly that protection.**

Fog subscribes after the connection is live —
[SignalConnection.cs:62-72](../../../../Fog/Common/Common/SignalMessages/Connections/SignalConnection.cs#L62-L72):

```csharp
var result = await hubClientFactory.TryToConnectToClient(ConnectionUrl, onServerDisconnect);

if (result.Connected)
{
    connection = result.Connection;
    RegisterForResponseMessage();     // connection.ServerMessage += ... happens HERE, after connect
```

So after Phase 1b there is a live window where the toolkit accepts a server push off the wire and Fog has
not attached `ServerMessage` yet → `await null` → `NullReferenceException` inside the SignalR client
callback. The failure converts from "message silently dropped" to "exception in the client message pump" —
strictly worse.

**Fog's exposure today is narrow** — `BrumeHandleClientConnections.ClientConnected` only writes to a cache
and pushes nothing, so nothing arrives in that window under current server behavior. But **Phase 5 widens
it**: awaiting `OnClientConnected` makes connect-time pushes land inside `OnConnectedAsync`, which is
precisely when the window is open.

**Required addition to Phase 1b** (mirrors Phase 1a on the client side):

```csharp
private Task<string> InvokeServerMessage(ToolkitMessage message)
{
    return ServerMessage?.Invoke(message) ?? Task.FromResult<string>(null);
}

private Task<string> InvokeDataBufferMessage(ToolkitMessage message, byte[] dataBuffer)
{
    return ServerDataBufferMessage?.Invoke(message, dataBuffer) ?? Task.FromResult<string>(null);
}
```

The existing `if (response is not null)` guards at :176 and :194 already handle the null result correctly —
no response is sent, which is the right behavior for an unsubscribed client. With this in, Phase 1b is
non-breaking for Fog.

---

## Blocker 2 — do not touch `ToolkitHubClientFactory`'s constructor (Fog compile break)

Fog does **not** resolve the factory from DI. It constructs it by hand —
[SignalConnectionFactory.cs:39](../../../../Fog/Common/Common/SignalMessages/Connections/SignalConnectionFactory.cs#L39):

```csharp
var hubClientFactory = new ToolkitHubClientFactory(scope);
```

Any new constructor parameter on `ToolkitHubClientFactory` — even optional — is a source break for Fog.

**Constraint:** Phase 0 and Phase 2 may add optional parameters to the factory's *methods*
(`ConnectToClient`, `TryToConnectToClient`), which Fog calls positionally with two arguments and which stay
source-compatible. They may **not** change its constructor. Phase 0's new dependency belongs on
`ToolkitHubClientConnection` only, which Fog never constructs directly.

---

## Phase-by-phase compatibility

### Phase 0 — test seams: **safe, verified**

The new `IHubConnectionBuilderFactory` resolves automatically in both repos with zero consumer changes.
`SystemScope.Initialize` always injects the ToolKit assembly into the scan set
([SystemScope.cs:49](../../ToolKit/Injection/SystemScope.cs#L49)) and registers
`RegisterAssemblyTypes(...).AsImplementedInterfaces().HasPublicConstructor().PublicOnly()`
([:118-125](../../ToolKit/Injection/SystemScope.cs#L118-L125)). Both repos boot through `SystemScope`.

Requirement: the new type must be `public` with a `public` constructor. Subject to Blocker 2.

### Phase 1 — correctness bugs: **safe once Blocker 1 is folded in**

Server side (1a) is a pure improvement for both, and fixes a **live latent crash in Apostil**. Apostil wires
only two of the four hooks —
[ApostilHubHooks.cs:14-15](../../../../Apostil/Api/Apostil.Api/Infrastructure/ApostilHubHooks.cs#L14-L15):

```csharp
settings.ClientConnected += ...;
settings.ClientMessage   += ...;
// ClientDisconnected and ClientDataBufferMessage: NOT subscribed
```

So any client sending a data-buffer message to Apostil today hits `await null` in
[ToolkitHub.cs:43](../../Toolkit.WebServer/SignalR/ToolkitHub.cs#L43) and crashes the hub. Phase 1a's
widening to all four hooks closes that. Fog subscribes all four
([ApplicationRunner.cs:66-69](../../../../Fog/Common/Common.WebServer/Infrastructure/ApplicationRunner.cs#L66-L69))
so it is unaffected either way.

Client side (1b): see Blocker 1. With the guard, Fog's behavior is unchanged; without it, Fog gets a new NRE.

Apostil deletes `ApostilHubConnection` / `IConnectToApostilHub` after this ships — that class exists solely
to move `.On(...)` before `StartAsync`, and its `[ExcludeFromCodeCoverage]` justification says so verbatim.

### Phase 2 — client options: **safe, and the server side already accepts it**

Both repos' `JwtBearerEvents` read `access_token` from the query string but only set `context.Token` when the
query value is **non-empty** —
[ApostilJwtBearerEvents.cs:21-31](../../../../Apostil/Api/Apostil.Api/Authentication/ApostilJwtBearerEvents.cs#L21-L31)
and [ApplicationRunner.cs:86-104](../../../../Fog/Common/Common.WebServer/Infrastructure/ApplicationRunner.cs#L86-L104),
same shape as the toolkit's own `OAuthExtensions.GetTokenBearerEvents`. When the query string is absent,
they fall through to JwtBearer's default `Authorization: Bearer` header extraction.

**So a client switching to `options.AccessTokenProvider` authenticates against both servers with no server
change.** Apostil can drop the token from the URL immediately; Fog's `EventHubUrl` can keep appending
`?access_token=` indefinitely with no pressure to migrate. Additive on both ends.

Automatic reconnect is opt-in and defaults off — neither repo's `onConnectionLost` semantics change unless
they ask for it. Note for Fog if it ever opts in: `SignalConnection` sets a `connected` bool and
`ToolkitHubClientFactory` evicts from its cache on `onConnectionLost`; under reconnect that callback fires
only after retries are exhausted, which is the behavior Fog wants, but `SignalConnection.connected` would
then stay `true` across a transient drop. Fine — just not automatic.

### Phase 3 — group support: **safe, verified no implementers**

Nothing in either repo implements `IToolkitHubServer`, `IToolkitHubClientConnection`, or
`IToolkitHubClientFactory`, and nothing fakes them in tests. Adding a **separate** `IToolkitHubGroups`
touches neither.

- Fog injects `IToolkitHubServer` into `FogHubManager` and uses only `SendToClient`,
  `SendToClientNoResponse`, `SendDataBufferToClient` — all unchanged.
- Nobody calls `SendToAllClients`, so replacing its `NotImplementedException` with a real implementation
  cannot regress anything.
- Apostil's `ApostilHubManager` uses `IHubContext<ToolkitHub>` directly and is unaffected until it chooses
  to migrate. Its `AddToGroup` / `SendToCurators` / `SendToSession` map cleanly onto the proposed
  `AddToGroup` / `SendToGroup`.

Reconfirmed: `SignalRModule` must chain `.As<IToolkitHubServer>().As<IToolkitHubGroups>().SingleInstance()`
onto the one registration, or the two interfaces resolve to different singletons.

### Phase 4 — configurable hub auth: **safe at the default; do not enable for Fog**

Both repos set `WebApplicationOptions.Authentication | WebApplicationOptions.SignalR`, so both get
`RequireAuthorization()` today. Default `true` preserves that exactly.

**Warning worth recording:** Fog's hub cannot tolerate anonymous connections.
[BrumeHandleClientConnections.GetUserId](../../../../Fog/Brume/Brume/Infrastructure/BrumeHandleClientConnections.cs#L41-L46)
does `user.Claims.FirstOrDefault(i => i.Type == FogClaimTypes.UserId).Value` with no null check — an
anonymous connection would `NullReferenceException` on every connect. The new setting must stay `true` for
Fog. This is a Fog-side hazard, not a toolkit defect, but it is the reason the default matters.

### Phase 5 — await lifecycle hooks: **safe only after Phase 1a; Apostil proves it**

Apostil does not subscribe `ClientDisconnected`. Awaiting `OnClientDisconnected` without Phase 1a's
`?? Task.CompletedTask` means **every Apostil disconnect throws a `NullReferenceException`**. This is the
concrete proof that the Phase 1 → Phase 5 ordering is mandatory, not stylistic.

With Phase 1a in place, Phase 5 is a clear win for Apostil:
[HubGroupAssigner](../../../../Apostil/Api/Apostil.Api/Signals/HubGroupAssigner.cs) pushes `ConnectionReady`
from the `ClientConnected` hook. Awaiting it moves that push inside `OnConnectedAsync`, so it lands before
the client's `StartAsync` returns — which, combined with Phase 1b, makes `ConnectionReady` deterministic and
removes the server half of the ADR-5 race. Apostil's E2E helper `ApostilHubSession` already registers its
handlers before `StartAsync` and needs no change.

Fog: `BrumeHandleClientConnections.ClientConnected` is a cache write. Awaiting it adds negligible connect
latency. Fog's `ClientDisconnected` is likewise trivial. No risk.

### Phase 6 — deterministic waiting: **safe, one invariant to preserve**

Internal only. Fog depends on the *exception type*: `SignalConnection.SendFileBytes` catches, logs and
rethrows, and `SendMessage` lets it propagate. The `TaskCompletionSource` rewrite must still throw
`TimeoutException` (not `TaskCanceledException` / `OperationCanceledException`) and must still return
`ToolkitMessage` unchanged. Apostil never uses the toolkit client's request/response path.

---

## Version gap: Fog is two releases behind

Apostil is on **1.0.342**, the current source baseline — it upgrades cleanly.

Fog is on **1.0.339**. Moving it to 1.0.343+ also picks up 340–342, which per `git log` on the affected
files is `ConfigureMiddleware` and `ConfigureLogging` — two new additive properties on
`ToolkitWebApplicationSettings`. Both additive, neither touches SignalR, so no break is expected. But it is a
larger jump than Apostil's and deserves its own build-and-smoke pass rather than being assumed.

Out of scope and intentionally left alone: `Fog/Spikes/dude2` pins 1.0.240 / 1.0.64. Spike code; do not
upgrade.

---

## Summary of required plan amendments

1. **Phase 1b must also null-guard `InvokeServerMessage` and `InvokeDataBufferMessage`** on the client.
   Without this, Phase 1b is a breaking change for Fog. *(Blocker)*
2. **`ToolkitHubClientFactory`'s constructor is frozen** — Fog constructs it with `new`. Method-level
   optional parameters only. *(Blocker)*
3. Phase 1a before Phase 5 is mandatory — Apostil does not subscribe `ClientDisconnected`.
4. Phase 4's new setting must default to `true`; Fog's claim handling would NRE on an anonymous connection.
5. Phase 6 must keep throwing `TimeoutException` — Fog catches on that path.
6. Fog's 339 → 343 upgrade spans two unrelated releases; smoke-test it separately.

With 1 and 2 applied, **no phase in this plan is a breaking change for either repository.**
