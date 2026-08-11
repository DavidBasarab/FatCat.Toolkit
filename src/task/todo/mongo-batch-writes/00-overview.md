# mongo-batch-writes (FatCat.Toolkit) — Overview

> **Origin:** raised by a consumer. Written from `C:\Code\Apostil` after completing that repo's
> `chunking` work item (now `tasks/done/chunking/`), whose ADR-10 recorded this gap and deliberately
> **did not close it** — a toolkit change mid-work-item is forbidden there, so the number was measured
> and handed over instead.
>
> **Status:** planned (2026-08-11). One phase; runbook in `orchestrator.md`; consumer verification in
> `consumer-compatibility.md`.

## Work Item

Make `MongoRepository<T>`'s three **list** overloads issue **one batched command** instead of a
`foreach` of single-document commands.

That is the entire change. **No interface changes, no new members, no signature changes** — the
methods already exist on `IDataRepository<T>` and every consumer already compiles against them. Only
three method bodies in one file change.

```csharp
// src/ToolKit/Data/Mongo/MongoRepository.cs:44 — today
public async Task<List<T>> Create(List<T> items)
{
    foreach (var item in items)
    {
        await Create(item);          // InsertOneAsync, awaited one at a time
    }

    return items;
}
```

`Delete(List<T>)` (line 63) and `Update(List<T>)` (line 136) have the identical shape with
`DeleteOneAsync` and `ReplaceOneAsync`.

## The evidence — measured, not predicted

From Apostil's `chunking` phase 6, on a live api against **localhost** MongoDB:

| | Measured |
|---|---|
| Document | a 10.2 MB `.txt` at the upload cap |
| Chunks written | **19,209**, via one `Create(List<ChunkData>)` |
| Time in the chunk stage | **22,172 ms** |
| Time actually spent chunking (pure CPU) | **~100–160 ms — under 1%** |
| Per-insert cost | **~1.15 ms**, i.e. one network round trip |
| The same upload before chunking existed | **5–7 ms** |

**The cost is latency, not work.** A chunk's size is irrelevant — a 200-character document costs the
same round trip as a 1,200-character one. That is why this matters more than the 22-second figure
suggests: the per-item cost scales with round-trip time to the database, and the consumer's
production database is **MongoDB Atlas** (its D16), not localhost.

| Database | RTT/op | 19,209 ops | Outcome |
|---|---:|---:|---|
| Localhost (what was measured) | 1.15 ms | 22 s | slow, completes |
| Atlas, co-located container | ~2–3 ms | 38–58 s | ugly, completes |
| **Atlas from a developer laptop** | ~20–50 ms | **6–16 min** | **exceeds the consumer's 100 s client timeout — the request dies** |

Batching converts a **latency-bound** operation into a **bandwidth-bound** one: the driver auto-batches
at 100,000 documents / 48 MB, so 19,209 documents (~15 MB with BSON overhead) becomes roughly a single
round trip plus the irreducible cost of sending the bytes. The remote column stops being a cliff.

## Scope — and what is deliberately left out

**In scope:** `Create(List<T>)`, `Delete(List<T>)`, `Update(List<T>)` on `MongoRepository<T>`.

All three are the same defect. Fixing one and leaving two guarantees the next consumer rediscovers it —
and the consumer that raised this will hit `Delete(List<T>)` directly, because its chunk store deletes
before it creates, so a re-ingest pays the cost twice.

**Explicitly out of scope — a separate work item, if it is ever wanted:**

- **A paged/filtered query** (`skip`/`limit` server-side) and a **delete-by-filter**. Both are
  genuinely useful and both are *interface additions* — they would touch `IDataRepository<T>`,
  `FileSystemRepository<T>`, `MongoFakeRepository<T>`, and every consumer's own fakes. That is a
  categorically larger and riskier change than this one, it is not what the 22 seconds is, and nothing
  today is blocked on it. Apostil's ADR-10 names it as a *possible* future need for its retrieval
  epics.
- **Version stepping and publishing.** Same doctrine as `rate-limiting-hook` ADR-6 — see below.

## Acceptance Criteria → Phase Map

| Acceptance criterion | Proven by |
|---|---|
| `Create(List<T>)` issues exactly one `InsertManyAsync` and no `InsertOneAsync` | Phase 1 |
| `Delete(List<T>)` issues exactly one `DeleteManyAsync` and no `DeleteOneAsync` | Phase 1 |
| `Update(List<T>)` issues exactly one `BulkWriteAsync` and no `ReplaceOneAsync` | Phase 1 |
| **An empty list is a no-op that touches the collection not at all and does not throw** | Phase 1 (see ADR-4 — this is the one real behavioural trap) |
| Each still returns the list it was given | Phase 1 (the existing `ReturnListOf…` facts, unchanged) |
| `EnsureCollection` still runs before any non-empty batch | Phase 1 (the existing `EnsureCollectionTests` base) |
| No interface, signature, or public surface change anywhere | Phase 1 (`git diff` covers one production file) |
| `MongoFakeRepository<T>` needs no change, and consumer unit tests cannot observe this | `consumer-compatibility.md` (the fake wraps `A.Fake<IMongoRepository<T>>()`, never a real repository) |
| No breaking change for `C:\Code\Fog` or `C:\Code\Apostil` | `consumer-compatibility.md` (both verified) |
| `dotnet build ToolKit.slnx` / `dotnet test ToolKit.slnx` clean | Phase 1's Definition of Done |
| Package publishable by the human afterwards | Out of plan scope by design (ADR-6 of `rate-limiting-hook`, followed here) |

