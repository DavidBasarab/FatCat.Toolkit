# mongo-batch-writes (FatCat.Toolkit) Orchestrator Runbook

**Trigger:** in a Claude session opened in `C:\Code\FatCat.Toolkit`, the user says
"run mongo-batch-writes" / "run the toolkit batch writes plan". If you are the session that was told
this, follow this runbook exactly — it is the complete instruction set.

## Ground rules (non-negotiable)

- **One phase.** There is no ordering problem to manage here; the value of this runbook is the
  verification, not the sequencing.
- **Fresh context for the phase.** Launch one general-purpose subagent (Agent tool), wait for it,
  and verify its result in this session. Do not execute the implementation work here.
- One commit; the message references the phase file.
- Never push, amend, squash, rebase, or force-push. **Never run `PushNugetPackages.ps1` or
  `Submit-NugetPackage`, and never edit a `VersionPrefix`** — version stepping and publishing are the
  human's post-merge action.
- **Branch setup:** create and switch to `mongo-batch-writes` from a clean tree if it does not exist.
  If the tree is dirty, stop and ask the user — uncommitted work is theirs, not yours. Never work on
  `main`.
- No external preconditions: no database, no deployed instance, no smoke host. Every check is
  `dotnet build` / `dotnet test`.
- The solution is **`ToolKit.slnx`**, built from `src/`. A phase report claiming it built `ToolKit.sln`
  did not build anything — treat that as a failed verification.

## Preconditions (check before launching; tell the subagent what you found)

- `dotnet build ToolKit.slnx` clean and `dotnet test ToolKit.slnx` green **before** any change.
  Record the real test count — a pre-existing red makes every check below meaningless.
- The working tree is clean and the branch is `mongo-batch-writes`.
- `MongoDB.Driver` is pinned at **3.10.0** in `src/ToolKit/ToolKit.csproj`. If it is not, say so in the
  prompt — the phase file's target shape was written against 3.10.0.

## The phase

| # | Phase file | Risk |
|---|---|---|
| 1 | `src/task/todo/mongo-batch-writes/01-batch-writes.md` | **Medium** — changes the wire behaviour of the most-used data class in a published package, with no integration test anywhere in this repo |

Supporting documents the subagent does **not** need but you may cite when reporting: `00-overview.md`
(evidence, ADRs) and `consumer-compatibility.md` (the `C:\Code\Apostil` and `C:\Code\Fog`
verification).

## Procedure

1. Record the current HEAD: `git rev-parse HEAD`.
2. Launch a general-purpose subagent with exactly this prompt:

   > Execute the implementation phase described in
   > `src/task/todo/mongo-batch-writes/01-batch-writes.md` in the repository `C:\Code\FatCat.Toolkit`.
   > Read that file completely first — it is the entire handoff document. Read
   > `src/task/todo/mongo-batch-writes/00-overview.md` as well; its ADRs are binding on your
   > implementation, and **ADR-4 (the empty-list guard) is the one that decides whether this change is
   > safe**. Follow every rule in `src/CLAUDE.md` and `src/.claude/rules` (ignore the stale
   > "Haivision" header text — the rule files are the operative standards). You are on branch
   > `mongo-batch-writes`; do not create or switch branches. The phase file's Definition of Done is
   > mandatory. **Rewrite the three existing per-item facts BEFORE touching production code and quote
   > the red they produce** — that red is the point of this phase. Complete the whole phase in one run:
   > the commit and the phase report are part of finishing, so do not end your turn on a review's
   > output. You may self-correct at most 2 times; if the Definition of Done still cannot be met after
   > that, leave the working tree completely clean, do NOT commit, and report PHASE FAILED. On success
   > create exactly one commit whose message references the phase file. Never amend or squash, never
   > push, never run a NuGet publish script, and never edit a `VersionPrefix`.

