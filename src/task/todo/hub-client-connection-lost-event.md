# hub-client-connection-lost-event (FatCat.Toolkit)

> **Origin:** raised by a consumer. Written from `C:\Code\Apostil` on 2026-08-16 while finishing that
> repository's `hub_connection_health` work item, which forwards the toolkit's connection lifecycle to a
> Blazor WebAssembly UI so a curator is told when the live channel dies.
>
> **Status:** a suggestion, not a commitment. **Apostil ships without any of the three items below** and
> depends on none of them. Nothing in the consumer is blocked; each item removes a wart the consumer is
> currently living with, and the consumer says out loud, in code, that it is living with it.

## Work Item

Three independent changes to `IToolkitHubClientConnection` /
`ToolkitHubClientConnection` (`src/ToolKit/Web/Api/SignalR/ToolkitHubClientConnection.cs`):

1. A **`Task`-returning `ConnectionLost` event**, symmetric with the existing `Reconnecting` and
   `Reconnected`.
2. A **null guard in `DisposeAsync`**.
3. A way to **pass SignalR's automatic-reconnect retry delays** through `TryToConnect` / `Connect`.

They can ship together or one at a time, in any order.

---

## Specification

### 1. `event ToolkitHubConnectionLost ConnectionLost`

Today the only way to hear that a connection closed is the `Action onConnectionLost` parameter on
`Connect` / `TryToConnect`:

```csharp
connection.Closed += a =>
{
	onConnectionLost?.Invoke();

	return Task.CompletedTask;
};
```

`Reconnecting` and `Reconnected` are events returning `Task`, and SignalR awaits both. The loss is the
one member of the same family that is neither an event nor awaitable.

**Proposed:**

```csharp
public delegate Task ToolkitHubConnectionLost();

public event ToolkitHubConnectionLost ConnectionLost;
```

wired the same way the other two are, so `connection.Closed` returns
`ConnectionLost?.Invoke() ?? Task.CompletedTask` **after** invoking the existing `onConnectionLost`.

**`Action onConnectionLost` stays exactly as it is** — same parameter, same position, same behaviour. This
is additive; nothing a consumer compiles against changes.

**What it removes for the consumer.** `Apostil.Common`'s `ApostilSignalListener` has to announce the loss
through an `async Task` raise, and the callback it is handed cannot await one, so it discards the task and
says why:

```csharp
private void HandleConnectionLost()
{
	if (stopWasAsked)
	{
		return;
	}

	// The toolkit hands this back as an Action, so there is nothing to await it. The raise swallows a
	// subscriber's failure itself, so nothing is left unobserved.
	_ = MarkLost();
}
```

That discard is safe — the raise cannot throw — but it is a discarded task in a receive path, and every
later reader has to re-derive that it is safe. With the event, the whole comment and the discard go away.

### 2. `DisposeAsync` null-guards `connection`

```csharp
public async ValueTask DisposeAsync()
{
	await Disconnect();
	await connection.DisposeAsync();
}
```

`connection` is assigned inside `Connect`, so it is `null` on any instance that was resolved and never
connected — or whose `Connect` threw, which is exactly the case `TryToConnect` swallows to return `false`.
`Disconnect()` already guards (`if (connection is not null)`); the line beneath it does not, so disposing
such an instance throws `NullReferenceException`.

**Proposed:** the same guard `Disconnect` already uses.

```csharp
public async ValueTask DisposeAsync()
{
	await Disconnect();

	if (connection is not null)
	{
		await connection.DisposeAsync();
	}
}
```

**Provenance:** first recorded as an open observation by Apostil's `toolkit_signalr_migration` phase 1,
and still true in the source read on 2026-08-16. It is reachable in a consumer that registers the
connection in a DI container which disposes what it created: a container holding a connection whose
`TryToConnect` returned `false` disposes a null field.

### 3. A way to pass the automatic-reconnect retry delays

`automaticReconnect: true` maps to `builder.WithAutomaticReconnect()` with **SignalR's default policy** —
retries at 0 s, 2 s, 10 s and 30 s, then the connection is closed. The toolkit exposes no way to pass
`WithAutomaticReconnect(TimeSpan[])` or an `IRetryPolicy`, so a consumer cannot choose how long a
reconnect is attempted before the connection is called lost.

**Proposed** (shape is the maintainer's call; the ask is only that the delays become expressible):

```csharp
public Task<bool> TryToConnect(
	string hubUrl,
	Action onConnectionLost = null,
	Action<HttpConnectionOptions> configureOptions = null,
	bool automaticReconnect = false,
	TimeSpan[] retryDelays = null
);
```

with `retryDelays` selecting `builder.WithAutomaticReconnect(retryDelays)` when it is non-null and
`automaticReconnect` is true, and the current `WithAutomaticReconnect()` otherwise. An optional parameter
with a `null` default keeps every existing call site compiling and behaving identically.

**What it removes for the consumer.** Apostil's ADR-4 defers entirely to the toolkit's reconnect policy —
the site owns no clock, and a second timeout would be a second source of truth. The consequence is a
latency **nobody in the consuming repository chose**: measured end to end in a real browser against a
deployed instance, a curator is told the connection is lost roughly **70–75 seconds** after the tab goes
offline (the browser transport takes about 36 seconds to fail an established WebSocket, and SignalR's four
retries run after that). The plan that produced this expected about 42 seconds from the retry delays alone,
which is what the delays cost and not what a user waits. If that is
too slow for a given instance, the only honest fix is a shorter retry policy — which the toolkit cannot
express today, and which the consumer will not fake with a timer of its own.

---

## Acceptance Criteria

- [ ] `IToolkitHubClientConnection` exposes `event ToolkitHubConnectionLost ConnectionLost` returning
      `Task`, raised from `connection.Closed`, and `Action onConnectionLost` still fires exactly as it does
      today
- [ ] A subscriber that throws from `ConnectionLost` is handled the same way `Reconnecting` and
      `Reconnected` subscribers are — no new asymmetry
- [ ] `DisposeAsync` on a connection that never connected completes without throwing, covered by a test
- [ ] `TryToConnect` and `Connect` accept the automatic-reconnect retry delays, defaulting to today's
      behaviour when they are not supplied
- [ ] Every existing call site in every consuming repository compiles unchanged — all three items are
      additive
- [ ] The package version is bumped and `consumer-compatibility.md` verification is written from the
      consumers' source rather than assumed

## Out of Scope

- **Any change to the server side of the Hub** — `ToolkitHub`, its groups, or its message plumbing.
- **A reconnect policy of the toolkit's own.** Item 3 passes SignalR's delays through; it does not
  introduce a FatCat retry loop, backoff, or timer.
- **Replaying messages a client missed while disconnected.** The consumer reconciles over REST on
  reconnect by design and is not asking for a buffer.
- **Removing `Action onConnectionLost`.** It stays for compatibility; the event sits beside it.

## Notes

- **Apostil ships against the toolkit as it is.** Its plan
  (`tasks/done/hub_connection_health/00-overview.md`, ADR-12) states that no toolkit change is required and
  that a phase which finds itself needing one has misread the plan. This file is the write-up that ADR
  called for — a proposal for a separate work item in a separate repository, with **no toolkit code
  written**.
- Read together, the three items remove: **a discarded task** in a consumer's receive path, **a disposal
  `NullReferenceException` risk** first reported by `toolkit_signalr_migration` phase 1, and **a
  ~70-second lost-connection latency nobody chose**.
- Items 1 and 2 are small and self-contained. Item 3 touches the public signature of the two most-used
  members on the interface and deserves its own phase and its own consumer-compatibility pass.
