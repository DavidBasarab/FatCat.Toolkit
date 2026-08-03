# SignalR Hub Improvements — Phase Plan

Derived from [00-overview.md](00-overview.md), the corrections in
[01-verification-findings.md](01-verification-findings.md), and the consumer check in
[03-consumer-compatibility.md](03-consumer-compatibility.md). Read the findings first — four phases here
are wider than the corresponding overview item.

## Two hard constraints from the consumer check (Fog, Apostil)

Both come from `C:\Code\Fog`; violating either is a breaking change.

1. **Phase 1b must null-guard the client hooks.** `ToolkitHubClientConnection.InvokeServerMessage` and
   `InvokeDataBufferMessage` return a `null` Task when unsubscribed and are awaited. Registering
   `connection.On(...)` before `StartAsync` removes the accidental protection that made this unreachable,
   and Fog subscribes `ServerMessage` *after* connect. See Phase 1b.
2. **`ToolkitHubClientFactory`'s constructor is frozen.** Fog builds it by hand —
   `new ToolkitHubClientFactory(scope)`. Optional parameters on its *methods* are fine; a new constructor
   parameter is a source break. Affects Phases 0 and 2.

Baseline: branch `SignalRChanges`, `VersionPrefix` **1.0.342** in both `ToolKit.csproj` and
`Toolkit.WebServer.csproj`.

## Phase map

| Phase | Covers overview items | Public API | Ships as | Unblocks Apostil |
|---|---|---|---|---|
| 0 — Test seams | (prerequisite) | additive, internal-ish | fold into 1 | — |
| 1 — Correctness bugs | 6 (×4 hooks), 1 | none | **1.0.343** | deletes `ApostilHubConnection` |
| 2 — Client connection options | 2, 3 | additive | **1.0.344** | token out of query string, reconnect |
| 3 — Server group support | 5 | new interface | **1.0.345** | drops `IHubContext<ToolkitHub>` |
| 4 — Configurable hub auth | 4 | additive setting | 1.0.346 | softens ADR-2 |
| 5 — Await hub lifecycle hooks | 7 (+`OnDisconnectedAsync`) | timing change | 1.0.347 | server half of ADR-5 race |
| 6 — Deterministic waiting | 8 | none | 1.0.348 | — |
| 7 — Release & handoff | — | — | — | notify Apostil |

Phases 1–3 are the value. Phases 4–6 are independent and can be reordered or dropped without
affecting them. **Do not merge phase 5 before phase 1** — phase 5 activates two latent null-Task
crashes that phase 1 fixes.

---

## Phase 0 — Test seams (prerequisite, no behavior change)

**Why:** `ToolkitHubClientConnection.Connect` builds its `HubConnection` with an inline
`new HubConnectionBuilder()`, so phases 1–2 cannot be written test-first. TDD is non-negotiable in this
codebase and the class is too logic-heavy for the `[ExcludeFromCodeCoverage]` wrapper exemption.

**Changes**

- Add `IHubConnectionBuilderFactory` (name to taste) in `ToolKit/Web/Api/SignalR/` exposing one method
  that returns a configured `IHubConnectionBuilder` for a URL plus an options callback.
- Add the concrete implementation, marked
  `[ExcludeFromCodeCoverage(Justification = "Direct wrapper over HubConnectionBuilder — no business logic, faked in consuming tests.")]`.
- Inject it into `ToolkitHubClientConnection`'s primary constructor alongside `IGenerator`/`IToolkitLogger`.
  **Not** into `ToolkitHubClientFactory` — see constraint 2.
- No `SignalRModule` entry needed. Verified: `SystemScope.Initialize` always adds the ToolKit assembly to
  the scan set and registers `RegisterAssemblyTypes(...).AsImplementedInterfaces().HasPublicConstructor().PublicOnly()`,
  so a `public` type with a `public` constructor resolves automatically in both Fog and Apostil.
- Create `Tests.ToolKit/Web/Api/SignalR/ToolkitHubClientConnectionTests.cs` (namespace
  `Tests.FatCat.Toolkit.Web.Api.SignalR`).

**Risk:** low. Only `ToolkitHubClientConnection`'s constructor changes, and no consumer constructs it by
hand — `ToolkitHubClientFactory` resolves it from the scope.

**Done when:** a test can assert the order of operations on a faked builder without a live server.

---

## Phase 1 — Correctness bugs (overview items 6 and 1)

The two highest-value changes. No public API change; pure fixes.

### 1a — Null-safe hub hooks (item 6, widened to four hooks)

**File:** `Toolkit.WebServer/ToolkitWebApplicationSettings.cs`