## Phases

| Phase | File | Risk | Depends on |
|---|---|---|---|
| 1 — Batch the three list overloads | `01-batch-writes.md` | **Medium** — it changes the wire behaviour of the most-used data class in the toolkit, in a published package, and there is no integration test against a real MongoDB anywhere in this repository. Mitigated: no API change, a real existing unit seam that already pins the current behaviour, and the empty-list guard of ADR-4. | — |

One phase, because the three methods are one change with one rationale and one risk. Splitting them
would produce three commits that each half-fix a class.

## Current state that shapes the design (verified against source)

- `MongoRepository<T>` (`src/ToolKit/Data/Mongo/MongoRepository.cs`) holds all three `foreach`
  implementations. `Create(List)` is line 44, `Delete(List)` line 63, `Update(List)` line 136.
- **The list overloads are already on the interface** — `IDataRepository<T>`
  (`src/ToolKit/Data/DataRepository.cs:11,15,29`). Nothing is being added.
- **There is a real test seam and it already pins the current behaviour.**
  `Tests.ToolKit/Data/Mongo/DataRepositorySpecs/` fakes `IMongoCollection<T>` with FakeItEasy
  (`DataRepositoryTests` base) and asserts per-item calls:
  `CreateItemListTests.CallInsertOneForEachItemInList` and the `DeleteListTests` equivalent. **Those
  facts must be rewritten by this phase** — they are the specification of the behaviour being replaced,
  which is exactly why they exist and why red-before-green is available here.
- `MongoRepository<T>` also exposes the raw `Collection`, so the fakes can assert the driver calls
  directly. No new seam is needed.
- **`MongoFakeRepository<T>`'s inner `repository` field is `A.Fake<IMongoRepository<T>>()`**
  (`src/ToolKit/Testing/MongoFakeRepository.cs:49`) — never a real `MongoRepository`. **No consumer
  unit test can observe this change**, which is what makes the consumer risk so low and also means
  consumer suites prove nothing about it.
- `FileSystemRepository<T>` has its own list implementations and is **not** touched.
- Solution is **`ToolKit.slnx`**; build/test from `src/`.
- Current source version is **1.0.344**; this work would publish as **1.0.345**.

## Decisions (lightweight ADRs)

### ADR-1 — Batch the existing methods rather than adding new ones
**Decision:** change `Create(List<T>)`, `Delete(List<T>)` and `Update(List<T>)` in place. Do not add
`CreateMany`/`InsertMany`-named members.
**Context:** the `foreach` is a defect, not a policy. A new method would leave every existing caller
on the slow path until each one is found and edited, which inverts the safe default and means the
consumer that reported this has to change code to get a fix for something it did not do wrong. The
signatures already exist and already promise "write these items"; nothing about them promised *one at
a time*.
**Alternatives rejected:** additive `…Many` methods (interface change, slow default, two ways to do
one thing); a `bool batched = true` optional parameter (a flag that no caller would ever pass `false`).

### ADR-2 — `IsOrdered` stays at the driver default (`true`)
**Decision:** call `InsertManyAsync(items)` / `BulkWriteAsync(models)` with no options object.
**Context:** ordered is the **closest match to today's behaviour**. A sequential `foreach` of awaited
single-document calls stops at the first failure with everything before it already written; an
*ordered* batch does exactly the same. Passing `IsOrdered = false` would be faster still and would
silently change failure semantics for every consumer — a different work item with its own argument.
**Alternatives rejected:** unordered for throughput (changes partial-failure behaviour invisibly);
exposing an options parameter (public surface for a question nobody has asked).

### ADR-3 — `Update(List<T>)` uses `BulkWriteAsync` with `ReplaceOneModel`
**Decision:** build one `ReplaceOneModel<T>` per item, filtered on `Id`, and issue a single
`BulkWriteAsync`.
**Context:** there is no `ReplaceManyAsync` — replacing *N* different documents with *N* different
bodies is what bulk write is for. The per-item filter is the same `i => i.Id == item.Id` the current
`Update(T)` uses, so the semantics carry over exactly.
**Note:** no consumer uses this overload today (verified — see `consumer-compatibility.md`), which
makes it the lowest-risk of the three to get wrong *and* the one with no consumer smoke test behind
it. It is included because leaving one of three identical defects in place is how it gets rediscovered.
**Alternatives rejected:** leaving `Update(List<T>)` alone (the same bug, waiting); a loop of
`ReplaceOneAsync` with `Task.WhenAll` (concurrent round trips still pay latency *N* times and add a
connection-pool failure mode).

