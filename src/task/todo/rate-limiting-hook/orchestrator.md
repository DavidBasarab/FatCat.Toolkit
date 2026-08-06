# rate-limiting-hook (FatCat.Toolkit) Orchestrator Runbook

**Trigger:** in a Claude session opened in `C:\Code\FatCat.Toolkit`, the user says
"run rate-limiting-hook" / "run the toolkit rate limiting plan" (optionally "starting at phase N").
If you are the session that was told this, follow this runbook exactly — it is the complete
instruction set.

## Ground rules (non-negotiable)

- Execute phases strictly in order: 1 → 2 → 3. Never start a phase before the previous one is
  verified complete. (Phase 3 is technically independent, but the order is fixed so the two
  consumer-blocking hooks land first.)
- **Fresh context per phase.** Launch one general-purpose subagent (Agent tool) per phase, wait for
  it to finish, and verify its result before launching the next. Never execute a phase's
  implementation work in this orchestrating session, and never let one subagent touch two phases —
  the phase file is the entire handoff.
- One commit per phase; the commit message references the phase file.
- Never push to any remote, never amend, squash, rebase, or force-push. **Never run
  `PushNugetPackages.ps1` or `Submit-NugetPackage`** — version stepping and publishing are the
  human's post-merge action (overview ADR-6).
- **Branch setup (before phase 1):** the repo is expected to be on `rate-limiting-hook` with a clean
  tree. If it is on `rate-limiting-hook` but dirty — at the time of planning there was an
  uncommitted move of `src/task/logging` and `src/task/signalr-hub-improvements` into
  `src/task/done/` — stop and ask the user to commit or stash that first; it is theirs, not yours.
  If the repo is on any other branch, stop and ask.
- No external preconditions (no database, no deployed instance). Smoke checks use `dotnet run` on
  the `OneOff` project (fallback `SampleDocker`).
- The solution is `ToolKit.slnx`. Any phase that reports building `ToolKit.sln` did not build
  anything — treat that as a failed verification.

## Phases (order)

| # | Phase file | Depends on |
|---|---|---|
| 1 | `src/task/todo/rate-limiting-hook/01-configure-services-hook.md` | — |
| 2 | `src/task/todo/rate-limiting-hook/02-configure-routed-middleware-hook.md` | 1 |
| 3 | `src/task/todo/rate-limiting-hook/03-redact-request-url-logging.md` | — (run third) |

Supporting documents the subagents do **not** need but you may cite when reporting:
`00-overview.md` (ADRs, phase map) and `consumer-compatibility.md` (the `C:\Code\Fog` and
`C:\Code\Apostil` verification).

## Per-phase procedure

1. Record the current HEAD: `git rev-parse HEAD`.
2. Launch a general-purpose subagent with exactly this prompt (substitute the phase path):

   > Execute the implementation phase described in `<phase path>` in the repository
   > `C:\Code\FatCat.Toolkit`. Read that file completely first — it is the entire handoff document;
   > you have no other context. Follow every rule in `src/CLAUDE.md` and `src/.claude/rules`
   > (ignore the stale "Haivision" header text — the rule files are the operative standards). The
   > phase file's Definition of Done, including its review requirements and its smoke checks, is
   > mandatory; never report a smoke result you did not observe. You may self-correct at most 2
   > times; if the Definition of Done still cannot be met after that, leave the working tree
   > completely clean (discard or stash your changes), do NOT commit, and report PHASE FAILED with
   > an explanation. On success create exactly one commit on branch `rate-limiting-hook` whose
   > message references `<phase path>`. Never amend or squash existing commits, never push to any
   > remote, and never run any NuGet publish script.

3. When the subagent finishes, verify in this session:
   - `git rev-list --count <recorded HEAD>..HEAD` is exactly `1`
   - `git status --porcelain` is empty
   - `git branch --show-current` is `rate-limiting-hook`
   - The new commit's message references the phase file
   - The subagent did not report PHASE FAILED
   - The subagent's report contains actual smoke output (for phase 2, the per-call status codes
     including the `429`), not a claim that the checks "would" pass
4. All checks pass → tell the user the phase is done (one line: commit hash + subject) and move to
   the next phase.
5. Any check fails → follow **Halt on failure** below. Do not continue.

**Extra check after phase 2 only:** confirm from the diff that the existing
`ToolkitWebApplication.Settings.ConfigureMiddleware?.Invoke(app)` line in
`src/Toolkit.WebServer/ApplicationStartUp.cs` is still in its original position (immediately after
`app.Use(CaptureMiddlewareExceptions)`, before `app.UseFileServer()`). Moving it is a breaking change
for every consumer on 1.0.342+ — concretely, Apostil's `RequestLoggingMiddleware` relies on that
position — and is explicitly forbidden by overview ADR-1. If it moved, halt.

## Halt on failure

If a phase reports PHASE FAILED, or its verification checks fail:

1. If the working tree is dirty:
   `git stash push --include-untracked -m "rate-limiting-hook phase <n> failure"`.
2. Write `src/task/todo/rate-limiting-hook/failure-report.md` containing: the phase number and file,
   what the subagent reported, which verification check failed, the `git status` output from before
   the stash, and the stash reference if one was created.
3. Stop the pipeline. Report the failure to the user and point them at the failure report. They can
   resume with "run rate-limiting-hook starting at phase N" after fixing the cause.

Special case — more than one new commit: do not revert anything on your own; halt, report, and let
the human decide.

## Completion report

After phase 3 verifies, report to the user:

- The three phase commits (hash + subject) on `rate-limiting-hook`.
- The smoke results from each phase, quoted from the phase reports — especially phase 2's status-code
  sequence (`200`, `200`, `200`, **`429`**, then `200` after the window resets) and phase 3's
  before/after log line.
- The combined deviation log from all three phase reports (test placement choices, whether `OneOff`
  or `SampleDocker` was the smoke host, whether a throwing sample endpoint was committed).
- Anything the phases observed about the `Run`-level `UseCors`/`UsePathBase`/`MapControllers`
  ordering (overview Open Questions) — observation only; nothing should have changed there.
- **The human's next steps, in order:**
  1. Review and merge `rate-limiting-hook` → `main` (your call how — the plan never pushes or
     merges).
  2. Move `src/task/todo/rate-limiting-hook/` to `src/task/done/rate-limiting-hook/`, matching what
     was done for `logging` and `signalr-hub-improvements`.
  3. From `src/`, run `PushNugetPackages.ps1` — it steps both projects' versions (expected next:
     1.0.344) and publishes.
  4. Then update `Fog`: bump `FatCat.Toolkit` / `FatCat.Toolkit.WebServer` to 1.0.344 in the four
     non-spike `.csproj` files (a three-release jump — give it its own build-and-smoke pass) and
     unblock `C:\Code\Fog\tasks\todo\email_opt_in\07-passphrase-endpoint-hardening.md`. See
     `consumer-compatibility.md` for exactly what Fog does. `Apostil` needs nothing beyond an
     optional version bump.
- Reminder: nothing was pushed and nothing was published; both are the human's call.
