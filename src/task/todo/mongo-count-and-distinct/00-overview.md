# mongo-count-and-distinct (FatCat.Toolkit) — Overview

> **Origin:** raised by a consumer. Written from `C:\Code\Apostil` while planning that repo's
> `embedding_model_consistency` work item (`tasks/todo/embedding_model_consistency/`), whose ADR-2
> records that the work item **cannot be built without this change** and must not start until it has
> shipped.
>
> **Status:** planned (2026-08-12). One phase; runbook in `orchestrator.md`; consumer verification in
> `consumer-compatibility.md`.

## Work Item

Add **two server-side query methods** to `IMongoRepository<T>` — a count and a distinct — and give
`MongoFakeRepository<T>` the canned-value support consumers need to drive them in unit tests.

```csharp
// src/ToolKit/Data/Mongo/MongoRepository.cs — the two new members
Task<long> CountByFilter(Expression<Func<T, bool>> filter);

Task<List<TValue>> DistinctByFilter<TValue>(Expression<Func<T, TValue>> field, Expression<Func<T, bool>> filter);
```

That is the whole change: two members on `IMongoRepository<T>`, their two implementations on
`MongoRepository<T>`, and their fake support on `MongoFakeRepository<T>`.

## Why — the consumer's problem, stated concretely

Apostil's invariant **I6** requires that every chunk record the embedding model that produced it, and
that the platform **refuse to serve a store whose recorded model differs from configuration**. The check
it needs is, for one library scope:

> which distinct embedding-model names are recorded on that scope's chunks, and how many chunks does
> each of them account for?

Today `IMongoRepository<T>` can only answer that by **loading every matching document**:

```csharp
var chunks = await repository.GetAllByFilter(chunk => chunk.Scope == scope);   // the only option today
```

Each of those documents carries a **768-float embedding**, and Apostil measured its own collection at
**11,312 bytes per chunk** of BSON (`README.md` §4.12, taken from `collStats`, not estimated). Its
documented ceiling is **~50,000 chunks per instance** (its D16). So the only available shape of the
query transfers and deserialises on the order of **565 MB** to compute two small numbers — and it would
run on **every upload** (the refusal gate) and on **every admin status check** (the health banner).

A count and a distinct answer the same question **server-side**, returning a handful of values.

**This is not a performance nicety for that consumer — it is the difference between the invariant being
enforceable and not.** Apostil's plan makes this change a hard precondition: if these methods are not
in the package, none of its phases run at all.

## Scope — and what is deliberately left out

**In scope:** `CountByFilter` and `DistinctByFilter<TValue>` on `IMongoRepository<T>` /
`MongoRepository<T>`, and their `MongoFakeRepository<T>` support.

**Explicitly out of scope:**

- **`IDataRepository<T>`.** The two methods go on **`IMongoRepository<T>`**, not on the shared data
  interface, so `FileSystemRepository<T>` is not touched and does not have to grow an in-memory
  equivalent of a database aggregation. If a file-system consumer ever needs them, that is its own work
  item with its own argument.
- **A paged/filtered query (`skip`/`limit`) and a delete-by-filter.** Both were named as out of scope by
  `mongo-batch-writes` and both are still wanted — Apostil's README §4.11 records the standing
  recommendation, and its retrieval epics will ask for the paged query. **Neither is needed here**, and
  bundling them would triple the surface of a change that is currently two methods.
- **An aggregation pipeline API** (`$group`, `$match`, arbitrary stages). One `$group` would answer the
  consumer's question in a single round trip instead of `1 + k`, and it would put the MongoDB
  aggregation framework into the toolkit's public surface, where every future consumer inherits it and
  every fake has to simulate it. Two small, ordinary, fakeable methods are the better trade at this
  size. Recorded as a rejected alternative in ADR-3 rather than as an oversight.
- **Index management.** The consumer's count runs an unindexed scan on its side; whether it creates an
  index is its decision and its API, not the toolkit's.
- **Version stepping and publishing.** Same doctrine as `rate-limiting-hook` ADR-6 and
  `mongo-batch-writes` — the plan never publishes.

## Acceptance Criteria → Phase Map

