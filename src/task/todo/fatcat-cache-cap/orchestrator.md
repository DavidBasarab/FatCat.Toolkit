# fatcat-cache-cap (FatCat.Toolkit) Orchestrator Runbook

**Trigger:** in a Claude session opened in `C:\Code\FatCat.Toolkit` (or its `src\`), the user says "run fatcat-cache-cap" or "run the cache cap plan". If you are that session, follow this runbook exactly; it is the complete instruction set.

## Ground rules (non-negotiable)

- **Fresh context per phase.** Launch each phase as its own general-purpose subagent (Agent tool), wait for it, and verify its result **in this session**. Never do phase work in this session, and never run two phases in one subagent.
- **Wave by wave, in dependency order.** At most 3 concurrent phases; this plan never has more than 2.
- **One commit per phase.** The commit message references the phase file. Never amend, squash, rebase or force-push.
- **Never push.** The one exception is the branch-creation procedure in the user's global `CLAUDE.md`, if it is needed in Preconditions. That procedure pushes an empty start commit and opens the PR. No phase commit is ever pushed.
- **Never edit a `VersionPrefix` and never run `PushNugetPackages.ps1`.** The owner releases after the plan is done.
- **Never edit `C:\Code\Fog`.** Phases read it only.
- **Security gate (ADR-13):** a phase fails the gate only if it introduces or worsens a vulnerability. Findings that were already there are listed in the phase report, not fixed. `code-security-review` is always run screen-only.
- The solution is **`src\ToolKit.slnx`**; run `dotnet` commands from `src\`.
- Plans live under `src\task\` (singular). The template's `tasks\todo` / `tasks\done` mean `src\task\todo` / `src\task\done`.

## Preconditions

1. **Branch.**
   - If on `main`, create `task/fatcat-cache-cap` using the user's global branch procedure.
   - If on any other branch, **ask the human once** whether to run on that branch or on a new `task/fatcat-cache-cap`. At planning time the checkout was `upgrade-net11-slnx-packages`, an unrelated open PR. The template says to use the current branch; the human decides.
2. **Clean tree, plan committed.** `git status --porcelain` must show nothing except this plan's own files (`src/task/todo/fatcat-cache-cap.md` and `src/task/todo/fatcat-cache-cap/`). If those are untracked or modified, commit them alone as `fatcat-cache-cap: add task and phased plan`, because worktrees are created from the branch tip and must contain the plan. Any other dirty file: **stop and ask**. Uncommitted work is the user's.
3. **Green baseline.** `dotnet build ToolKit.slnx` with zero warnings and `dotnet test ToolKit.slnx` green. Record the test count. A pre-existing red makes every later gate meaningless, so stop and report.
4. Record `BASE=$(git rev-parse HEAD)`.

## Waves

| Wave | Phases | Mode |
|---|---|---|
| 1 | 01 `01-cache-foundation.md`, 02 `02-fake-timeout-forwarding.md` | parallel, one worktree each |
| 2 | 03 `03-cap-and-eviction.md`, 04 `04-readme-caching.md` | parallel, one worktree each |
| 3 | 05 `05-final-verification.md` | main working tree, alone |

Every phase except 04 and 05 is **high-risk**. After waves 1 and 2, show the human the high-risk phase diffs and reports and **wait for their go-ahead** before starting the next wave.

## Running a parallel wave

For each phase `NN` in the wave:

1. `git worktree add ..\FatCat.Toolkit-wt\fatcat-cache-cap-NN -b wt/fatcat-cache-cap/NN` (from the git root `C:\Code\FatCat.Toolkit`, at the task-branch tip). Check `git status --ignored` in the main tree for git-excluded files a build needs (for example `global.json` or `*.user`) and copy them in. None are known today.
2. Record `PHASE_BASE_NN=$(git -C <worktree> rev-parse HEAD)`.
3. Launch the subagents for the wave **in one message** so they run concurrently. Use the prompt template below, with the worktree's absolute path.

When all of them finish:

4. Verify each phase (see "Verifying a phase"). A phase that failed verification is a failure; see the halt policy.
5. On the task branch, `git cherry-pick <phase commit>` for each passing phase, **in phase-number order**. Do not edit the commits.
6. **Integration gate** on the task branch: `dotnet build ToolKit.slnx` with zero warnings, then `dotnet test ToolKit.slnx` green. A cherry-pick conflict or a red gate means the plan's parallel claim was wrong. Treat it as a failure; never hand-merge.
7. After a green gate: `git worktree remove ..\FatCat.Toolkit-wt\fatcat-cache-cap-NN` and `git branch -D wt/fatcat-cache-cap/NN` for each phase.

## Running wave 3

Phase 05 runs in the main working tree on the task branch. Record `PHASE_BASE_05` and launch one subagent with the prompt template (path = the main repo). Then verify it.

## Subagent prompt template

> Execute the implementation phase described in `<ROOT>\src\task\todo\fatcat-cache-cap\<PHASE FILE>` in the repository at `<ROOT>` (work only inside that directory). Read that file completely first; it is the entire hand-off. Also read `<ROOT>\src\task\todo\fatcat-cache-cap.md` (the task spec) and `<ROOT>\src\task\todo\fatcat-cache-cap\00-overview.md`; their ADRs are binding. Follow every rule in `<ROOT>\src\CLAUDE.md` and `<ROOT>\src\.claude\rules\`. Do not create or switch branches. Write the tests first and quote the red they produce in your report. The phase's Definition of Done and the task spec's "Definition of Done (every phase)" are mandatory, including the `unit-test-review` → `code-review` → `code-security-review` loop. Finish the whole phase in one run: the commit and the phase report are part of finishing, so do not end your turn on a review's output. You may self-correct at most 2 times. If the Definition of Done still cannot be met, leave the working tree completely clean, do NOT commit, and report `PHASE FAILED` with the reason. On success, create exactly one commit whose message is the one given in the phase file. Never amend, squash or push, never run a NuGet publish script, never edit a `VersionPrefix`, and never write to `C:\Code\Fog`. End with the phase report: files added, changed and deleted (and any outside the phase's `## Touches`), test counts, the deviation log, and open questions or risks.

## Verifying a phase (in this session)

- `git -C <tree> rev-list --count <PHASE_BASE>..HEAD` is exactly `1`, and `git -C <tree> status --porcelain` is empty.
- The commit message references the phase file.
- `git -C <tree> diff --name-only <PHASE_BASE>..HEAD` stays within the phase's `## Touches`. A file outside it is a footprint miss: report it, and for a parallel wave treat it as a failure.
- `dotnet build` with zero warnings and `dotnet test` green in that tree. The test count did not drop, except phase 01's one expected removal (`CanCacheItems`).
- No `VersionPrefix` line in the diff.
- The report contains the quoted red, the deviation log, and no `PHASE FAILED`.

**Phase-specific gates (halt if missing):**

- **01:** the report states the Step 0 signing result. `IFatCatCache<T>` is byte-identical in the diff. `grep -n "DateTime.UtcNow\|DateTime.Now\|timeoutEnabled" src/ToolKit/Caching/FatCatCache.cs` is empty. `AddListTests.ExpireEveryItemAfterTheTimeout` exists and was red first. The `ToolKit.csproj` diff is only the `InternalsVisibleTo` item. Any new `NoWarn` entry has its explaining comment.
- **02:** the existing `Verify*` signatures are unchanged. `FailWhenTheItemWasAddedWithATimeout` exists. The Fog grep result is in the report.
- **03:** `ResolveAnUnboundedCacheWhenScannedLikeSystemScope` passes. `ConcurrencyTests` exists with its ADR-6 comment and was green on three runs. No read path takes the lock. `ToolkitModule.cs` is untouched.
- **05:** two unfiltered green runs are recorded, and `05-walk-report.md` maps every acceptance criterion to evidence.

## Halt-on-failure policy

If a phase reports `PHASE FAILED`, fails verification, conflicts on cherry-pick, or turns the integration gate red:

1. Let the wave's other phases finish. Integrate only those that passed and do not depend on the failed one.
2. Leave the task branch clean (`git stash` or discard; never commit a partial fix).
3. Write `src/task/todo/fatcat-cache-cap/failure-report.md`: the phase, what failed, the commands and their output, and what was integrated. Do not commit it; the human decides.
4. Do not start dependent phases. Stop and report to the human.

## Completion

After phase 05 is committed and its gate is green:

1. Move the plan with `git mv src/task/todo/fatcat-cache-cap src/task/done/fatcat-cache-cap` and `git mv src/task/todo/fatcat-cache-cap.md src/task/done/fatcat-cache-cap.md`. If either destination exists, **stop and ask**.
2. Commit the move alone: `fatcat-cache-cap complete - move plan to tasks/done`.
3. Report to the human:
   - the commits (`git log --oneline <BASE>..HEAD`);
   - test count, baseline versus final;
   - the high-risk notes from each phase report;
   - a pointer to `05-walk-report.md`;
   - the release steps that remain theirs: bump `VersionPrefix` in both `.csproj` files, run `PushNugetPackages.ps1`, and record the version in Fog's epic tracker and `vendor-cost-guards-decisions.md` (F-20).
4. Do not push.