Fix all four hooks, not just `OnClientHubMessage`. Two crash today, two are latent and would be
activated by phase 5:

| Hook | Line | Fix |
|---|---|---|
| `OnClientHubMessage` | 61 | `ClientMessage?.Invoke(message) ?? Task.FromResult<string>(null)` |
| `OnOnClientDataBufferMessage` | 66 | `ClientDataBufferMessage?.Invoke(...) ?? Task.FromResult<string>(null)` |
| `OnClientConnected` | 51 | `ClientConnected?.Invoke(...) ?? Task.CompletedTask` |
| `OnClientDisconnected` | 56 | `ClientDisconnected?.Invoke(...) ?? Task.CompletedTask` |

The `?.` alone is not enough — it returns a `null` `Task`, and `ToolkitHub` awaits the data-buffer hook
at line 43. Drop the now-redundant `!` suppressions.

Optionally normalize the same shape in `ToolkitHubServer.InvokeClientConnected` /
`InvokeClientDisconnected` (:163-171) while in the area — benign today, cheap to make consistent.

**Tests (new, `Tests.FatCat.Toolkit.WebServer`):** one test per hook asserting it returns a completed
Task (and a `null` string result for the two message hooks) when no subscriber is attached; one per hook
asserting the subscriber's result flows through unchanged.

### 1b — Register receive handlers before `StartAsync` (item 1)

**File:** `ToolKit/Web/Api/SignalR/ToolkitHubClientConnection.cs`, `Connect` (:42-56)

Move `RegisterForServerMessages()` from line 55 to before `await connection.StartAsync()`. Also move it
above the `Closed` subscription or leave it after — either is fine; only the `StartAsync` relationship
matters.

**Mandatory companion fix — null-guard the client hooks (constraint 1).** Moving registration earlier means
a server push can now be delivered before the consumer has attached its event handler. Fog does exactly
that: it subscribes `ServerMessage` only after `TryToConnectToClient` returns. Without this guard,
Phase 1b converts a silent drop into a `NullReferenceException` in Fog's message pump.

| Method | Line | Fix |
|---|---|---|
| `InvokeServerMessage` | 145-148 | `ServerMessage?.Invoke(message) ?? Task.FromResult<string>(null)` |
| `InvokeDataBufferMessage` | 140-143 | `ServerDataBufferMessage?.Invoke(...) ?? Task.FromResult<string>(null)` |

The existing `if (response is not null)` guards at :176 and :194 already do the right thing with a null
result — no client response is sent. Drop the now-redundant `!` suppressions.

**Tests:** using the phase-0 seam, assert `connection.On(...)` is called for all three method names before
`StartAsync` is invoked; and assert an inbound server message with no subscriber completes without throwing
and sends no response.

**Acceptance:** Apostil can point at the new package, delete `ApostilHubConnection` /
`IConnectToApostilHub`, and see its `ConnectionReady` push arrive on 100% of connections.

**Ship as 1.0.343.** This release alone justifies the work.

---

## Phase 2 — Client connection options (overview items 2 and 3)

Additive; existing callers get today's behavior.

**Files:** `ToolkitHubClientConnection.cs` (interface + class), `ToolkitHubClientFactory.cs`
(interface + class).

**Changes**

1. Add an optional `Action<HttpConnectionOptions> configureOptions = null` to `Connect` and
   `TryToConnect`; pass it into `.WithUrl(hubUrl, options => configureOptions?.Invoke(options))`.
2. Add an optional `bool automaticReconnect = false`; when true, chain `.WithAutomaticReconnect()`
   before `.Build()`.
3. **Thread both through `IToolkitHubClientFactory`** (`ConnectToClient`, `TryToConnectToClient`) —
   without this the factory path, which is the DI-registered entry point, cannot reach either feature.
   This is the gap the overview misses. **Optional method parameters only — the factory's constructor is
   frozen (constraint 2).** Fog calls `TryToConnectToClient(url, onServerDisconnect)` positionally, so
   trailing optional parameters stay source-compatible.
4. Surface `Reconnecting`/`Reconnected` as toolkit events on `IToolkitHubClientConnection` so callers
   can render connection state.

**Decide before coding**

- Optional parameters vs. a single `ToolkitHubConnectionOptions` settings class. Four booleans/callbacks
  threaded through four methods argues for the settings object; the codebase's existing style
  (`TimeSpan? timeout = null`) argues for optional parameters. Optional parameters are the smaller,
  more in-keeping change — pick that unless a fifth knob appears.
