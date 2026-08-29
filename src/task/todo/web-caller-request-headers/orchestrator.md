# web-caller-request-headers Orchestrator Runbook

**Trigger:** the user says "run web-caller-request-headers". If you are the Claude session told this, follow this
runbook exactly — it is the complete instruction set.

## Ground rules (non-negotiable)

- One phase only (`01-arbitrary-request-headers.md`). Run it in its own isolated context: launch **one
  general-purpose subagent (Agent tool)**, wait for it to finish, verify its result.
- One commit; the commit message references the phase file. **Never squash, never amend.**
- Never push to any remote, never publish the NuGet package — the human runs `PushNugetPackages.ps1`.
- **Branch:** run `git rev-parse --abbrev-ref HEAD`.
  - On `main` → create and switch to **`web-caller-request-headers`**.
  - On any other branch → **use that branch**; do not create a new one.
  Tell the subagent which branch it is on.

## Preconditions (check before the phase; tell the subagent what you found)

- **`dotnet build ToolKit.slnx` clean with 0 warnings and `dotnet test ToolKit.slnx` green.** Re-measure the
  baseline yourself and record the real count. The WebCaller specs hit `https://httpbin.org` — confirm the
  baseline suite is green in this environment; if httpbin is unreachable, note it (the suite already depends on it,
  this task does not introduce the dependency).
- **Confirm `IWebCaller` has no arbitrary-header member yet:** read `src/ToolKit/Web/WebCaller.cs`; the interface
  exposes `Accept`, the auth setters, and the Get/Post/Put/Delete overloads, but **no `AddHeader`**. If `AddHeader`
  (or an equivalent) already exists, the work is done — stop and tell the user.
- **Confirm the pattern-to-mirror files exist:** `src/ToolKit/Web/WebCaller.cs` (the `EnsureAccept`/
  `EnsureAuthorization`/`SendWebRequest` shape) and `src/Tests.ToolKit/Web/Api/WebCallerSpecs/WebCallerTests.cs`
  (the httpbin-echo test pattern). Check whether `HttpBinResponse` exposes the echoed request headers; if not, the
  phase adds a minimal accessor.
- **Services needed:** network access to `httpbin.org` for the WebCaller specs (existing suite behaviour). No
  MongoDB, no SignalR host.
- **Commit the plan files** (`task/todo/web-caller-request-headers/`) on their own before the phase if the repo
  convention is to track them; leave any unrelated pre-existing modifications alone.

## Per-phase procedure

1. Record the current HEAD: `git rev-parse HEAD`.
2. Launch a general-purpose subagent with this prompt (substitute the branch name):

   > Execute the implementation phase described in `task/todo/web-caller-request-headers/01-arbitrary-request-headers.md`.
   > Read that file completely first — it is the entire handoff document; you have no other context. Read
   > `task/todo/web-caller-request-headers/00-overview.md` as well; its ADRs are binding. You are working on
   > branch `<branch>`; do not create or switch branches. Leave any unrelated pre-existing modified files alone.
   > Follow every rule in CLAUDE.md and .claude/rules. This is a **published library** — the change is additive to
   > `IWebCaller`; do not change any existing signature or behaviour. Bindings: `AddHeader(string, string)` on
   > `IWebCaller`; apply stored headers in `SendWebRequest` via `TryAddWithoutValidation` (NOT `Add`); persist
   > headers on the instance (do not reset them after send); the new code path logs NO header name or value.
   > Write the spec first (TDD, red before green) against httpbin's echo, mirroring the existing WebCaller specs.
   > The phase file's Definition of Done is mandatory. You may self-correct at most 2 times; if it still cannot be
   > met, leave the working tree exactly as you found it (discard/stash only YOUR changes), do NOT commit, and
   > report PHASE FAILED. On success create exactly one commit referencing the phase file. Never amend/squash,
   > never push, **never publish the NuGet package.**

3. When the subagent finishes, verify **in this session**:
   - `git rev-list --count <recorded HEAD>..HEAD` is exactly `1`
   - `git status --porcelain`, ignoring known pre-existing unrelated files, is otherwise clean
   - the new commit's message references the phase file
   - `IWebCaller` declares `AddHeader(string, string)`; `grep -n "TryAddWithoutValidation" src/ToolKit/Web/WebCaller.cs`
     is present; the new code logs no header value (read-through)
   - `git diff --name-only <recorded HEAD>..HEAD` is `WebCaller.cs`, the WebCaller spec(s), and (only if needed)
     `HttpBinResponse` — no existing signature/behaviour changed
   - `dotnet build ToolKit.slnx` green at **0 warnings** and `dotnet test ToolKit.slnx` green
   - the subagent did not report PHASE FAILED
4. All checks pass → tell the user the phase is done (commit hash + subject) and the publish flow is theirs.
5. Any check fails → **Halt on failure**.

## Halt on failure

If the phase reports PHASE FAILED or its verification fails:

1. If the subagent left changes beyond known pre-existing files:
   `git stash push --include-untracked -m "web-caller-request-headers failure"`.
2. Write `task/todo/web-caller-request-headers/failure-report.md`: what the subagent reported, which check failed,
   the `git status` before the stash, and the stash reference.
3. Stop. Report to the user and point them at the failure report.

**Special cases:**

- **`request.Headers.Add` used instead of `TryAddWithoutValidation`** — halt; custom names like `x-api-key` throw
  on `Add` (ADR-2). A green suite that only tested a *typed* header name would hide this — confirm a custom
  (non-typed) header name is in the specs.
- **A header value reaches a log** (the new path logs name→value at any level) — halt; ADR-4 forbids it (the
  consumer's D27 depends on it).
- **An existing signature or behaviour changed** (`EnsureAccept`, `EnsureAuthorization`, an auth setter, a
  Get/Post/Put/Delete overload) — halt; the change is additive only (published library).
- **More than one commit, or a publish/push happened** — halt; do not revert on your own, let the human decide.
  Publishing is never the plan's to do.

## Completion report

After the phase verifies, report to the user:

- The one commit (hash + subject) and the branch (`web-caller-request-headers`).
- The acceptance criteria and how each was proven (the `AddHeader` member; the single/multiple/coexists-with-auth
  header facts against httpbin; the no-log-of-value read-through; the additive `git diff`).
- Any deviation (e.g. a `Headers` accessor added to `HttpBinResponse`; whether `ClearHeaders()` was included).
- **The publish flow is the human's** (`00-overview.md` → Publish flow): review the commit, run
  `PushNugetPackages.ps1`, then bump `C:\Code\Apostil`'s `Apostil.Api.csproj` to the published version — only then
  does `run claude_backend` proceed (its Toolkit gate checks for `AddHeader` first).
- Reminder: **nothing was pushed or published.**
