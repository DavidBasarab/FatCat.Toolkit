# signalr-hub-improvements Orchestrator Runbook

**Trigger:** in a Claude session opened in `C:\Code\FatCat.Toolkit`, the user says
"run signalr-hub-improvements" (optionally "starting at phase N", or "run signalr-hub-improvements phase 4"
for one of the independent phases). If you are the session that was told this, follow this runbook exactly —
it is the complete instruction set.

## Read before launching anything

Four documents in `src/task/signalr-hub-improvements/`, in this order:

| File | What it is |
|---|---|
| `00-overview.md` | The original request from the Apostil team — the eight items, with rationale |
| `01-verification-findings.md` | Verification of all eight against source. **Four items are wider than the overview says and one proposed approach does not work.** The findings win where they disagree with the overview |
| `02-phase-plan.md` | The executable plan. Phases are **sections** of this file, not separate files |
| `03-consumer-compatibility.md` | Fog and Apostil checked against every phase. **Two hard constraints; violating either is a breaking change** |

**The two constraints are the reason this runbook exists.** Repeat them to every subagent:

1. **Phase 1b must null-guard the client hooks** (`InvokeServerMessage`, `InvokeDataBufferMessage`).
   Registering `connection.On(...)` before `StartAsync` removes the accidental protection that made a
   null-Task `await` unreachable, and Fog subscribes `ServerMessage` *after* connect. Without the guard,
   phase 1 is a breaking change for Fog.
2. **`ToolkitHubClientFactory`'s constructor is frozen.** Fog builds it by hand —
   `new ToolkitHubClientFactory(scope)` in `SignalConnectionFactory.cs`. Optional parameters on its
   *methods* are fine; a new constructor parameter is a source break.

## Ground rules (non-negotiable)

- Execute phases in dependency order: 0 → 1 → 2 → 3 → 4 → 5 → 6. **Phase 1 must complete before phase 5**
  — phase 5 awaits hub hooks that phase 1a makes null-safe, and Apostil does not subscribe
  `ClientDisconnected`, so awaiting it without phase 1a throws on every disconnect. Phases 3, 4 and 6 are
  independent of each other and of 2; they may be reordered or skipped on request.
- **Fresh context per phase.** Launch one general-purpose subagent (Agent tool) per phase, wait for it to
  finish, and verify its result before launching the next. Never execute a phase's implementation work in
  this orchestrating session, and never let one subagent touch two phases.
- **Phases are sections, not files.** Each subagent reads the four documents above but implements **only its
  own named section** of `02-phase-plan.md`. Tell it the section heading verbatim and tell it explicitly not
  to implement any neighbouring phase's section, however small. This is the one place this runbook is weaker
  than a one-file-per-phase layout — compensate with the explicit scope statement and the one-commit check.
- One commit per phase; the commit message references `src/task/signalr-hub-improvements/02-phase-plan.md`
  and names the phase.
- Never push to any remote, never amend, squash, rebase, or force-push.
- **Never run `PushNugetPackages.ps1` or `Submit-NugetPackage`, and never hand-edit `VersionPrefix` in
  either `.csproj`.** That script steps both projects' versions and publishes; it is the human's post-merge
  action. (This supersedes step 1 of `02-phase-plan.md`'s Phase 7, which reads as if a subagent bumps the
  version — it does not.)
- **Branch:** check `git rev-parse --abbrev-ref HEAD` before phase 0.
  - On `main` → create and switch to **`task/signalr-hub-improvements`**.
  - On any other branch → **use that branch**; do not create a new one. (At planning time the repo was on
    `SignalRChanges`.)

  Tell every subagent which branch it is working on.

## Preconditions (check before phase 0, tell each subagent what you found)

