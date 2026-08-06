# SignalR Hub Improvements — Overview

## Where this came from

These changes are requested by the **Apostil** application (`C:\Code\Apostil`), whose `signalr_set_up`
work item built a full SignalR feature on top of **FatCat.Toolkit 1.0.342**. Every item below is a
toolkit limitation that Apostil had to design around with an ADR or a hand-rolled replacement. All line
references are against 1.0.342 source in this repository and were verified while writing this document.

Nothing here is a bug in Apostil — the feature ships and is green (417→648 unit tests, a 104-test
end-to-end suite, isolation proven both ways). But four of these items forced Apostil to either replace a
toolkit class outright or reach around the toolkit's own abstraction, and fixing them in the toolkit would
let Apostil delete code and drop standing risk flags.

## Priority summary

| # | Change | Impact | Backward compatible? | Removes in Apostil |
|---|---|---|---|---|
| 1 | Register client receive handlers **before** `StartAsync` | **Critical** | Yes (pure fix) | The entire `ApostilHubConnection` replacement class |
| 2 | Expose the client `HubConnection` options callback | High | Yes (additive) | Access-token-in-query-string standing flag |
| 3 | Optional automatic reconnect on the client | High | Yes (opt-in) | "No reconnection" standing flag |
| 4 | Make hub `RequireAuthorization()` configurable | Medium | Yes (default unchanged) | Softens the forced anonymous-session-token decision (ADR-2) |
| 5 | Add group support to `IToolkitHubServer` | High | **No** (interface change — see item) | `IHubContext<ToolkitHub>` used directly (ADR-4) |
| 6 | Null-check the inbound `ClientMessage` invoke | **Critical** | Yes (pure fix) | The "subscribe or the hub throws `NullReferenceException`" hazard |
| 7 | `await` the connect hook in `OnConnectedAsync` | Medium | Timing change | The server half of the connect race (ADR-5) |
| 8 | Replace internal `while/Task.Delay` polling + `DateTime.UtcNow` | Low | Yes | (Hygiene only — not an Apostil blocker) |

Items **1 and 6 are correctness bugs** and are the ones worth doing first regardless of the rest.
Items **1, 2, 3, 5** are the ones that let Apostil delete code.

---

## 1 — Register client receive handlers BEFORE `StartAsync` (Critical)

**File:** `src/ToolKit/Web/Api/SignalR/ToolkitHubClientConnection.cs`, `Connect`, lines 42–56.

**Current:**
```csharp
public async Task Connect(string hubUrl, Action onConnectionLost = null)
{
    connection = new HubConnectionBuilder().WithUrl(hubUrl, options => { }).Build();

    connection.Closed += a => { onConnectionLost?.Invoke(); return Task.CompletedTask; };

    await connection.StartAsync();

    RegisterForServerMessages();   // <-- connection.On(...) happens only AFTER StartAsync
}
```

**Problem:** SignalR drops any server→client invocation that arrives before a matching `connection.On(...)`
handler exists. Because `RegisterForServerMessages()` runs *after* `StartAsync()` returns, any message the
server pushes during `OnConnectedAsync` (for example, a readiness/"you are subscribed" signal sent the
instant a connection is assigned to its group) lands in the unguarded window and is silently lost.

**Measured impact in Apostil:** ~40% of connections dropped the server's `ConnectionReady` push. It is
intermittent and invisible (no error, just a missing message), which is the worst failure mode. Apostil
could not fix it from the outside because `Connect` builds its own `HubConnection` and
`RegisterForServerMessages` is private — so Apostil **replaced the whole class** with its own
`ApostilHubConnection` built directly on `Microsoft.AspNetCore.SignalR.Client.HubConnection`, purely to
move the `.On(...)` registration before `StartAsync`.

**Proposed change:** register handlers before starting.
```csharp
connection = new HubConnectionBuilder().WithUrl(hubUrl, options => { }).Build();
connection.Closed += a => { onConnectionLost?.Invoke(); return Task.CompletedTask; };

RegisterForServerMessages();   // move up

await connection.StartAsync();
```
`RegisterForServerMessages` only calls `connection.On(...)`, which is legal before `StartAsync` and is in
fact the documented SignalR order. No consumer relies on the current (broken) ordering.

**Result:** Apostil deletes `ApostilHubConnection` / `IConnectToApostilHub` and goes back to
`IToolkitHubClientConnection`.

---

## 2 — Expose the client `HubConnection` options callback (High)

**File:** `src/ToolKit/Web/Api/SignalR/ToolkitHubClientConnection.cs`, `Connect`, line 44.

**Current:** `new HubConnectionBuilder().WithUrl(hubUrl, options => { })` — the options callback is empty and
not reachable by the caller. The only way to authenticate the connection is to put the token in the URL
query string (`?access_token=...`).