- Accept that `HttpConnectionOptions` (`Microsoft.AspNetCore.Http.Connections.Client`) enters the public
  surface. It is already a transitive dependency; this is a deliberate, low-cost choice.

**Document:** with `automaticReconnect: true`, `onConnectionLost` fires only after reconnect attempts are
exhausted, not on every transient drop. `ToolkitHubClientFactory`'s cache eviction inherits that — which
is the behavior you want, but it must be stated in the release notes.

**Tests:** the caller's `configureOptions` is invoked with the real options instance; the builder is
configured for reconnect only when opted in; both reach through the factory overloads.

**Ship as 1.0.344.**

---

## Phase 3 — Server group support (overview item 5)

**Files:** `Toolkit.WebServer/SignalR/ToolkitHubServer.cs`, `SignalRModule.cs`

**Changes**

1. New interface `IToolkitHubGroups` in the same file as `ToolkitHubServer` (file stays named after the
   class, per the naming rules):

   ```csharp
   Task AddToGroup(string connectionId, string groupName);
   Task RemoveFromGroup(string connectionId, string groupName);
   Task SendToGroup(string groupName, ToolkitMessage message);
   ```

   `ToolkitHubServer` implements it via the `IHubContext<ToolkitHub>` it already holds
   (`hubContext.Groups.AddToGroupAsync`, `hubContext.Clients.Group(...)`).

2. Implement `SendToAllClients` via `hubContext.Clients.All` instead of throwing
   `NotImplementedException` (:138-141).

3. **`SignalRModule`: chain the new interface onto the existing registration** —
   `.As<IToolkitHubServer>().As<IToolkitHubGroups>().SingleInstance()`. Two separate `RegisterType`
   calls produce two singletons with two separate connection dictionaries.

**Correction to the overview:** its option (b), default-interface methods, does **not** work — a default
implementation has no access to `ToolkitHubServer`'s `hubContext` and could only throw. Separate
interface is the only viable route. `IToolkitHubServer` itself stays untouched, so no external
implementer breaks.

**Decide before coding:** whether `SendToGroup` is fire-and-forget (as the overview proposes) or gets a
request/response sibling like `SendToClient`. Fire-and-forget only — response correlation across a group
has no coherent semantics with the current single-session-id design.

**Tests:** faked `IHubContext<ToolkitHub>`; assert group name and connection id reach the right hub-context
calls, and that `SendToAllClients` no longer throws.

**Ship as 1.0.345.** After this, Apostil drops `IHubContext<ToolkitHub>` from its own code.

---

## Phase 4 — Configurable hub authorization (overview item 4)

**Files:** `Toolkit.WebServer/ToolkitWebApplicationSettings.cs`,
`Toolkit.WebServer/ApplicationStartUp.cs` (`SetUpSignalR`, :214-230)

Add `public bool SignalRRequireAuthorization { get; set; } = true;` to the settings and gate
`endpointOption.RequireAuthorization()` (line 227) on it in addition to the existing `Authentication`
flag check. Default `true` keeps current behavior exactly.

**Prefer a settings bool over a new `WebApplicationOptions` flag.** The enum's next free bit is 32, but a
flag named `AnonymousSignalR` reads as "turn a feature on" while the semantics are "turn a check off",
and it complicates `CommonOptions`.

**Consumer warning:** Fog must never set this to `false`. `BrumeHandleClientConnections.GetUserId` reads
`user.Claims.FirstOrDefault(...).Value` with no null check, so an anonymous hub connection would
`NullReferenceException` on every connect. The `true` default keeps Fog and Apostil exactly as they are.

**Tests:** `ApplicationStartUp` is not currently unit-testable (it wires the ASP.NET pipeline directly).
Cover the settings default with a test; verify the wiring by inspection and a manual anonymous-connect
check against `OneOffToolkitOnly`. Do not build an integration harness for this one line.

**Ship as 1.0.346.**

---

## Phase 5 — Await hub lifecycle hooks (overview item 7, widened)

**Depends on phase 1a.** Awaiting these hooks turns two currently-benign null Tasks into crashes;
phase 1a is what makes it safe. This is proven by a real consumer, not theoretical: Apostil subscribes
only `ClientConnected` and `ClientMessage`, so awaiting `OnClientDisconnected` without phase 1a would
`NullReferenceException` on **every** Apostil disconnect.

**File:** `Toolkit.WebServer/SignalR/ToolkitHub.cs`

- `OnConnectedAsync` (:97-117): make it `async`, `await ToolkitWebApplication.Settings.OnClientConnected(...)`
  inside the existing try/catch, then `await base.OnConnectedAsync()`.