3. When the subagent finishes, verify **in this session**:
   - `git rev-list --count <recorded HEAD>..HEAD` is exactly `1`
   - `git status --porcelain` is empty
   - the commit message references the phase file
   - `git diff --name-only <recorded HEAD>..HEAD` contains **exactly one** production file,
     `src/ToolKit/Data/Mongo/MongoRepository.cs`, plus test files under
     `src/Tests.ToolKit/Data/Mongo/DataRepositorySpecs/` — **and nothing else**
   - `dotnet build ToolKit.slnx` clean, `dotnet test ToolKit.slnx` green, test count **up by 3**
     (the empty-list facts) with no fact removed beyond the three rewritten
   - the subagent did not report PHASE FAILED

## Extra gates (read the report slowly — this is a published package)

- **The empty-list facts exist, one per method, and assert `A.CallTo(collection).MustNotHaveHappened()`**
  — not merely that nothing threw. Without these, ADR-4 is unproven and the change is a latent runtime
  break for every consumer. **Missing or weakened ⇒ halt.**
- **Each rewritten fact has BOTH halves** — the positive single-command assertion *and* the
  `MustNotHaveHappened` negative on the per-item call. The negative is the half that proves the loop is
  gone; a report with only the positive has not shown the change took effect.
- **The red was quoted**, from the rewritten facts failing against the `foreach` implementation. A
  report that went straight to green rewrote the tests after the code and should be treated as
  unverified.
- **The report states honestly whether the empty-list facts passed vacuously before the production
  change.** They will have — that is expected, and a report claiming otherwise is wrong about its own
  run.
- **`grep -n "IsOrdered\|InsertManyOptions\|BulkWriteOptions"` returns nothing** (ADR-2). An options
  object means failure semantics changed silently for every consumer.
- **`git diff` shows no change to** `DataRepository.cs`, `MongoFakeRepository.cs`,
  `FileSystemRepository.cs`, or any `.csproj`. Any of those ⇒ halt: the whole safety argument for this
  change is that it is implementation-only.
- **The report says plainly that nothing in this repository proves the batched path works against a
  real MongoDB** (ADR-5). A report claiming this is verified end to end is overstating it — the
  evidence comes from the consumer re-measure, after publish.

## Halt on failure

If the subagent reports PHASE FAILED, or a verification check fails:

1. If the working tree is dirty: `git stash push --include-untracked -m "mongo-batch-writes failure"`.
2. Write `src/task/todo/mongo-batch-writes/failure-report.md` with what was reported, which check
   failed, the `git status` from before the stash, and the stash reference.
3. Stop. Report to the user and point them at the failure report.

**Special case — an interface changed.** If `IDataRepository<T>` gains a member, or
`FileSystemRepository<T>` / `MongoFakeRepository<T>` is edited, halt regardless of a green suite. This
work item is implementation-only by design; a paged query or delete-by-filter is a *different* work
item with a much larger consumer-impact surface (`00-overview.md`, Scope).

**Special case — a package was added or upgraded.** Including the MongoDB driver. Halt.

**Special case — a version was stepped or a publish script was run.** Halt; that is the human's, and a
double-step corrupts the release history.

**Special case — an integration test against a real MongoDB appears.** ADR-5 declined it deliberately:
it is a larger piece of work than the change it would guard, and whether this repository takes on that
infrastructure is the human's call. Halt and hand it over.

## Completion report

After the phase verifies, report to the user:

- The commit (hash + subject) and the branch.
- The three methods' before/after shape, and confirmation that **no interface changed**.
- The red, quoted.
- The empty-list evidence (ADR-4) — the single most important gate in this plan.
- Test count before and after.
- **The publish flow, restated, because the work is not finished at the commit:** merge → run
  `PushNugetPackages.ps1` (steps to **1.0.345**) → **bump `C:\Code\Apostil`'s `Apostil.Api.csproj` to
  1.0.345 and re-run the cap-sized upload measurement, ideally against a remote database rather than
  localhost.** That number is the only end-to-end evidence this change will ever have, and it is also
  what decides whether Apostil's epic 08 (background ingestion) still needs pulling forward — before
  this change that looked urgent; if the chunk stage drops from 22 s to under a second, it should be
  judged on its own merits instead.
- `C:\Code\Fog` needs nothing, but its 339 → 345 upgrade is its own task.
- Reminder: **nothing was pushed and nothing was published.**
