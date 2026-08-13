# mongo-count-and-distinct Orchestrator Runbook

**Trigger:** the user says "run mongo-count-and-distinct". If you are the Claude session that was told
this, follow this runbook exactly — it is the complete instruction set.

## Ground rules (non-negotiable)

- **One phase.** `01-count-and-distinct.md`. There is no dependency order to honour and nothing to
  parallelise.
- **Fresh context for the phase.** Launch **one general-purpose subagent (Agent tool)** with the phase
  file as its entire handoff. Do not execute the implementation work in this orchestrating session.
- One commit; the commit message references the phase file. **Never squash, never amend.**
- **Never push, never publish, never step the package version.** Publishing is the human's, after review
  (`00-overview.md`'s publish flow).

## Preconditions (check before launching; tell the subagent what you found)

- `git rev-parse --abbrev-ref HEAD`. On `main` → the phase creates and switches to
  `mongo-count-and-distinct`. On any other branch → use it.
- From `src/`: `dotnet build ToolKit.slnx` clean and `dotnet test ToolKit.slnx` green. **Record the real
  numbers**; a pre-existing red makes the verification below meaningless.
- Working tree clean. **Commit the plan files on their own first** so the phase's diff is readable.
- The referenced MongoDB driver exposes `CountDocumentsAsync`, `DistinctAsync`, and
  `ExpressionFieldDefinition<T, TValue>`. **If it does not, halt** — a package upgrade is a decision for
  the human, not a phase.

## Procedure

1. Record `git rev-parse HEAD`.
2. Launch a general-purpose subagent with this prompt:

   > Execute the implementation phase described in
   > `task/todo/mongo-count-and-distinct/01-count-and-distinct.md`. Read that file completely first — it
   > is the entire handoff document; you have no other context. Read
   > `task/todo/mongo-count-and-distinct/00-overview.md` as well; its ADRs are binding. You are working
   > on branch `<branch>`; do not create or switch branches beyond what the preconditions state. Follow
   > every rule in CLAUDE.md and .claude/rules. The phase file's Definition of Done is mandatory. You may
   > self-correct at most 2 times; if it still cannot be met, leave the working tree completely clean
   > (discard or stash), do NOT commit, and report PHASE FAILED with an explanation. On success create
   > exactly one commit whose message references the phase file. Never amend or squash, and never push.

3. When it finishes, verify **in this session**:
   - `git rev-list --count <recorded HEAD>..HEAD` is exactly `1`
   - `git status --porcelain` is empty
   - `dotnet build ToolKit.slnx` clean at the baseline warning count; `dotnet test ToolKit.slnx` green
   - the subagent did not report PHASE FAILED

### Extra gates (this phase earns them)

- **Red was observed before green**, and the report says what red looked like.
- **The null fact exists and passes** — a matching document with no value for the field yields a `null`
  entry from `DistinctByFilter`. **If it is missing, halt**: ADR-4 is the whole reason the consumer can
  detect the state it exists to detect, and a `Where(v => v is not null)` is the tidy-looking mistake
  this gate is here to catch.
- **Distinct with no matches returns an empty list, not null** — asserted on both the real
  implementation and the fake.
- **`MongoFakeRepository<T>` gained the four members of ADR-5**, and `Collection` is still `null`.
- **`IDataRepository<T>` and `FileSystemRepository<T>` are not in the diff.**
- **No existing member's body or signature changed** — the report states this from a reviewed `git diff`.
- **No package reference, no version step, nothing published.**
- **The `grep` for implementations of `IMongoRepository<T>`** was run and its output recorded.

If a report cannot substantiate a gate, treat it as a failed verification and halt.

## Halt on failure

1. If the tree is dirty: `git stash push --include-untracked -m "mongo-count-and-distinct failure"`.
2. Write `task/todo/mongo-count-and-distinct/failure-report.md`: what the subagent reported, which check
   failed, the `git status` from before the stash, and the stash reference.
3. Stop and report to the user. **Tell them explicitly that `C:\Code\Apostil`'s
   `embedding_model_consistency` work item stays blocked** until this lands.

**Special case — the subagent filters nulls out of the distinct result.** Halt (ADR-4).

**Special case — the subagent adds a paged query, a delete-by-filter, an aggregation passthrough, or
index management.** Out of scope by `00-overview.md`; halt.

**Special case — the subagent puts the methods on `IDataRepository<T>`** and starts writing an in-memory
count for `FileSystemRepository<T>`. Halt (ADR-1).

**Special case — the subagent rewrites `MongoFakeRepository<T>` into a real in-memory store.** That is a
much larger change to a class every consumer's suite depends on. Halt (ADR-5).

**Special case — a package upgrade or a version step.** Halt; both are the human's.

## Completion report

- The commit (hash + subject) and the branch it is on.
- Baseline and final test counts.
- The `DistinctByFilter` doc comment, quoted — the human's chance to object to the words about nulls.
- The deviation log from the phase report.
- **The publish flow from `00-overview.md`, repeated as the next human action**, including that
  `C:\Code\Apostil` must be bumped to 1.0.346 before `run embedding_model_consistency` will do anything
  at all.
- Reminder: **nothing was pushed and nothing was published.**
