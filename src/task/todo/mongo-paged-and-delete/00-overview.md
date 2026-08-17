# mongo-paged-and-delete (FatCat.Toolkit) — Overview

> **Origin:** raised by a consumer. Written from `C:\Code\Apostil` while planning that repo's
> `repository_paged_queries` work item (`tasks/todo/repository_paged_queries/`), whose ADR-1 records
> that the work item **cannot be built without this change** and must not start until it has shipped.
>
> **Status:** planned (2026-08-17). One phase; runbook in `orchestrator.md`; consumer verification in
> `consumer-compatibility.md`.

## Work Item

Add **two server-side query capabilities** to `IMongoRepository<T>` — a **paged, filtered, sorted query
that also returns a total count**, and a **delete-by-filter** — and give `MongoFakeRepository<T>` the
canned-value support consumers need to drive both in unit tests.

```csharp
// src/ToolKit/Data/Mongo/MongoRepository.cs — the two new members
Task<PagedResults<T>> QueryByFilter<TSort>(
    Expression<Func<T, bool>> filter,
    Expression<Func<T, TSort>> sortBy,
    bool sortDescending,
    int skip,
    int limit);

Task<long> DeleteByFilter(Expression<Func<T, bool>> filter);
```

plus a small result type:

```csharp
public class PagedResults<T> : EqualObject
    where T : MongoObject
{
    public List<T> Items { get; set; } = [];

    public long TotalCount { get; set; }
}
```

That is the whole change: one new type, two members on `IMongoRepository<T>`, their implementations on
`MongoRepository<T>`, and their fake support on `MongoFakeRepository<T>`.

## Why — the consumer's problem, stated concretely

`C:\Code\Apostil` has **three** places that load a whole Mongo collection into memory only to keep a
slice of it, because `IMongoRepository<T>` offers no server-side `skip`/`limit` and no delete-by-filter:

| Consumer | What it does today | Why it matters |
|---|---|---|
| `Auditing/Endpoints/GetAuditRecordsEndpoint.cs` | `GetAll()`, then `OrderByDescending().Take(100)` in memory | **The sharp edge.** Its D12 exempts audit records from the conversation purge — the collection is institutional memory and **never shrinks**, so this degrades without bound. |
| `Signals/Endpoints/GetSignalReceiptsEndpoint.cs` | `GetAll()`, then projects the lot | Same shape; a dev-only diagnostic, bounded only by whatever prunes receipts. |
| `Chunking/DocumentChunkStore.cs` (`Page`) | Materialises one document's whole chunk set per page request | ~11.3 KB per passage (its README §4.12) against a ~50,000-chunk ceiling (its D16). |

A **fourth** place loads a collection only to **delete** it: `DocumentChunkStore.Replace` calls
`GetAllByFilter(chunk => chunk.DocumentId == documentId)` and then `Delete(existing)` — a whole-set read
whose only purpose is a delete. **This is the current, non-speculative consumer of delete-by-filter**
(ADR-5).

A paged, sorted query answers the first three **server-side**, transferring only the page plus a count;
a delete-by-filter answers the fourth with one command and no transfer.

**There is direct precedent.** `CountByFilter` and `DistinctByFilter` were added to this toolkit as an
explicit **precondition** of Apostil's `embedding_model_consistency` work item, for exactly this reason —
the alternative was loading every chunk in scope into memory to count them. This work item is the same
move for `skip`/`limit`/sort and for delete-by-filter, and its plan makes it a hard precondition in the
same way: if these members are not in the package, none of its phases run at all.

## Scope — and what is deliberately left out

**In scope:** `QueryByFilter<TSort>` and `DeleteByFilter` on `IMongoRepository<T>` / `MongoRepository<T>`,
the `PagedResults<T>` result type, and their `MongoFakeRepository<T>` support.

**Explicitly out of scope:**

- **`IDataRepository<T>`.** Both members go on **`IMongoRepository<T>`**, not the shared data interface,
  so `FileSystemRepository<T>` is not touched and does not have to grow an in-memory equivalent of a
  server-side page or a bulk delete. Same reasoning as `mongo-count-and-distinct` ADR-1. If a file-system
  consumer ever needs them, that is its own work item with its own argument.