- `OnDisconnectedAsync` (:119-135): same treatment for `OnClientDisconnected` at line 127 — the overview
  does not mention it, but it has the identical discarded-Task race in mirror image.

**Timing change, flagged for release notes:** `HubConnection.StartAsync()` on the client will no longer
return until the server's connect handler completes. An app with a slow `ClientConnected` subscriber will
see connect latency increase. That is the point of the change, but it is observable.

**Tests:** `ToolkitHub` resolves through `SystemScope.Container` and static `ToolkitWebApplication.Settings`,
so a test must drive both statics. Assert the subscriber's Task is completed before the hub method returns.
If driving the statics proves unreasonably invasive, state that in the PR rather than skipping the change —
it is small and correct.

**Ship as 1.0.347.**

---

## Phase 6 — Deterministic waiting (overview item 8)

**Files:** `ToolkitHubServer.WaitForClientResponse` (:185-211),
`ToolkitHubClientConnection.WaitForResponse` (:239-269)

Replace `while (true)` + `await Task.Delay(35)` + `DateTime.UtcNow` with a `TaskCompletionSource<ToolkitMessage>`
keyed by session id in the existing `waitingForResponses` dictionary, completed by the response handler and
cancelled by a `CancellationTokenSource(timeout)` that still throws `TimeoutException`.

Beyond the overview's hygiene argument, this also settles a house-rules violation: raw `Task.Delay` should be
`IThread`. Moving to a `TaskCompletionSource` removes the sleep entirely rather than abstracting it, which is
the better answer.

**Watch:** `timedOutResponses` exists to swallow a late response for an already-timed-out session. Keep that
behavior — a `TaskCompletionSource` that has already been cancelled must not throw when a late
`TrySetResult` arrives (use `TrySet*`, never `Set*`).

**Preserve the exception type.** Fog's `SignalConnection.SendFileBytes` catches and rethrows on this path,
and `SendMessage` lets it propagate. The rewrite must still throw `TimeoutException` — not
`TaskCanceledException` or `OperationCanceledException` — and must return `ToolkitMessage` unchanged.

**Purely internal — no public surface change.** This is the first phase that becomes properly unit-testable
without a live socket, so it is worth real test coverage on both timeout and late-response paths.

**Ship as 1.0.348** (or fold into whichever release follows).

---

## Phase 7 — Release and handoff

1. **Human action, not a phase.** Run `src/PushNugetPackages.ps1` — it calls `Submit-NugetPackage` in both
   project folders, which steps `VersionPrefix` and publishes. **No phase hand-edits a version**, and no
   subagent runs this script; both packages go out on the same version.
2. Release notes must call out the two behavioral changes that are not pure additions:
   - phase 2 — `onConnectionLost` semantics under `automaticReconnect: true`;
   - phase 5 — `StartAsync` now waits for the server connect handler.
3. Notify the Apostil side with the version number. Their transition work item is already written and
   waiting at `C:\Code\Apostil\tasks\todo\toolkit_signalr_migration\` (run with
   "run toolkit_signalr_migration"); its phases are gated on which of these fixes shipped. After 1.0.345
   they delete `ApostilHubConnection` / `IConnectToApostilHub`, move the hub token to `AccessTokenProvider`,
   enable reconnect, and drop `IHubContext<ToolkitHub>`. Both repos' `JwtBearerEvents` already fall through
   to header-based bearer extraction when no `access_token` query value is present, so the token move needs
   no server change.
4. **Fog is on 1.0.339, not 1.0.342.** Upgrading it also picks up the `ConfigureMiddleware` and
   `ConfigureLogging` settings hooks from 340–342 — both additive and unrelated to SignalR, but it is a
   two-release jump and gets its own build-and-smoke pass rather than being assumed. Leave
   `Fog/Spikes/dude2` (pinned 1.0.240 / 1.0.64) alone.

---

## Standing constraints for every phase

- **TDD.** Tests first. Phase 0 exists because phases 1–2 cannot otherwise honor this.
- Test namespaces mirror source with `Tests.` prepended; server-side tests go in `Tests.ToolKit`
  (it already references `Toolkit.WebServer`; there is no separate WebServer test project).
- No expression-bodied members, including in tests. Block bodies only.
- Primary constructors for injection; no explicit constructor bodies.
- `var`, string interpolation, file-scoped namespaces, no nullable annotations on always-populated values.
- CSharpier is the formatting authority — do not hand-format.
- Existing SignalR test coverage is a single file (`GetUserClaimTests.cs`). There is no regression net
  under any of this code, so each phase carries its own tests or it does not merge.