| Acceptance criterion | Proven by |
|---|---|
| `CountByFilter` issues exactly one `CountDocumentsAsync` with the caller's filter and returns its value | Phase 1 |
| `CountByFilter` returns `0` rather than throwing when nothing matches | Phase 1 |
| `DistinctByFilter<TValue>` issues exactly one `DistinctAsync` for the named field with the caller's filter | Phase 1 |
| **`DistinctByFilter` returns `null` entries when matching documents carry no value for the field** — it does not silently drop them (ADR-4) | Phase 1 |
| `DistinctByFilter` returns an empty list rather than throwing when nothing matches | Phase 1 |
| Both call `EnsureCollection()` before touching the driver, exactly as every other member does | Phase 1 |
| `MongoFakeRepository<T>` implements both, captures the filter, and lets a consumer set the returned count and the returned distinct values | Phase 1 |
| No existing member changes behaviour; no existing signature changes | Phase 1 (`git diff` covers three files) |
| No breaking change for `C:\Code\Fog` or `C:\Code\Apostil` | `consumer-compatibility.md` |
| `dotnet build ToolKit.slnx` / `dotnet test ToolKit.slnx` clean | Phase 1's Definition of Done |

## Phases

| Phase | File | Risk | Depends on |
|---|---|---|---|
| 1 — Count, distinct, and their fake support | `01-count-and-distinct.md` | **Medium** — it is additive to a published interface, so nothing existing can break at compile time; the risk is that `IMongoRepository<T>` is implemented outside this repository (a consumer's own class or fake would stop compiling) and that `DistinctAsync`'s null handling is easy to get quietly wrong. | — |

One phase: two methods with one rationale, and a fake that is useless without both.

## Current state that shapes the design (verified against source, 2026-08-12)

- `IMongoRepository<T>` (`src/ToolKit/Data/Mongo/MongoRepository.cs:8`) already exposes
  `IMongoCollection<T> Collection`, `Connect`, and two `GetById` overloads on top of
  `IDataRepository<T>`. **Adding members here is the established place for Mongo-only capability.**
- `MongoRepository<T>`'s members all call `EnsureCollection()` first and throw
  `ConnectionToMongoIsRequired` when it is null. The two new methods follow that exactly.
- `GetByFilter` is implemented as `GetAllByFilter(...).FirstOrDefault()` — **so even "is there one?"
  materialises everything that matches today.** That is worth knowing while reading the consumer's
  problem statement above.
- **`MongoFakeRepository<T>` is a canned-value fake, not an in-memory store**
  (`src/ToolKit/Testing/MongoFakeRepository.cs:49` — its inner field is `A.Fake<IMongoRepository<T>>()`).
  Its `GetAllByFilter` returns a settable `Items` list and captures the filter in `FilterCapture`. The
  new methods must follow that idiom or consumers cannot drive them.
- Solution is **`ToolKit.slnx`**; build and test from `src/`.
- Current source version is **1.0.345** (the version `mongo-batch-writes` published); this work would
  publish as **1.0.346**.

## Decisions (lightweight ADRs)

### ADR-1 — The two methods go on `IMongoRepository<T>`, not `IDataRepository<T>`

**Decision:** declare both on `IMongoRepository<T>`.

**Context:** `IDataRepository<T>` is also implemented by `FileSystemRepository<T>`, which has no query
engine — it would have to load and count in memory, which is precisely the behaviour this change exists
to avoid, dressed as an implementation. A count and a distinct are database capabilities.

**Consequence:** `MongoFakeRepository<T>` (which implements `IMongoRepository<T>`) must implement both.
`FileSystemRepository<T>` is untouched.

**Rejected:** the shared interface (a fake implementation for a file store); a separate
`IQueryMongo<T>` interface (a second thing to inject for two methods, and consumers already hold the
repository).

### ADR-2 — Additive only: no existing member changes

**Decision:** nothing that exists today changes — not a signature, not a body, not a return value.

**Context:** `mongo-batch-writes` changed three method bodies and had to argue empty-list semantics for
every consumer. This work item has no such surface: a member nobody calls cannot regress anybody. The
only compile-time risk is an implementation of `IMongoRepository<T>` **outside** this repository, which
`consumer-compatibility.md` checks explicitly.

### ADR-3 — Two ordinary methods rather than an aggregation API

**Decision:** `CountByFilter` + `DistinctByFilter`, answering the consumer's question in `1 + k` round
trips where `k` is the number of distinct values (**one** in a healthy Apostil library, two or three in
a broken one).

**Context:** a single `$group` would be one round trip. It would also put pipeline stages, `BsonDocument`
shapes, and result-projection types into the toolkit's public surface — and into every fake. `1 + k`
round trips of small results is not a cost worth that.

**Rejected:** an `Aggregate` passthrough (public surface for a framework the toolkit otherwise hides);
returning counts and values from one combined method (a bespoke result type for one caller's shape).

### ADR-4 — `DistinctByFilter` reports a missing value as `null`, and must not filter it out

**Decision:** the returned list carries whatever the field's distinct values are, **including `null`**
for matching documents that have no value for it (a missing field, an explicit null, or a document
written before the field existed).

**Context:** this is the whole reason the consumer can detect a **partially embedded** library. Apostil
treats a chunk that names no embedding model as *not comparable to configuration*, which is a mismatch
it must refuse — and a `Where(value => value is not null)` inside the toolkit would silently make that
state invisible. **This is the single easiest thing to get quietly wrong in this work item**, because
dropping nulls looks tidy and passes every test that does not plant one.

**Consequence:** the phase ships a fact planting documents with no value for the field and asserting the
`null` comes back. The doc comment says the entry may be `null` and why.

### ADR-5 — The fake gets settable results and filter captures, in the existing idiom

**Decision:** `MongoFakeRepository<T>` gains `CountByFilterResult` (settable), a `CountFilterCapture`,
and a `SetUpDistinct<TValue>(List<TValue> values)` helper plus a `DistinctFilterCapture`, mirroring how
`Items` / `FilterCapture` already work for `GetAllByFilter`.

**Context:** the fake is a canned-value fake. A consumer cannot configure a generic method's return
value through `A.CallTo` from outside without knowing the fake's internals, so the helper is what makes
`DistinctByFilter` usable in a consumer's specs at all — and the consumer that raised this work item
**cannot unit-test its invariant without it**.

**Consequence:** the fake's surface grows by four members. `Collection` stays `null`, unchanged.

**Rejected:** turning `MongoFakeRepository<T>` into a real in-memory store that evaluates filters
(a much larger change to a class every consumer's suite already depends on — a separate work item if it
is ever wanted); leaving the fake alone and telling consumers to fake `IMongoRepository<T>` themselves
(Apostil's own rules forbid that: *"use the concrete `MongoFakeRepository<T>`, do NOT fake the interface"*).

## Consumer compatibility

See `consumer-compatibility.md`. Summary: the change is additive, no consumer implements
`IMongoRepository<T>` itself, and no consumer's behaviour changes. **`C:\Code\Apostil` is the requester
and cannot start its `embedding_model_consistency` work item until this ships.**

## Publish flow (human-owned, after the phase completes)

1. Review the single commit; merge to `main` (your call how — the plan never pushes or merges).
2. From `src/`, run `PushNugetPackages.ps1` — next version **1.0.346**.
3. Bump `Api/Apostil.Api/Apostil.Api.csproj` to 1.0.346 in `C:\Code\Apostil`, build, and confirm the two
   methods resolve. **Only then does `run embedding_model_consistency` do anything** — its orchestrator
   checks for these methods first and stops without touching the repository if they are absent.
4. `Fog` needs nothing.

## Assumptions

- The rules in `src/.claude/rules/csharp/*` govern this repo's C# (TDD, xUnit + FakeItEasy +
  FatCat.Testing with `.Not.` negation, CSharpier owns formatting, warnings are errors, block bodies
  only, no expression-bodied members anywhere including tests).
- Build/test entry points from `src/`: `dotnet build ToolKit.slnx` and `dotnet test ToolKit.slnx`.
- The task branch is `mongo-count-and-distinct`; the commit policy forbids working on `main`.
- The referenced MongoDB C# driver exposes `CountDocumentsAsync`, `DistinctAsync`, and
  `ExpressionFieldDefinition<T, TValue>`. **No new package reference.** If any is unavailable, that is a
  halt and a question for the human, not a licence to upgrade a package.

## Open Questions

None blocking. Flagged for the human reviewer:

- **ADR-4's null handling is the one thing to read slowly.** Dropping nulls is the tidy-looking mistake
  that would make the consumer's invariant undetectable in exactly the state it exists to detect.
- **Nothing in this repository tests against a real MongoDB** (`mongo-batch-writes` ADR-5 said the same).
  The unit seam proves which driver call is issued with which arguments; that the query is *cheap* is
  proven in the consumer, and Apostil's phase 4 records the measurement.
- `CountByFilter` returns `long` because `CountDocumentsAsync` does. The consumer converts to `int` at
  its own boundary rather than the toolkit narrowing on its behalf.