- **A general query-builder / specification pattern.** This is `skip`/`limit`/one-field-sort and a
  delete-by-filter, nothing more. Multi-key sort, projections, `$group`, arbitrary stages — none of it.
  One `TSort` sort field serves both consumers (audit sorts by `OccurredOn` descending; chunks by
  `Ordinal` ascending — ADR-3). Bundling a builder would put the aggregation framework into the toolkit's
  public surface and into every fake, which `mongo-count-and-distinct` ADR-3 already rejected.
- **Index management.** The consumer's paged sort runs against whatever indexes exist on its side; whether
  it creates one is its decision and its API, not the toolkit's. Recorded as a standing recommendation to
  that human, not built here.
- **Cursor / continuation-token paging.** Offset paging (`skip`/`limit`) is what both consumers need and
  what the driver's `IFindFluent` gives directly. A keyset cursor is a larger surface nobody has asked
  for.
- **Version stepping and publishing.** Same doctrine as `rate-limiting-hook` ADR-6,
  `mongo-batch-writes`, and `mongo-count-and-distinct` — **the plan never publishes**. The human reviews
  the commit and runs `PushNugetPackages.ps1`.

## Acceptance Criteria → Phase Map

| Acceptance criterion | Proven by |
|---|---|
| `QueryByFilter` issues exactly one `CountDocumentsAsync` with the caller's filter and returns it as `TotalCount` | Phase 1 |
| `QueryByFilter` issues a `Find(filter)` sorted on the named field, `Skip(skip).Limit(limit)`, and returns those documents as `Items` | Phase 1 |
| `sortDescending` selects `Sort.Descending` vs `Sort.Ascending` on the given field — asserted both ways | Phase 1 |
| `TotalCount` counts **every** document matching the filter, not the page size (skip/limit do not touch the count) | Phase 1 |
| `QueryByFilter` returns an **empty `Items` and `TotalCount = 0`** when nothing matches — no throw | Phase 1 |
| `DeleteByFilter` issues exactly one `DeleteManyAsync` with the caller's filter and returns `DeletedCount` | Phase 1 |
| `DeleteByFilter` returns `0` rather than throwing when nothing matches (a no-op delete is legal) | Phase 1 |
| Both call `EnsureCollection()` before touching the driver, exactly as every other member does | Phase 1 |
| `MongoFakeRepository<T>` implements both, captures the filters, lets a consumer set the returned page and the returned deleted count, and captures `skip`/`limit`/`sortDescending` | Phase 1 |
| No existing member changes behaviour; no existing signature changes | Phase 1 (`git diff` covers three files) |
| No breaking change for `C:\Code\Fog` or `C:\Code\Apostil` | `consumer-compatibility.md` |
| `dotnet build ToolKit.slnx` / `dotnet test ToolKit.slnx` clean | Phase 1's Definition of Done |

## Phases