**Problem:** query strings routinely end up in reverse-proxy and browser logs, so a bearer token in the URL
is a real exposure. SignalR's own mechanism is `options.AccessTokenProvider`, which the toolkit hides.

**Proposed change:** let the caller configure the connection options, e.g. add an optional parameter or a
settable property:
```csharp
public Task Connect(string hubUrl, Action onConnectionLost = null,
    Action<HttpConnectionOptions> configureOptions = null)
{
    connection = new HubConnectionBuilder()
        .WithUrl(hubUrl, options => configureOptions?.Invoke(options))
        .Build();
    ...
}
```
(Additive; existing callers pass nothing and get today's behavior.)

**Result:** Apostil can supply `options.AccessTokenProvider = () => Task.FromResult(token)` and drop the
token from the URL — closing a standing security flag on the public surface.

---

## 3 — Optional automatic reconnect on the client (High)

**File:** `src/ToolKit/Web/Api/SignalR/ToolkitHubClientConnection.cs`, `Connect`, line 44.

**Current:** the builder has no `.WithAutomaticReconnect()`, so a dropped socket is permanent — the client
gets no further messages until the whole connection is rebuilt by the caller.

**Proposed change:** make automatic reconnect opt-in (a `bool` parameter or a settable property, default
`false` to preserve today's behavior):
```csharp
var builder = new HubConnectionBuilder().WithUrl(hubUrl, ...);
if (automaticReconnect) { builder = builder.WithAutomaticReconnect(); }
connection = builder.Build();
```
Consider surfacing `connection.Reconnecting` / `connection.Reconnected` as toolkit events so callers can
reflect connection state.

**Result:** removes Apostil's "no reconnection" standing flag; the upcoming ask-streaming epic needs this.

---

## 4 — Make hub `RequireAuthorization()` configurable (Medium)

**File:** `src/Toolkit.WebServer/ApplicationStartUp.cs`, `SetUpSignalR`, lines 214–229.

**Current:**
```csharp
var endpointOption = endpoints.MapHub<ToolkitHub>(ToolkitWebApplication.Settings.SignalRPath);

if (ToolkitWebApplication.Settings.Options.IsFlagSet(WebApplicationOptions.Authentication))
{
    endpointOption.RequireAuthorization();
}
```

**Problem:** any app that enables `WebApplicationOptions.Authentication` (which Apostil must, for its REST
security boundary) gets an authenticated hub with **no opt-out**. An anonymous public visitor therefore
cannot open the hub at all. Apostil worked around this by minting a signed, identity-free anonymous session
token just so public visitors can connect (ADR-2).

**Proposed change:** add a knob — e.g. a `WebApplicationOptions.AnonymousSignalR` flag, or a
`SignalRRequireAuthorization` bool on the settings (default `true` so current behavior is unchanged). When
false, skip `RequireAuthorization()` so the hub accepts anonymous connections even with Authentication on.

**Note / non-goal:** Apostil may keep signed session tokens even if this lands — a server-signed session
claim is what makes its per-session isolation enforceable, so a client cannot ask to join another visitor's
stream. So this is a "give us the choice," not "we will definitely switch." Worth doing anyway; the current
all-or-nothing coupling is surprising.

---

## 5 — Add group support to the server abstraction (High)

**File:** `src/Toolkit.WebServer/SignalR/ToolkitHubServer.cs` (+ its interface, lines 11–39).

**Current:** `IToolkitHubServer` sends to individual connections only. `SendToAllClients` throws
`NotImplementedException` (line 138–141), and there is **no** group concept — no `AddToGroup`,
no `SendToGroup`. (The published assembly contains zero `Groups` references.)

**Problem:** group fan-out is the natural primitive for "push to all curators" or "push to one session."
With nothing on the toolkit seam, Apostil had to inject `IHubContext<ToolkitHub>` **directly** into its own
manager and call `hubContext.Groups.AddToGroupAsync` / `hubContext.Clients.Group(...)` itself (ADR-4). That
works, but it means the toolkit's own hub abstraction is bypassed for the most common real-time pattern.

**Proposed change:** add group operations, backed by the `IHubContext<ToolkitHub>` the server already holds:
```csharp
Task AddToGroup(string connectionId, string groupName);
Task RemoveFromGroup(string connectionId, string groupName);
Task SendToGroup(string groupName, ToolkitMessage message);          // fire-and-forget
```
and implement `SendToAllClients` (via `hubContext.Clients.All`) rather than throwing.

**Compatibility:** adding methods to the **existing public interface** `IToolkitHubServer` is a breaking
change for any external implementer. Two safe options: (a) put the new methods on a **separate** interface
(e.g. `IToolkitHubGroups`) that `ToolkitHubServer` also implements, or (b) add them as default-interface
methods. Please pick whichever fits the toolkit's conventions.

**Result:** Apostil pushes through the toolkit seam and no longer needs `IHubContext<ToolkitHub>` in its own
code — simplifying its "only one class may touch the hub context" rule to "nobody needs to."

---

## 6 — Null-check the inbound `ClientMessage` invoke (Critical)

**File:** `src/Toolkit.WebServer/ToolkitWebApplicationSettings.cs`, `OnClientHubMessage`, lines 61–64.

**Current:**
```csharp
public Task<string> OnClientHubMessage(ToolkitMessage message)
{
    return ClientMessage.Invoke(message)!;   // no null-conditional
}
```
Every sibling hook in the same file is null-safe — `OnClientConnected` uses `ClientConnected?.Invoke(...)`
(line 53) and `OnOnClientDataBufferMessage` uses `ClientDataBufferMessage?.Invoke(...)!` (line 68). Only
`OnClientHubMessage` omits the `?.`.

**Problem:** with `WebApplicationOptions.SignalR` enabled and **no** `ClientMessage` subscriber, any inbound
hub invocation throws a `NullReferenceException` inside the hub. So "map the hub" and "subscribe to inbound
messages" are silently coupled: enabling the hub without a subscriber crashes on the first client message.
Apostil had a genuine, if brief, window between two commits where exactly this hazard was live.

**Proposed change:** make it null-safe like the others:
```csharp
public Task<string> OnClientHubMessage(ToolkitMessage message)
{
    return ClientMessage?.Invoke(message) ?? Task.FromResult<string>(null);
}
```

**Result:** an app can enable the hub for server→client push without being forced to also handle inbound
messages just to avoid a crash.

---

## 7 — `await` the connect hook in `OnConnectedAsync` (Medium)

**File:** `src/Toolkit.WebServer/SignalR/ToolkitHub.cs`, `OnConnectedAsync`, lines 97–117.

**Current:**
```csharp
HubServer.OnClientConnected(toolkitUser, Context.ConnectionId);
ToolkitWebApplication.Settings.OnClientConnected(toolkitUser, Context.ConnectionId);  // Task discarded

return base.OnConnectedAsync();
```
`Settings.OnClientConnected` returns a `Task` (see `ToolkitWebApplicationSettings` line 51) but it is not
awaited, so `HubConnection.StartAsync()` can return on the client before the server-side connect handler has
finished. If that handler assigns the connection to a group, the client can start (and the app can push)
before the group membership exists — an intermittent "message went nowhere" race.

Apostil covers this with an explicit `ConnectionReady` handshake (ADR-5), which it wants anyway, so this is
lower priority than 1/6 — but awaiting the hook is simply more correct and removes the server half of the
race.

**Proposed change:** make `OnConnectedAsync` await the hooks:
```csharp
public override async Task OnConnectedAsync()
{
    try
    {
        var toolkitUser = ToolkitUser.Create(Context.User);
        HubServer.OnClientConnected(toolkitUser, Context.ConnectionId);
        await ToolkitWebApplication.Settings.OnClientConnected(toolkitUser, Context.ConnectionId);
    }
    catch (Exception ex) { Logger.Exception(ex); }

    await base.OnConnectedAsync();
}
```
(Guard against a null Task if no subscriber, same spirit as item 6.)

---

## 8 — Replace internal polling and `DateTime.UtcNow` (Low / hygiene)

**Files:** `ToolkitHubServer.WaitForClientResponse` (lines 185–211) and the equivalent in
`ToolkitHubClientConnection.WaitForResponse` — both use `while (true) { ... await Task.Delay(35); }` plus
`DateTime.UtcNow` for the timeout.

This is not an Apostil blocker (it is third-party code behind a seam), and it is only noted so it is on the
list: a `TaskCompletionSource` keyed by session id, completed when the response arrives and cancelled by a
`CancellationTokenSource(timeout)`, would remove both the busy-wait and the wall-clock read. Purely internal;
no public surface changes.

---

## Suggested sequencing

1. **6** then **1** — the two correctness bugs; smallest, highest value, no API change.
2. **2** and **3** — additive client options (token provider, reconnect).
3. **5** — group support (decide the interface-compat approach first).
4. **4** — configurable hub authorization.
5. **7**, then **8** — correctness/hygiene.

After 1, 2, 3 and 5 ship in a new toolkit release, Apostil can delete `ApostilHubConnection`, move its hub
token out of the query string, add reconnect, and drop `IHubContext<ToolkitHub>` from its own code. Ping the
Apostil side with the new version number and it will re-point and remove the workarounds.
