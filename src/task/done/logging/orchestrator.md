# logging (FatCat.Toolkit) Orchestrator Runbook

**Trigger:** in a Claude session opened in `C:\Code\FatCat.Toolkit`, the user says "run logging"
/ "run the toolkit logging plan" (optionally "starting at phase N"). If you are the session that
was told this, follow this runbook exactly — it is the complete instruction set.

## Ground rules (non-negotiable)

- Execute phases strictly in order: 1 → 2. Never start a phase before the previous one is
  verified complete.
- **Fresh context per phase.** Launch one general-purpose subagent (Agent tool) per phase, wait
  for it to finish, and verify its result before launching the next. Never execute a phase's
  implementation work in this orchestrating session, and never let one subagent touch two
  phases — the phase file is the entire handoff.
- One commit per phase; the commit message references the phase file.
- Never push to any remote, never amend, squash, rebase, or force-push. **Never run
  `PushNugetPackages.ps1` or `Submit-NugetPackage`** — version stepping and publishing are the
  human's post-merge action (overview ADR-5).
- **Branch setup (before phase 1):** the repo is expected on `main` with a clean tree. Create
  and switch to `task/logging` (`git switch -c task/logging`). If the repo is already on
  `task/logging`, continue; if it is on any other branch or the tree is dirty, stop and ask the
  user.
- No external preconditions (no database, no deployed instance). Smoke checks use
  `dotnet run` on the `OneOff` project.

## Phases (order)

| # | Phase file | Depends on |
|---|---|---|
| 1 | `src/task/logging/01-configure-middleware-hook.md` | — |
| 2 | `src/task/logging/02-configure-logging-hook.md` | — (run second) |

## Per-phase procedure

1. Record the current HEAD: `git rev-parse HEAD`.
2. Launch a general-purpose subagent with exactly this prompt (substitute the phase path):

   > Execute the implementation phase described in `<phase path>` in the repository
   > `C:\Code\FatCat.Toolkit`. Read that file completely first — it is the entire handoff
   > document; you have no other context. Follow every rule in `src/CLAUDE.md` and
   > `src/.claude/rules` (ignore the stale "Haivision" header text — the rule files are the
   > operative standards). The phase file's Definition of Done, including its review
   > requirements, is mandatory. You may self-correct at most 2 times; if the Definition of
   > Done still cannot be met after that, leave the working tree completely clean (discard or
   > stash your changes), do NOT commit, and report PHASE FAILED with an explanation. On
   > success create exactly one commit on branch `task/logging` whose message references
   > `<phase path>`. Never amend or squash existing commits, never push to any remote, and
   > never run any NuGet publish script.

3. When the subagent finishes, verify in this session:
   - `git rev-list --count <recorded HEAD>..HEAD` is exactly `1`
   - `git status --porcelain` is empty
   - `git branch --show-current` is `task/logging`
   - The new commit's message references the phase file
   - The subagent did not report PHASE FAILED
4. All checks pass → tell the user the phase is done (one line: commit hash + subject) and move
   to the next phase.
5. Any check fails → follow **Halt on failure** below. Do not continue.

## Halt on failure

If a phase reports PHASE FAILED, or its verification checks fail:

1. If the working tree is dirty:
   `git stash push --include-untracked -m "toolkit logging phase <n> failure"`.
2. Write `src/task/logging/failure-report.md` containing: the phase number and file, what the
   subagent reported, which verification check failed, the `git status` output from before the
   stash, and the stash reference if one was created.
3. Stop the pipeline. Report the failure to the user and point them at the failure report. They
   can resume with "run logging starting at phase N" after fixing the cause.

Special case — more than one new commit: do not revert anything on your own; halt, report, and
let the human decide.

## Completion report

After phase 2 verifies, report to the user:

- The two phase commits (hash + subject) on `task/logging`.
- The smoke results from both phases (per-request hook line including the 404 case; startup
  hook line; unchanged behavior with hooks unset; full suite green).
- The combined deviation log from both phase reports (especially the ADR-4 test-seam outcomes).
- **The human's next steps, in order:** review and merge `task/logging` → `main`; run
  `src/PushNugetPackages.ps1` (it steps both projects' versions and publishes — expected next
  version 1.0.342); then have the Apostil `api_logging` plan updated to consume the new package
  (drop the `IStartupFilter` design in its phase 2; bridge framework logs in its phase 1).
- Reminder: nothing was pushed and nothing was published; both are the human's call.
