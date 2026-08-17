# Phase 1 — `QueryByFilter`, `DeleteByFilter`, `PagedResults<T>`, and their fake support

- **Work item:** mongo-paged-and-delete (see `task/todo/mongo-paged-and-delete/00-overview.md`)
- **Depends on:** —
- **Depended on by:** — (single-phase work item)
- **Risk:** **Medium.** Additive to a **published interface**, so nothing existing can break at compile
  time inside this repository — but any implementation of `IMongoRepository<T>` living outside it would
  stop compiling, and the generic sort expression (ADR-3) is easy to translate to the driver as a boxed
  `Convert` instead of a real field sort.

## Context (complete handoff — read before coding)

You have no context from any prior session. Read, in this order:

1. `task/todo/mongo-paged-and-delete/00-overview.md` — **its six ADRs are binding**, especially **ADR-3**
   (one generic sort field, not a builder), **ADR-4** (`TotalCount` counts the whole filter), and
   **ADR-6** (the fake's shape, and why `QueryByFilter` needs a `SetUpQuery<TSort>` helper).
2. `task/todo/mongo-paged-and-delete/consumer-compatibility.md`.
3. `src/.claude/rules/csharp/*` and the repo's `CLAUDE.md` — TDD, xUnit + FakeItEasy + FatCat.Testing
   (negation is `.Not.`), block bodies only, CSharpier owns formatting, warnings are errors.
4. `src/ToolKit/Data/Mongo/MongoRepository.cs` **in full** — the interface, `EnsureCollection`,
   `CountByFilter`, `DistinctByFilter`, `Delete(List<T>)`, and the shape every member already follows.
5. `src/ToolKit/Testing/MongoFakeRepository.cs` **in full** — `SetUpGetByFilter`, `FilterCapture`,
   `SetUpCountByFilter` / `CountByFilterResult` / `CountFilterCapture`, and `SetUpDistinct<TValue>` /
   `DistinctFilterCapture`. **The new fake members copy those idioms exactly.**
6. `src/Tests.ToolKit/Data/Mongo/DataRepositorySpecs/` — the existing faked `IMongoCollection<T>` seam,
   which is where the real-implementation facts go. Read the `CountByFilter` / `DistinctByFilter` specs
   added by `mongo-count-and-distinct` before writing anything — they are the nearest model.
7. `src/Tests.ToolKit/Testing/MongoFakeRepositorySpecs/` — where the fake facts go.

### What is true today

- `IMongoRepository<T> : IDataRepository<T>` exposes `Collection`, `DatabaseName`, `Connect`, two
  `GetById` overloads, `CountByFilter`, and `DistinctByFilter`. Every `MongoRepository<T>` member calls
  `EnsureCollection()` first.
- `CountByFilter` is `Collection.CountDocumentsAsync(filter)`. `Delete(List<T>)` is one `DeleteManyAsync`
  over an id `In` filter. `GetAllByFilter` is `Collection.FindAsync(filter).ToListAsync()`.
- `MongoFakeRepository<T>` wraps `A.Fake<IMongoRepository<T>>()` and returns canned values; its
  `Collection` is `null`. Generic reads are arranged through a `SetUp…<T>` helper (`SetUpDistinct`).
- Current source version is **1.0.348**; **do not** step it.
- Working branch: **`mongo-paged-and-delete`** — create it from `main` only if the repository is on
  `main`, otherwise use the current branch. Do not push.

## Design (build exactly this shape)

### 1. `src/ToolKit/Data/Mongo/PagedResults.cs` — the result type (ADR-2)

One class per file, named after the class. Match the repo's existing POCO idiom for the base type and
collection initialisation (read a neighbouring result/value type first):

```csharp
namespace FatCat.Toolkit.Data.Mongo;

/// <summary>
/// One page of a filtered, sorted query, together with the total number of documents that matched the
/// filter. <b><see cref="TotalCount" /> is every matching document, not the size of <see cref="Items" /></b>
/// — a caller paging through results needs to know how much lies beyond the page, and a count that
/// respected <c>skip</c>/<c>limit</c> would only re-report the page size (ADR-4).
/// </summary>
public class PagedResults<T> : EqualObject
    where T : MongoObject
{
    public List<T> Items { get; set; } = [];

    public long TotalCount { get; set; }
}
```

### 2. `IMongoRepository<T>` — the two new members

Add, with doc comments that carry the facts a caller must know:

```csharp
/// <summary>
/// A single page of the documents matching <paramref name="filter" />, sorted on
/// <paramref name="sortBy" /> (<paramref name="sortDescending" /> chooses the direction), sliced by
/// <paramref name="skip" /> and <paramref name="limit" /> — all <b>on the server</b>. The returned
/// <see cref="PagedResults{T}.TotalCount" /> is <b>every</b> document matching the filter, independent of
/// <paramref name="skip" /> and <paramref name="limit" />. Returns an empty page with a <c>0</c> count
/// when nothing matches.
/// </summary>
Task<PagedResults<T>> QueryByFilter<TSort>(
    Expression<Func<T, bool>> filter,
    Expression<Func<T, TSort>> sortBy,
    bool sortDescending,
    int skip,
    int limit);

/// <summary>
/// Deletes every document matching <paramref name="filter" /> in one server-side command and returns how
/// many were removed. A filter matching nothing is a legal no-op that returns <c>0</c> and does not throw.
/// </summary>
Task<long> DeleteByFilter(Expression<Func<T, bool>> filter);
```

### 3. `MongoRepository<T>` — the two implementations

```csharp
public async Task<PagedResults<T>> QueryByFilter<TSort>(
    Expression<Func<T, bool>> filter,
    Expression<Func<T, TSort>> sortBy,
    bool sortDescending,
    int skip,
    int limit
)
{
    EnsureCollection();

    var totalCount = await Collection.CountDocumentsAsync(filter);

    var sort = sortDescending
        ? Builders<T>.Sort.Descending(sortBy)
        : Builders<T>.Sort.Ascending(sortBy);

    var items = await Collection.Find(filter).Sort(sort).Skip(skip).Limit(limit).ToListAsync();

    return new PagedResults<T> { Items = items, TotalCount = totalCount };
}

public async Task<long> DeleteByFilter(Expression<Func<T, bool>> filter)
{
    EnsureCollection();

    var result = await Collection.DeleteManyAsync(filter);

    return result.DeletedCount;
}
```

**Use the generic `Sort.Descending(sortBy)` / `Sort.Ascending(sortBy)` overload — not
`Expression<Func<T, object>>` and not a hand-built `SortDefinition` string** (ADR-3). Whatever the server
returns is the page; no in-memory re-sort, re-skip, or re-count.

### 4. `MongoFakeRepository<T>` — canned values in the existing idiom (ADR-6)

```csharp
public PagedResults<T> QueryByFilterResult { get; set; } = new();

public EasyCapture<Expression<Func<T, bool>>> QueryFilterCapture { get; private set; }

public int QuerySkip { get; private set; }

public int QueryLimit { get; private set; }

public bool QuerySortDescending { get; private set; }

public long DeleteByFilterResult { get; set; }

public EasyCapture<Expression<Func<T, bool>>> DeleteFilterCapture { get; private set; }

public void SetUpQuery<TSort>();
```

- `SetUpQuery<TSort>()` arranges
  `A.CallTo(() => repository.QueryByFilter<TSort>(QueryFilterCapture, A<Expression<Func<T, TSort>>>._, A<bool>._, A<int>._, A<int>._))`
  to capture `sortDescending` / `skip` / `limit` (`call.GetArgument<bool>(2)`, `<int>(3)`, `<int>(4)`) into
  `QuerySortDescending` / `QuerySkip` / `QueryLimit` and return `QueryByFilterResult`. It mirrors how
  `SetUpDistinct<TValue>` arranges its generic call. **`QueryByFilterResult` defaults to `new()`** — a
  consumer that never calls `SetUpQuery` gets an empty page, never a null.
- `DeleteByFilter` returns `DeleteByFilterResult` and captures its filter — set up in a new
  `SetUpDeleteByFilter()` called from the constructor, exactly as `SetUpCountByFilter()` is.
- Add `VerifyDeleteByFilter()` / `VerifyDidNotDeleteByFilter()` only if it matches the fake's existing
  `Verify…` idiom — otherwise the `DeleteFilterCapture` and a `MustHaveHappened` are enough. Do not invent
  a verification style the fake does not already use.
- **`Collection` stays `null` and nothing else in the fake changes.**

### Do not add in this phase

- **No query builder, no multi-key sort, no projection, no aggregation passthrough, no cursor/continuation
  paging, no index management** (00-overview scope). A phase that adds one has left this work item —
  **stop and report**.
- **Nothing on `IDataRepository<T>` and nothing in `FileSystemRepository<T>`** (ADR-1).
- **No change to any existing member's body or signature** (ADR-2/consumer-compatibility).
- **No package reference, no package upgrade, no version step, no publish.**

## Steps (TDD — tests first, red before green)

1. **Baseline:** `dotnet build ToolKit.slnx` and `dotnet test ToolKit.slnx` from `src/`. Record the
   warning count and the test count; a pre-existing red makes every verification below meaningless.
2. **Write the specs and run red**, using the existing faked `IMongoCollection<T>` seam for the real
   implementation and the fake's own spec folder for the fake:

   | `[Fact]` (real implementation) | Asserts |
   |---|---|
   | `CountTheWholeFilterForTheTotal` | one `CountDocumentsAsync` with the caller's filter; its value is `TotalCount` (ADR-4) |
   | `FindTheFilteredSlice` | a `Find(filter).Skip(skip).Limit(limit)` issues, and `Items` are what it returns |
   | `SortDescendingWhenAsked` | the sort definition is `Descending` on the given field |
   | `SortAscendingWhenAsked` | the sort definition is `Ascending` on the given field |
   | `ReturnAnEmptyPageWhenNothingMatches` | empty `Items`, `TotalCount` `0`, no throw |
   | `RequireAConnectionToQuery` | `ConnectionToMongoIsRequired` when `Collection` is null, before the driver is touched |
   | `DeleteEveryMatchWithOneCommand` | exactly one `DeleteManyAsync` with the caller's filter |
   | `ReturnTheDeletedCount` | the driver's `DeletedCount` comes back |
   | `ReturnZeroWhenNothingMatchesTheDelete` | `0`, not a throw |
   | `RequireAConnectionToDelete` | as above |

   | `[Fact]` (fake) | Asserts |
   |---|---|
   | `ReturnTheConfiguredPage` | `QueryByFilterResult` comes back after `SetUpQuery<TSort>()` |
   | `ReturnAnEmptyPageWhenNoneWasSetUp` | **an empty page, never null**, when `SetUpQuery` was not called |
   | `CaptureTheQueryFilter` | `QueryFilterCapture` holds the expression; compiling it against a matching item returns true |
   | `CaptureThePagingArguments` | `QuerySkip` / `QueryLimit` / `QuerySortDescending` hold what was passed |
   | `ReturnTheConfiguredDeletedCount` | `DeleteByFilterResult` comes back |
   | `CaptureTheDeleteFilter` | `DeleteFilterCapture` holds the expression |

   **Run the suite and observe red. Record what red looked like.**
3. **Implement to green.**
4. **Prove the outside-implementer risk is real or absent** — `grep -rn "IMongoRepository<" src --include=*.cs`
   and confirm the only implementations in this repository are `MongoRepository<T>` and
   `MongoFakeRepository<T>`. Record the output.
5. **Gates:** build clean at zero warnings, suite green, then this repository's formatting/analyzer
   commands per its `CLAUDE.md`.
6. Exactly one commit on the task branch; **nothing pushed**.

## Definition of Done (all mandatory)

- [ ] Baseline confirmed green at the recorded warning count before any change
- [ ] Tests written before implementation; **red observed and recorded**
- [ ] `PagedResults<T>` exists with `Items` (initialised, never null) and `long TotalCount`
- [ ] `QueryByFilter<TSort>` and `DeleteByFilter` are on `IMongoRepository<T>`, both implemented on
      `MongoRepository<T>`, both calling `EnsureCollection()` before the driver — asserted
- [ ] **`TotalCount` counts the whole filter, not the page** — asserted (ADR-4)
- [ ] **Descending and ascending sort each reach the driver correctly** — both asserted (ADR-3)
- [ ] Query with no matches returns an empty page (not null, no throw); delete with no matches returns `0`
- [ ] `MongoFakeRepository<T>` implements both with a settable page, a `SetUpQuery<TSort>()` helper, a
      settable deleted count, all three paging captures, and both filter captures — asserted; unarranged
      `QueryByFilter` returns an empty page, not null
- [ ] `IDataRepository<T>` and `FileSystemRepository<T>` are **not** in the diff
- [ ] No existing member's body or signature changed — `git diff` reviewed and stated
- [ ] No package reference added or upgraded; no version step; nothing published
- [ ] `dotnet build ToolKit.slnx` zero warnings; `dotnet test ToolKit.slnx` green
- [ ] Exactly one commit referencing this file; **nothing pushed**

Suggested commit message:

```
mongo-paged-and-delete phase 1: server-side QueryByFilter and DeleteByFilter with PagedResults and fake support (task/todo/mongo-paged-and-delete/01-paged-query-and-delete.md)
```

## Rollback Procedure

- `git revert <phase-1-commit>`. Nothing else in the repository references the new members or type, so the
  revert is self-contained.
- **Data step:** none — `QueryByFilter` writes nothing, and `DeleteByFilter` is only exercised by tests
  against the faked collection.
- **Consumer step:** none unless the package was already published and a consumer bumped to it. If
  `C:\Code\Apostil` has taken 1.0.349, reverting this leaves it unable to compile — **pin it back to
  1.0.348 first.**

## Phase Report (produce before finishing)

Files added and changed; test counts (new, total, passing); **deviation log**, explicitly stating "none"
if there were none. Specifically required:

- What **red** looked like.
- The exact `grep` output from step 4 (who implements `IMongoRepository<T>`).
- The doc comments shipped on `QueryByFilter`, `DeleteByFilter`, and `PagedResults<T>`, quoted — **the
  human's chance to object to the words a future caller inherits about `TotalCount` and about a no-op
  delete.**
- A one-line statement of what a consumer must do to use this (bump the package version; nothing else).

## Hand-off

What consumers may rely on after this ships:

- **`IMongoRepository<T>.QueryByFilter<TSort>(filter, sortBy, sortDescending, skip, limit)`** →
  `PagedResults<T>` server-side: `Items` is the sorted, sliced page; `TotalCount` is every matching
  document; empty page + `0` when nothing matches.
- **`IMongoRepository<T>.DeleteByFilter(filter)`** → `long`, one server-side `DeleteMany`, `0` on no match.
- **`PagedResults<T>`** (`FatCat.Toolkit.Data.Mongo`): `List<T> Items` (initialised), `long TotalCount`,
  `EqualObject` for structural assertions.
- **`MongoFakeRepository<T>`**: `QueryByFilterResult` (defaults to an empty page), `SetUpQuery<TSort>()`,
  `QueryFilterCapture`, `QuerySkip` / `QueryLimit` / `QuerySortDescending`, `DeleteByFilterResult`,
  `DeleteFilterCapture`.
- Published as **1.0.349** by the human, after review.