- **The working tree is clean.** If `src/task/signalr-hub-improvements/` has uncommitted plan files, commit
  them first (or let phase 0's commit carry them — decide once and tell the user which).
- **The existing suite is green** — `dotnet build src/ToolKit.sln` and `dotnet test src/ToolKit.sln`. Record
  the counts. A pre-existing red obscures every phase verification that follows.
- **Know how thin the net is.** `Tests.ToolKit` is the *only* test project, and its entire SignalR coverage
  is one file: `Tests.ToolKit/Web/Api/SignalR/GetUserClaimTests.cs`. There is effectively no regression net
  under any of this code. Every phase carries its own tests or it does not merge. Say this to each subagent.
- **There is no test project for `Toolkit.WebServer`.** `Tests.ToolKit` already references it, so
  server-side tests go there under `Tests.FatCat.Toolkit.WebServer.SignalR`. Do not create a new test
  project.
- **No external dependencies** — no database, no deployed instance. Smoke checks use the in-repo samples:
  `OneOff/Old/ServerWorker.cs` hosts a hub (`SignalR | Cors`, anonymous) and
  `OneOffToolkitOnly/ClientWorker.cs` connects to one. **Confirm this pair actually runs before relying on
  it** — `OneOff/Old/` may be dormant. If it does not, say so and fall back to a purpose-built
  `[Fact]`-level verification rather than inventing a new sample project.

## Phases

| # | Section in `02-phase-plan.md` | Depends on | Risk |
|---|---|---|---|
| 0 | `Phase 0 — Test seams (prerequisite, no behavior change)` | — | Low — but everything after depends on it |
| 1 | `Phase 1 — Correctness bugs (overview items 6 and 1)` | 0 | **High** — the two correctness bugs; **constraint 1 lives here** |
| 2 | `Phase 2 — Client connection options (overview items 2 and 3)` | 0, 1 | Medium — public API; **constraint 2 lives here** |
| 3 | `Phase 3 — Server group support (overview item 5)` | — | Medium — new public interface + Autofac registration |
| 4 | `Phase 4 — Configurable hub authorization (overview item 4)` | — | Low — one bool, default preserves behaviour |
| 5 | `Phase 5 — Await lifecycle hooks (overview item 7, widened)` | **1** | Medium — timing change visible to consumers |
| 6 | `Phase 6 — Deterministic waiting (overview item 8)` | — | Low — internal only |

Phase 7 in the plan is the human's release and handoff. **Do not execute it.**

## Per-phase procedure

1. Record the current HEAD: `git rev-parse HEAD`.
2. Launch a general-purpose subagent with exactly this prompt (substitute the section heading and branch):

   > Execute the implementation phase headed `<section heading>` in
   > `src/task/signalr-hub-improvements/02-phase-plan.md`, in the repository `C:\Code\FatCat.Toolkit`.
   >
   > Read these four files completely first — together they are the entire handoff document; you have no
   > other context: `src/task/signalr-hub-improvements/00-overview.md`,
   > `01-verification-findings.md`, `02-phase-plan.md`, `03-consumer-compatibility.md`. Where the overview
   > and the findings disagree, the findings win.
   >
   > **Implement ONLY the section named above.** Do not implement, refactor, or "while I'm here" any other
   > phase's section, however small it looks. Other phases have their own commits.
   >
   > Two constraints apply to every phase and are breaking changes if violated:
   > (1) `ToolkitHubClientConnection.InvokeServerMessage` and `InvokeDataBufferMessage` must never return a
   > null Task that a caller awaits; (2) `ToolkitHubClientFactory`'s constructor must not gain a parameter —
   > Fog constructs it with `new`.
   >
   > Follow every rule in `src/CLAUDE.md` and `src/.claude/rules` (ignore the stale "Haivision" header text —
   > the rule files are the operative standards). TDD is mandatory: tests first, red observed before green.
   > `Tests.ToolKit` is the only test project and current SignalR coverage is a single file, so your phase
   > must bring its own tests.
   >
   > Do not edit `VersionPrefix` in any `.csproj`. Never run `PushNugetPackages.ps1` or
   > `Submit-NugetPackage`.
   >
   > You are working on branch `<branch>`; do not create or switch branches. You may self-correct at most 2
   > times; if the Definition of Done still cannot be met after that, leave the working tree completely
   > clean (discard or stash your changes), do NOT commit, and report PHASE FAILED with an explanation. On
   > success create exactly one commit whose message references
   > `src/task/signalr-hub-improvements/02-phase-plan.md` and names the phase. Never amend or squash
   > existing commits and never push to any remote.

3. When the subagent finishes, verify in this session:
   - `git rev-list --count <recorded HEAD>..HEAD` is exactly `1`
   - `git status --porcelain` is empty
   - `git branch --show-current` is the expected branch
   - The new commit's message references the plan file and names the phase
   - `git diff <recorded HEAD>..HEAD -- '*.csproj'` shows **no** `VersionPrefix` change
   - The subagent did not report PHASE FAILED
4. Run the **consumer guard** (below). It is cheap and catches the two breaking changes directly.
5. All checks pass → tell the user the phase is done (one line: commit hash + subject) and move on.
6. Any check fails → follow **Halt on failure**. Do not continue.

## Consumer guard (run after every phase, not just the obvious ones)

Two mechanical checks. Both are fast; run them every time, because the breaking changes they catch can be
introduced by a well-meaning refactor in any phase.

1. **Fog's hand-constructed factory still compiles.** Read
   `src/ToolKit/Web/Api/SignalR/ToolkitHubClientFactory.cs` and confirm its constructor is still
   `ToolkitHubClientFactory(ISystemScope scope)` — one parameter, unchanged. Fog does
   `new ToolkitHubClientFactory(scope)` in
   `C:\Code\Fog\Common\Common\SignalMessages\Connections\SignalConnectionFactory.cs`.
2. **No awaited null Task on either side.** Grep
   `src/ToolKit/Web/Api/SignalR/ToolkitHubClientConnection.cs` and
   `src/Toolkit.WebServer/ToolkitWebApplicationSettings.cs` for `?.Invoke(` and confirm **every** hit is
   followed by a `??` fallback. A bare `?.Invoke(...)!` on a `Task`-returning hook is the defect —
   the `?.` guards the delegate, not the awaited Task.

Either check failing is a halt, regardless of what the subagent reported.

### Extra gates (the phases earn them)

**After phase 0**, confirm the new builder-factory type is `public` with a `public` constructor and lives in
the `ToolKit` project. `SystemScope.Initialize` always scans that assembly with
`AsImplementedInterfaces().HasPublicConstructor().PublicOnly()`, which is what makes it resolve in Fog and
Apostil with no consumer change. A type that is `internal`, or placed in `Toolkit.WebServer`, silently
breaks both.

**After phase 1**, before launching phase 2 or 5, confirm from the report that:
- **All four** settings hooks are null-safe, not just `OnClientHubMessage`. Apostil subscribes only
  `ClientConnected` and `ClientMessage`, so the data-buffer hook is a live crash for it today.
- **Both** client-side invokers are null-safe (constraint 1).
- A `[Fact]` asserts `connection.On(...)` happens before `StartAsync`.

**After phase 2**, confirm the options callback and the reconnect switch reached **`TryToConnect`**, not
only `Connect`, and that they were threaded through `IToolkitHubClientFactory`'s *methods*. Apostil calls
`TryToConnect`; Fog calls `TryToConnectToClient`. A change that only lands on `Connect` is unreachable for
both consumers.

**After phase 3**, confirm `SignalRModule` **chains** the new interface onto the existing registration —
`.As<IToolkitHubServer>().As<IToolkitHubGroups>().SingleInstance()`. Two separate `RegisterType` calls
produce two singletons with two separate connection dictionaries, which fails in a way no unit test will
catch.

**After phase 4**, confirm the new setting defaults to **`true`**. Fog's
`BrumeHandleClientConnections.GetUserId` reads `user.Claims.FirstOrDefault(...).Value` with no null check —
an anonymous hub connection would throw on every connect.

**After phase 5**, confirm `OnDisconnectedAsync` was awaited too, not just `OnConnectedAsync`, and that
phase 1 is already in the history.

**After phase 6**, confirm the rewrite still throws **`TimeoutException`** — not `TaskCanceledException` or
`OperationCanceledException`. Fog's `SignalConnection.SendFileBytes` catches and rethrows on that path.

If any report cannot substantiate its gate, treat it as a failed verification and halt.

## Halt on failure

If a phase reports PHASE FAILED, or its verification or consumer-guard checks fail:

1. If the working tree is dirty:
   `git stash push --include-untracked -m "signalr-hub-improvements phase <n> failure"`.
2. Write `src/task/signalr-hub-improvements/failure-report.md` containing: the phase number and section
   heading, what the subagent reported, which check failed, the `git status` output from before the stash,
   and the stash reference if one was created.
3. Stop the pipeline. Do not start dependent phases. Report the failure and point the user at the report.
   They can resume with "run signalr-hub-improvements starting at phase N".

**Special case — a consumer guard failed.** Say plainly which consumer breaks and how
(`03-consumer-compatibility.md` has the detail). Do not let the next phase start on top of a breaking
change; it gets harder to unpick with every commit.

**Special case — more than one new commit:** do not revert anything on your own; halt, report, and let the
human decide.

## Completion report

After the last executed phase verifies, report to the user:

- The phase commits (hash + subject) and the branch they are on.
- Which of the eight overview items are now done, and which remain.
- The consumer-guard result for the final state: `ToolkitHubClientFactory`'s constructor unchanged, and
  every `?.Invoke(` on a Task-returning hook paired with a `??` fallback.
- Test counts before and after — and, given the suite started with a single SignalR test file, how much
  coverage each phase actually added.
- The combined deviation log from the phase reports.
- **The two behavioural changes that must appear in the release notes**, because they are not pure
  additions:
  - phase 2 — with `automaticReconnect: true`, `onConnectionLost` fires only after retries are exhausted,
    not on every transient drop;
  - phase 5 — `HubConnection.StartAsync()` no longer returns until the server's connect handler completes,
    so a slow `ClientConnected` subscriber now shows up as connect latency.
- **The human's next steps, in order:**
  1. Review and merge the branch.
  2. Run `src/PushNugetPackages.ps1` — it steps both projects' versions and publishes. Both packages must
     go out on the **same** version.
  3. Tell the Apostil side the version number. Their transition work item is already written and waiting at
     `C:\Code\Apostil\tasks\todo\toolkit_signalr_migration\` — it is run with
     "run toolkit_signalr_migration" and its phases are gated on which of these fixes shipped.
  4. Decide whether to move Fog. Fog is on **1.0.339**, two releases behind, so its upgrade also picks up
     the `ConfigureMiddleware` / `ConfigureLogging` hooks — additive and unrelated to SignalR, but it
     deserves its own build-and-smoke pass rather than being assumed. Leave `Fog/Spikes/dude2`
     (pinned 1.0.240 / 1.0.64) alone.
- Reminder: nothing was pushed and nothing was published; both are the human's call.