| Phase | File | Risk | Depends on |
|---|---|---|---|
| 1 — Paged query, delete-by-filter, and their fake support | `01-paged-query-and-delete.md` | **Medium** — additive to a **published interface**, so nothing existing can break at compile time inside this repository; the risk is an implementation of `IMongoRepository<T>` living outside it (a consumer's own class or fake would stop compiling), and the generic sort expression is easy to translate to the driver incorrectly. | — |

One phase: a paged query and the delete that its consumers reach for in the same class, with a fake that
is useless without both. Splitting them would produce two commits that each half-close the gap the
consumer named as one gap.

## Current state that shapes the design (verified against source, 2026-08-17)

- `IMongoRepository<T>` (`src/ToolKit/Data/Mongo/MongoRepository.cs:8`) already exposes `Collection`,
  `DatabaseName`, `Connect`, two `GetById` overloads, and — since `mongo-count-and-distinct` (1.0.346) —
  `CountByFilter` and `DistinctByFilter`. **Adding Mongo-only capability here is the established place.**
- Every `MongoRepository<T>` member calls `EnsureCollection()` first and throws
  `ConnectionToMongoIsRequired` when `Collection` is null. The two new methods follow that exactly.
- `GetAllByFilter` is `Collection.FindAsync(filter).ToListAsync()` — the whole-collection read this work
  item gives consumers an alternative to. `CountByFilter` is already `Collection.CountDocumentsAsync`, and
  `QueryByFilter` reuses that same call for its `TotalCount`.
- `Update(List<T>)` already issues one `BulkWriteAsync` (batched since 1.0.345). `Delete(List<T>)` issues
  one `DeleteManyAsync` **with an `In` filter over ids** — `DeleteByFilter` is the same driver call with
  the caller's own predicate instead of an id set.
- **`MongoFakeRepository<T>` is a canned-value fake, not an in-memory store**
  (`src/ToolKit/Testing/MongoFakeRepository.cs:55` — its inner field is `A.Fake<IMongoRepository<T>>()`).
  Its `GetAllByFilter` returns a settable `Items` list and captures the filter in `FilterCapture`;
  `CountByFilter` returns a settable `CountByFilterResult` and captures in `CountFilterCapture`;
  `DistinctByFilter` is arranged through `SetUpDistinct<TValue>` because it is generic. **The new members
  must follow those idioms or consumers cannot drive them** — and `QueryByFilter` is generic in `TSort`,
  so it needs a `SetUpQuery<TSort>()` helper exactly as `DistinctByFilter` needed `SetUpDistinct`.
- Solution is **`ToolKit.slnx`**; build and test from `src/`.
- Current source version is **1.0.348**; this work would publish as **1.0.349**.

## Decisions (lightweight ADRs)

### ADR-1 — The two members go on `IMongoRepository<T>`, not `IDataRepository<T>`

**Decision:** declare both on `IMongoRepository<T>`.

**Context:** `IDataRepository<T>` is also implemented by `FileSystemRepository<T>`, which has no query
engine — a server-side page and a bulk delete would both become "load everything and do it in memory,"
which is precisely the behaviour this change exists to avoid, dressed as an implementation. Offset paging,
sorting, and a filtered delete are database capabilities.

**Consequence:** `MongoFakeRepository<T>` (which implements `IMongoRepository<T>`) must implement both.
`FileSystemRepository<T>` is untouched.

**Rejected:** the shared interface (a fake page/delete for a file store); a separate `IQueryMongo<T>`
interface (a second thing to inject for two methods consumers already hold the repository for).

### ADR-2 — One combined `PagedResults<T>`, because every consumer needs the page **and** the count

**Decision:** `QueryByFilter` returns a `PagedResults<T>` carrying `List<T> Items` and `long TotalCount`,
rather than the page alone with a separate `CountByFilter` call.

**Context:** each consuming site needs both — a curator paging chunks needs to know how far the document
goes, and the audit endpoint wants to say "showing 100 of N." Making the caller issue two round trips
(the page, then a count) invites the two to disagree under a concurrent write for no benefit, and every
caller would write the same pairing. One method, one small POCO, one meaning.

**Consequence:** `PagedResults<T>` is a new public type in `FatCat.Toolkit.Data.Mongo`. It derives from
`EqualObject` so consumers can assert against it structurally, and `Items` initialises to `[]` so an empty
page is never a null a caller has to guard. `TotalCount` is `long` because `CountDocumentsAsync` returns
`long`; consumers narrow to `int` at their own boundary if they want to (Apostil does), exactly as
`CountByFilter`'s `long` is narrowed by its callers.

**Rejected:** returning `Items` only and telling callers to also call `CountByFilter` (two round trips, a
guaranteed pairing every caller re-writes, and a snapshot-consistency question no caller asked for);
returning a tuple (undiscoverable member names, no structural equality); a bespoke per-consumer result
type (the toolkit does not know its consumers' shapes).

### ADR-3 — One `TSort` sort field, not a query builder (consumer Q2)

**Decision:** the sort is a single `Expression<Func<T, TSort>>` plus a `bool sortDescending`. No secondary
sort key, no `SortDefinition` passthrough, no field-name strings.

**Context:** the two consumers that motivate this sort by exactly one field each — audit by `OccurredOn`
descending, chunks by `Ordinal` ascending — and a generic `TSort` serves a `DateTime` field and an `int`
field without boxing. A `SortDefinition<T>` parameter or a builder would put MongoDB's sort DSL into the
toolkit's public surface and into every fake, which is the same trade `mongo-count-and-distinct` ADR-3
rejected for aggregation. Two consumers with one sort field each do not justify a builder.

**Consequence:** `QueryByFilter` is generic in `TSort`, which is what forces the fake's `SetUpQuery<TSort>`
helper (ADR-6) — the same shape `DistinctByFilter<TValue>` already established. The real implementation
uses `Builders<T>.Sort.Descending(sortBy)` / `.Ascending(sortBy)`, whose generic overload takes the
expression directly and avoids the boxing an `Expression<Func<T, object>>` sort would introduce for value
types.

**Rejected:** `Expression<Func<T, object>>` for a non-generic signature (boxes every value-type sort field
into a `Convert` node the driver's translator handles unevenly — fragile for exactly the `int`/`DateTime`
fields the consumers use); a `SortDefinition<T>` parameter (the driver's DSL on the public surface); a
multi-key sort (no consumer needs it, and it multiplies the fake's arrangement surface).

### ADR-4 — `TotalCount` counts the whole filter; `skip`/`limit` only slice the page

**Decision:** `TotalCount` is `CountDocumentsAsync(filter)` — the count of everything matching the filter,
computed **before and independent of** `skip`/`limit`. The `Items` are `Find(filter).Sort(...).Skip(skip)
.Limit(limit)`.

**Context:** the whole reason a paged result carries a count is so a caller knows how much there is beyond
the page. A count that respected `skip`/`limit` would just re-report the page size and tell the caller
nothing. Apostil's `DocumentChunkPage` documents exactly this: "`TotalCount` is **every** chunk the
document has, not the number in `Chunks`."

**Consequence:** `QueryByFilter` issues **two** driver operations — one `CountDocumentsAsync` and one
`Find`. They are not a snapshot: a concurrent write between them can make `TotalCount` disagree with a
full read of `Items` by one. That is accepted rather than locked around — both consumers recompute on
demand, and neither makes a decision that a one-off skew breaks. Stated so a reviewer does not mistake it
for a defect.

**Rejected:** counting the page (tells the caller nothing); a single aggregation with a `$facet` to get
count and page in one round trip (the aggregation framework on the public surface again — ADR-3); a
transaction to snapshot the two (a lock for a skew no consumer is harmed by).

### ADR-5 — `DeleteByFilter` has a real current consumer, and it returns the deleted count

**Decision:** ship `DeleteByFilter(filter)` returning `long` (the driver's `DeletedCount`), and note that
its first consumer already exists.

**Context:** consumer Q3 asked whether delete-by-filter is needed or merely anticipated. It is needed:
Apostil's `DocumentChunkStore.Replace` does `GetAllByFilter(chunk => chunk.DocumentId == documentId)`
followed by `Delete(existing)` — a whole-set read whose only purpose is the delete that follows. That is
the exact load-to-delete this member removes, and Apostil's plan rewires `Replace` onto it in the same
work item. Taking it now — rather than reopening the toolkit a third time — is what the consumer's human
asked for.

**Consequence:** the return is `long DeletedCount` because the driver hands it back for free and a caller
that wants to log or assert "how many did I clear" then can. `DeleteMany` with a filter matching nothing
is a legal no-op returning `0`, so callers do not have to pre-check existence (Apostil's rewired
`Replace` drops its `if (existing.Count > 0)` guard as a result).

**Rejected:** `void`/`Task` return (throws away a number the driver already computed); a `bool didDelete`
(loses the count for no gain); leaving delete-by-filter out as speculative (it has a consumer, and
reopening the toolkit later has its own cost the consumer's human explicitly weighed).

### ADR-6 — The fake gets settable results, filter captures, and paging captures, in the existing idiom

**Decision:** `MongoFakeRepository<T>` gains:

- `PagedResults<T> QueryByFilterResult` (settable; defaults to an empty page so it is never null),
  `QueryFilterCapture`, captured `QuerySkip` / `QueryLimit` / `QuerySortDescending`, and a
  `SetUpQuery<TSort>()` helper that arranges the generic call.
- `long DeleteByFilterResult` (settable), `DeleteFilterCapture`, set up in the constructor exactly as
  `SetUpCountByFilter` is.

**Context:** the fake is a canned-value fake. A consumer cannot configure a **generic** method's return
through `A.CallTo` from outside without naming `TSort`, so `SetUpQuery<TSort>()` is what makes
`QueryByFilter` usable in a consumer's specs at all — precisely the role `SetUpDistinct<TValue>` plays for
`DistinctByFilter`. The paging arguments are captured inside the arranged call (`call.GetArgument<int>(…)`)
so a consumer can assert it asked for the right slice and direction. `DeleteByFilter` is non-generic and
needs only the `CountByFilter`-shaped treatment.

**Consequence:** the fake's surface grows by roughly seven members. `Collection` stays `null`, unchanged.
`QueryByFilterResult` defaults to `new()` (an empty page) so a consumer that forgot to arrange it gets
"nothing matched," not a `NullReferenceException` three frames away — the same courtesy
`SetUpDistinct`-less `DistinctByFilter` gives with an empty list.

**Rejected:** turning `MongoFakeRepository<T>` into a real in-memory store that evaluates filters, sorts,
and pages (a much larger change to a class every consumer's suite depends on — its own work item if ever
wanted); leaving the fake alone and telling consumers to fake `IMongoRepository<T>` themselves (Apostil's
rules forbid it: "use the concrete `MongoFakeRepository<T>`").

## Consumer compatibility

See `consumer-compatibility.md`. Summary: the change is additive, no consumer implements
`IMongoRepository<T>` itself, and no consumer's behaviour changes at compile time. **`C:\Code\Apostil` is
the requester and cannot start its `repository_paged_queries` work item until this ships.**

## Publish flow (human-owned, after the phase completes)

1. Review the single commit; merge to `main` (your call how — the plan never pushes or merges).
2. From `src/`, run `PushNugetPackages.ps1` — next version **1.0.349**.
3. Bump `Api/Apostil.Api/Apostil.Api.csproj` from 1.0.348 to 1.0.349 in `C:\Code\Apostil`, build, and
   confirm the two methods and `PagedResults<T>` resolve. **Only then does `run repository_paged_queries`
   do anything** — its orchestrator checks for these members first and stops without touching the
   repository if they are absent.
4. `Fog` needs nothing.

## Assumptions

- The rules in `src/.claude/rules/csharp/*` govern this repo's C# (TDD, xUnit + FakeItEasy +
  FatCat.Testing with `.Not.` negation, CSharpier owns formatting, warnings are errors, block bodies
  only, no expression-bodied members anywhere including tests). **Match the repo's existing idiom for
  collection initialisation and POCO base types — read `MongoRepository.cs` and a neighbouring result type
  before choosing.**
- Build/test entry points from `src/`: `dotnet build ToolKit.slnx` and `dotnet test ToolKit.slnx`.
- The task branch is `mongo-paged-and-delete`; the commit policy forbids working on `main`.
- The referenced MongoDB C# driver exposes `CountDocumentsAsync`, `Find(...).Sort(...).Skip(...).Limit(...)
  .ToListAsync()`, `DeleteManyAsync`, and `Builders<T>.Sort.Ascending/Descending<TField>(Expression<Func<T,
  TField>>)`. **No new package reference.** If any is unavailable, that is a halt and a question for the
  human, not a licence to upgrade a package.

## Open Questions

None blocking. Flagged for the human reviewer:

- **The generic sort translation is the one thing to read slowly.** `Builders<T>.Sort.Descending(sortBy)`
  with a generic `TSort` expression must reach the driver as a sort on the real field, not a boxed
  `Convert`. The phase pins this with a fact that asserts the ascending and descending sort definitions
  reach the collection.
- **Nothing in this repository tests against a real MongoDB** (`mongo-batch-writes` ADR-5 and
  `mongo-count-and-distinct` said the same). The unit seam proves which driver calls are issued with which
  arguments; that the paged query is *cheap* is proven in the consumer, and Apostil's plan records the
  measurement.
- `QueryByFilter`'s `TotalCount` and `DeleteByFilter` both return `long` because the driver does. Consumers
  narrow to `int` at their own boundary rather than the toolkit narrowing on their behalf, exactly as
  `CountByFilter` already does.
