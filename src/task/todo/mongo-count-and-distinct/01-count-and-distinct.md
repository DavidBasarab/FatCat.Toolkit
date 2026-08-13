# Phase 1 — `CountByFilter`, `DistinctByFilter`, and their fake support

- **Work item:** mongo-count-and-distinct (see `task/todo/mongo-count-and-distinct/00-overview.md`)
- **Depends on:** —
- **Depended on by:** — (single-phase work item)
- **Risk:** **Medium.** Additive to a **published interface**, so nothing existing can break at compile
  time inside this repository — but any implementation of `IMongoRepository<T>` living outside it would
  stop compiling, and `DistinctAsync`'s null handling (ADR-4) is easy to get quietly wrong in the exact
  direction that hurts the consumer.

## Context (complete handoff — read before coding)

You have no context from any prior session. Read, in this order:

1. `task/todo/mongo-count-and-distinct/00-overview.md` — **its five ADRs are binding**, especially
   **ADR-4** (nulls are values and must survive) and **ADR-5** (the fake's shape).
2. `task/todo/mongo-count-and-distinct/consumer-compatibility.md`.
3. `src/.claude/rules/csharp/*` and the repo's `CLAUDE.md` — TDD, xUnit + FakeItEasy + FatCat.Testing
   (negation is `.Not.`), block bodies only, CSharpier owns formatting, warnings are errors.
4. `src/ToolKit/Data/Mongo/MongoRepository.cs` **in full** — the interface, `EnsureCollection`, and the
   shape every member already follows.
5. `src/ToolKit/Testing/MongoFakeRepository.cs` **in full** — `SetUpGetByFilter`, `FilterCapture`,
   `Items`. **The new fake members copy that idiom exactly.**
6. `src/Tests.ToolKit/Data/Mongo/DataRepositorySpecs/` — the existing `IMongoCollection<T>` FakeItEasy
   seam, which is where the two new facts go. Read `GetAllByFilterTests` (or its nearest equivalent)
   before writing anything.

### What is true today

- `IMongoRepository<T> : IDataRepository<T>` exposes `Collection`, `DatabaseName`, `Connect`, and two
  `GetById` overloads. Every `MongoRepository<T>` member calls `EnsureCollection()` first.
- The only filtered read is `GetAllByFilter`, which materialises every matching document.
  `GetByFilter` is `GetAllByFilter(...).FirstOrDefault()`.
- `MongoFakeRepository<T>` wraps `A.Fake<IMongoRepository<T>>()` and returns canned values; its
  `Collection` is `null`.
- Working branch: **`mongo-count-and-distinct`** — create it from `main` if the repository is on `main`,
  otherwise use the current branch. Do not push.

## Design (build exactly this shape)

### 1. `src/ToolKit/Data/Mongo/MongoRepository.cs` — the interface

Add to `IMongoRepository<T>`, with doc comments that carry the two facts a caller must know:

```csharp
/// <summary>
/// The number of documents matching <paramref name="filter" />, counted <b>on the server</b>: no
/// document is transferred or deserialised. Returns <c>0</c> when nothing matches.
/// </summary>
Task<long> CountByFilter(Expression<Func<T, bool>> filter);

/// <summary>
/// The distinct values of <paramref name="field" /> across the documents matching
/// <paramref name="filter" />, computed <b>on the server</b>. Returns an empty list when nothing
/// matches.
/// <para>
/// <b>A matching document that carries no value for the field contributes <c>null</c> to the result,
/// and that entry is deliberately not filtered out.</b> "Some documents have no value here" is
/// frequently the answer a caller is asking for — dropping it would make that state invisible.
/// </para>
/// </summary>
Task<List<TValue>> DistinctByFilter<TValue>(Expression<Func<T, TValue>> field, Expression<Func<T, bool>> filter);
```

### 2. `MongoRepository<T>` — the two implementations

```csharp
public async Task<long> CountByFilter(Expression<Func<T, bool>> filter)
{
    EnsureCollection();

    return await Collection.CountDocumentsAsync(filter);
}

public async Task<List<TValue>> DistinctByFilter<TValue>(Expression<Func<T, TValue>> field, Expression<Func<T, bool>> filter)
{
    EnsureCollection();

    var cursor = await Collection.DistinctAsync(new ExpressionFieldDefinition<T, TValue>(field), filter);

    return await cursor.ToListAsync();
}
```

**No `Where`, no null filtering, no ordering, no distinct-of-distinct** (ADR-4). Whatever the server
returns is what the caller gets.

### 3. `MongoFakeRepository<T>` — canned values in the existing idiom (ADR-5)

```csharp
public long CountByFilterResult { get; set; }

public EasyCapture<Expression<Func<T, bool>>> CountFilterCapture { get; private set; }

public EasyCapture<Expression<Func<T, bool>>> DistinctFilterCapture { get; private set; }

public void SetUpDistinct<TValue>(List<TValue> values);
```

- `CountByFilter` returns `CountByFilterResult` and captures its filter — set up in a new
  `SetUpCountByFilter()` called from the constructor, exactly as `SetUpGetByFilter()` is.
- `SetUpDistinct<TValue>(values)` configures
  `A.CallTo(() => repository.DistinctByFilter<TValue>(A<Expression<Func<T, TValue>>>._, DistinctFilterCapture)).ReturnsLazily(() => values)`.
  **Without a call to it, `DistinctByFilter` must return an empty list rather than null** — a consumer
  that forgot to arrange it gets "nothing matched", not a `NullReferenceException` three frames away.
- **`Collection` stays `null` and nothing else in the fake changes.**

### Do not add in this phase

- **No paged query, no delete-by-filter, no aggregation passthrough, no index management** (00-overview
  scope section). A phase that adds one has left this work item — **stop and report**.
- **Nothing on `IDataRepository<T>` and nothing in `FileSystemRepository<T>`** (ADR-1).
- **No change to any existing member's body or signature** (ADR-2).
- **No package reference, no package upgrade, no version step, no publish.**

## Steps (TDD — tests first, red before green)

1. **Baseline:** `dotnet build ToolKit.slnx` and `dotnet test ToolKit.slnx` from `src/`. Record the
   warning count and the test count; a pre-existing red makes every verification below meaningless.
2. **Write the specs and run red**, in `src/Tests.ToolKit/Data/Mongo/DataRepositorySpecs/`, using the
   existing faked `IMongoCollection<T>` seam:

   | `[Fact]` | Asserts |
   |---|---|
   | `CallCountDocumentsWithTheFilter` | exactly one `CountDocumentsAsync` carrying the caller's filter |
   | `ReturnTheCount` | the driver's value is returned unchanged |
   | `ReturnZeroWhenNothingMatches` | `0`, not a throw |
   | `RequireAConnectionToCount` | `ConnectionToMongoIsRequired` when `Collection` is null, before the driver is touched |
   | `CallDistinctWithTheFieldAndFilter` | exactly one `DistinctAsync` for the named field and the caller's filter |
   | `ReturnTheDistinctValues` | the cursor's values, in the order the driver gave them |
   | `ReturnANullValueForDocumentsWithNoValue` | **a `null` entry survives** (ADR-4) |
   | `ReturnAnEmptyListWhenNothingMatches` | empty, not null, not a throw |
   | `RequireAConnectionToGetDistinctValues` | as above |

   And for the fake, in whatever spec folder covers `MongoFakeRepository<T>` (create one if there is
   none — the fake is public surface consumers depend on):

   | `[Fact]` | Asserts |
   |---|---|
   | `ReturnTheConfiguredCount` | `CountByFilterResult` comes back |
   | `CaptureTheCountFilter` | `CountFilterCapture` holds the expression, and compiling it against a matching item returns true |
   | `ReturnTheConfiguredDistinctValues` | after `SetUpDistinct` |
   | `ReturnNoDistinctValuesWhenNoneWereSetUp` | **empty list, never null** |
   | `CaptureTheDistinctFilter` | `DistinctFilterCapture` holds the expression |

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
- [ ] Exactly two new members on `IMongoRepository<T>`, both implemented on `MongoRepository<T>`
- [ ] Both call `EnsureCollection()` before the driver — asserted
- [ ] **A `null` distinct value survives** — asserted (ADR-4)
- [ ] Distinct with no matches returns an **empty list, not null** — asserted, on both the real and the
      fake implementation
- [ ] `MongoFakeRepository<T>` implements both with a settable count, a `SetUpDistinct<TValue>` helper,
      and both filter captures — asserted
- [ ] `IDataRepository<T>` and `FileSystemRepository<T>` are **not** in the diff
- [ ] No existing member's body or signature changed — `git diff` reviewed and stated
- [ ] No package reference added or upgraded; no version step; nothing published
- [ ] `dotnet build ToolKit.slnx` zero warnings; `dotnet test ToolKit.slnx` green
- [ ] Exactly one commit referencing this file; **nothing pushed**

Suggested commit message:

```
mongo-count-and-distinct phase 1: server-side CountByFilter and DistinctByFilter with fake support (task/todo/mongo-count-and-distinct/01-count-and-distinct.md)
```

## Rollback Procedure

- `git revert <phase-1-commit>`. Nothing else in the repository references the new members, so the
  revert is self-contained.
- **Data step:** none — nothing is written by either method.
- **Consumer step:** none unless the package was already published and a consumer bumped to it. If
  `C:\Code\Apostil` has taken 1.0.346, reverting this leaves it unable to compile — **pin it back to
  1.0.345 first.**

## Phase Report (produce before finishing)

Files added and changed; test counts (new, total, passing); **deviation log**, explicitly stating
"none" if there were none. Specifically required:

- What **red** looked like.
- The exact `grep` output from step 4 (who implements `IMongoRepository<T>`).
- The doc comment shipped on `DistinctByFilter`, quoted — **the human's chance to object to the words a
  future caller inherits about nulls.**
- A one-line statement of what a consumer must do to use this (bump the package version; nothing else).

## Hand-off

What consumers may rely on after this ships:

- **`IMongoRepository<T>.CountByFilter(filter)`** → `long`, server-side, `0` when nothing matches.
- **`IMongoRepository<T>.DistinctByFilter<TValue>(field, filter)`** → `List<TValue>`, server-side, empty
  when nothing matches, **including `null` entries for matching documents with no value for the field.**
- **`MongoFakeRepository<T>`**: `CountByFilterResult`, `CountFilterCapture`, `SetUpDistinct<TValue>(...)`,
  `DistinctFilterCapture`; distinct returns an empty list when it was never set up.
- Published as **1.0.346** by the human, after review.