### ADR-4 — An empty list must remain a no-op, and this is the one real trap
**Decision:** each method returns immediately when `items` is empty, **without calling
`EnsureCollection()` and without touching the driver at all.**
**Context:** today `Create([])` runs a `foreach` over nothing — it is a silent, successful no-op that
never reaches Mongo. **The MongoDB driver throws on an empty `InsertManyAsync` and on a
`BulkWriteAsync` with no requests** (`Must contain at least 1 request`). Without a guard, this change
converts a successful no-op into a thrown exception for every consumer that ever passes an empty list
— a genuine breaking change hiding inside a performance fix, and the single most likely way for this
work item to cause an incident.
**This is not hypothetical.** Apostil's `DocumentChunkStore.Replace` is documented to accept
`Replace(id, [])` as a legal call meaning "clear this document's passages"; it guards its own call
sites today, but the toolkit must not depend on every caller doing so.
**Consequence:** the phase ships an explicit empty-list fact per method asserting the collection was
**never touched** — `A.CallTo(collection).MustNotHaveHappened()` — not merely that nothing threw.

### ADR-5 — Verification is the existing unit seam plus a consumer re-measure
**Decision:** prove the driver calls with the existing FakeItEasy `IMongoCollection<T>` seam. Do not
add a Testcontainers or live-MongoDB integration test in this repository.
**Context:** this repository has **no integration test against a real MongoDB** for any repository
method, and introducing that infrastructure is a far larger piece of work than the change it would be
guarding. The unit seam proves *which driver call is issued with which arguments*, which is the whole
of the change. That the batched call is **faster** is proven where it was measured — in the consumer,
by re-running the cap-sized upload after the version bump.
**Consequence, stated plainly:** nothing in this repository proves the batched path works against a
real MongoDB. **The consumer re-measure is not optional polish — it is the only end-to-end evidence
this change will ever have**, and it belongs in the publish flow below rather than in a phase.

## Consumer compatibility

`consumer-compatibility.md` records the verification. Summary:

| Repo | Version | Uses a list overload? | Verdict |
|---|---|---|---|
| `C:\Code\Apostil` | 1.0.344 | **Yes** — `DocumentChunkStore.Replace` calls both `Create(List)` and `Delete(List)` | Safe, and the sole beneficiary. It is also the only place the fix can be measured. |
| `C:\Code\Fog` | 1.0.339 | **No** — every `Create`/`Delete`/`Update` call found is the single-item overload | Unaffected either way. Its 339 → 345 gap deserves its own build-and-smoke pass, as its own task. |

**No consumer compiles differently, and no consumer unit test can observe the change**, because
`MongoFakeRepository<T>` fakes the interface rather than wrapping a real repository.

## Publish flow (human-owned, after the phase completes)

1. Review the single commit on `mongo-batch-writes`; merge to `main` (your call how — the plan never
   pushes or merges).
2. From `src/`, run `PushNugetPackages.ps1` — `Submit-NugetPackage` steps the version (next: 1.0.345),
   commits the step, and pushes.
3. **Bump `Apostil.Api.csproj` to 1.0.345 and re-run the cap-sized upload measurement — ideally against
   a remote database, not localhost.** That number is the only real evidence this worked, and it is
   also the number that decides whether Apostil's epic 08 (background ingestion) still needs pulling
   forward. Before this change the answer looked like "yes, urgently"; if the chunk stage drops from
   22 s to under a second, epic 08 should be judged on its own merits instead.
4. `Fog` needs nothing.

## Assumptions

- The rules in `src/.claude/rules/csharp/*` govern this repo's C# (TDD, xUnit + FakeItEasy +
  FatCat.Testing with `.Not.` negation, CSharpier owns formatting, warnings are errors, block bodies
  only, no expression-bodied members anywhere including tests).
- Build/test entry points from `src/`: `dotnet build ToolKit.slnx` and `dotnet test ToolKit.slnx`.
- The task branch is `mongo-batch-writes`. The commit policy forbids working on `main`.
- The MongoDB C# driver version already referenced by `ToolKit.csproj` exposes `InsertManyAsync`,
  `DeleteManyAsync`, `BulkWriteAsync`, `ReplaceOneModel<T>` and `Builders<T>.Filter.In`. **No new
  package reference.** If any of those is unavailable, that is a halt and a question for the human, not
  a licence to add or upgrade a package.

## Open Questions

None blocking. Flagged for the human reviewer:

- **The empty-list behaviour is the one thing to read slowly** (ADR-4). It is the only way this change
  can break a consumer, and it breaks it at runtime rather than at compile time.
- **`Delete(List<T>)` matches on `Id` via `Builders<T>.Filter.In`.** Today's per-item
  `DeleteOneAsync(i => i.Id == item.Id)` deletes at most one document per item; an `In` filter deletes
  every document whose id is in the set, which is the same thing given ids are unique. Worth one
  reviewer's glance rather than an assumption.
- **Nothing here measures anything.** See ADR-5 — the evidence lives in the consumer, and step 3 of the
  publish flow is where it gets taken.
