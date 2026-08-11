# Phase 1 — Batch the three list overloads on `MongoRepository<T>`

**Risk: Medium.** One production file, three method bodies, no API change — but it changes the wire
behaviour of the most-used data class in a published package, and **no test in this repository runs
against a real MongoDB**. Read ADR-4 in `00-overview.md` before writing a line: the empty-list case is
the one way this breaks a consumer.

**Depends on:** nothing. **Depended on by:** nothing.

## What changes

**One production file:** `src/ToolKit/Data/Mongo/MongoRepository.cs`.
**Three test files rewritten, three facts added:** `src/Tests.ToolKit/Data/Mongo/DataRepositorySpecs/`.

Nothing else. No interface, no new member, no signature, no package reference, no
`FileSystemRepository`, no `MongoFakeRepository`.

## The current code, verbatim

```csharp
// line 44
public async Task<List<T>> Create(List<T> items)
{
    foreach (var item in items)
    {
        await Create(item);
    }

    return items;
}

// line 63
public async Task<List<T>> Delete(List<T> items)
{
    foreach (var item in items)
    {
        await Delete(item);
    }

    return items;
}

// line 136
public async Task<List<T>> Update(List<T> items)
{
    foreach (var item in items)
    {
        await Update(item);
    }

    return items;
}
```

Each delegates to the single-item overload, which calls `EnsureCollection()` and then one
`InsertOneAsync` / `DeleteOneAsync` / `ReplaceOneAsync`. For a list of *N*, that is *N* awaited round
trips.

## The target shape

Guard, ensure, one command, return. The guard is not defensiveness — see ADR-4.

```csharp
public async Task<List<T>> Create(List<T> items)
{
    if (items.Count == 0)
    {
        return items;
    }

    EnsureCollection();

    await Collection.InsertManyAsync(items);

    return items;
}

public async Task<List<T>> Delete(List<T> items)
{
    if (items.Count == 0)
    {
        return items;
    }

    EnsureCollection();

    await Collection.DeleteManyAsync(Builders<T>.Filter.In(item => item.Id, items.Select(item => item.Id)));

    return items;
}

public async Task<List<T>> Update(List<T> items)
{
    if (items.Count == 0)
    {
        return items;
    }

    EnsureCollection();

    await Collection.BulkWriteAsync(items.Select(ReplaceModelFor));

    return items;
}

private ReplaceOneModel<T> ReplaceModelFor(T item)
{
    return new ReplaceOneModel<T>(Builders<T>.Filter.Eq(current => current.Id, item.Id), item);
}
```

This is the shape, not a transcription to paste — match the file's own idiom and let CSharpier format
it. `EnsureCollection` is `private` on this class and stays that way.

**No options object anywhere** (ADR-2): the driver's default `IsOrdered = true` is what makes batched
failure behave like today's sequential loop.

## TDD — the red is already written for you

**This is the unusual and valuable part of this phase.** The existing specs pin the behaviour being
replaced, in as many words:

```csharp
// CreateItemListTests.cs
[Fact]
public async Task CallInsertOneForEachItemInList()
{
    await repository.Create(itemList);

    foreach (var currentItem in itemList)
    {
        A.CallTo(() => collection.InsertOneAsync(currentItem, default, default)).MustHaveHappened();
    }
}

// DeleteListTests.cs
[Fact]
public async Task CallDeleteOneForAllItems()
{
    await repository.Delete(itemList);

    A.CallTo(() => collection.DeleteOneAsync(A<ExpressionFilterDefinition<TestingMongoObject>>._, default))
        .MustHaveHappened(itemList.Count, Times.Exactly);
}
```

`UpdateListTests.cs` has the matching `ReplaceOneAsync` fact.

**Rewrite these three facts first, before touching production code.** They then fail against the
current implementation, and that failure is the red this phase is built on — a genuine
specification-first red rather than a compile error. Record what it looked like in the phase report.

Each becomes a single-command assertion **plus an explicit negative** — the negative is what actually
proves the loop is gone:

```csharp
[Fact]
public async Task CallInsertManyOnce()
{
    await repository.Create(itemList);

    A.CallTo(() => collection.InsertManyAsync(itemList, default, default)).MustHaveHappenedOnceExactly();
}

[Fact]
public async Task NotCallInsertOne()
{
    await repository.Create(itemList);

    A.CallTo(() => collection.InsertOneAsync(A<TestingMongoObject>._, default, default)).MustNotHaveHappened();
}
```

`ReturnListOfCreatedItems`, `ReturnDeleteItemList`, the `Update` equivalent, and the inherited
`EnsureCollection` fact all stay **unchanged and passing** — `TestMethod()` uses the 4-item `itemList`,
so the guard never fires for them.

### The three facts to add — ADR-4, and do not skip them

One per method. `MustNotHaveHappened()` on the collection itself, not merely "did not throw":

```csharp
[Fact]
public async Task NotTouchTheCollectionForAnEmptyList()
{
    await repository.Create([]);

    A.CallTo(collection).MustNotHaveHappened();
}
```

**Why this matters more than it looks.** Today `Create([])` is a silent successful no-op — the
`foreach` runs zero times and the driver is never reached. `InsertManyAsync` with an empty sequence and
`BulkWriteAsync` with no requests **throw** (`Must contain at least 1 request`). Without the guard, a
performance fix silently becomes a runtime breaking change for any consumer that passes an empty list —
and Apostil's chunk store documents `Replace(id, [])` as a legal call.

## Steps

1. `git rev-parse --abbrev-ref HEAD` — confirm `mongo-batch-writes`, not `main`.
2. Rewrite the three per-item facts into the single-command + negative pairs above. **Run the suite and
   record the red**, quoting it.
3. Add the three empty-list facts. Run again — they will fail differently (or pass vacuously against the
   current `foreach`, which is itself worth noting: they only become meaningful after step 4, and the
   phase report should say so honestly).
4. Change the three method bodies. Run the suite green.
5. **Verify the driver API surface actually used.** `MongoDB.Driver` is pinned at **3.10.0** in
   `ToolKit.csproj`. If `InsertManyAsync`, `DeleteManyAsync`, `BulkWriteAsync`, `ReplaceOneModel<T>`,
   `Builders<T>.Filter.In` or `Builders<T>.Filter.Eq` is not available as used, **STOP and report** —
   adding or upgrading a package is not this phase's to do.
6. `dotnet build ToolKit.slnx` clean, `dotnet test ToolKit.slnx` green, CSharpier/format per the repo's
   rules.
7. The review loop the repo's rules require.
8. Exactly one commit referencing this file.

## The greps (run them; put the output in the report)

```
# one production file, and nothing else
git diff --name-only

# no interface or fake was touched
git diff --name-only | grep -E "DataRepository.cs|MongoFakeRepository.cs|FileSystemRepository.cs"

# the loops are gone
grep -n "foreach" src/ToolKit/Data/Mongo/MongoRepository.cs

# no options object smuggled in (ADR-2)
grep -n "IsOrdered\|InsertManyOptions\|BulkWriteOptions" src/ToolKit/Data/Mongo/MongoRepository.cs

# no new package
git diff -- src/ToolKit/ToolKit.csproj
```

The `foreach` grep should return **only** whatever loops exist elsewhere in the file, if any — the
three list overloads must have none.

## Definition of Done

- The three list overloads issue exactly one driver command each, asserted positively **and** with a
  `MustNotHaveHappened` negative on the per-item call.
- Three empty-list facts assert the collection is **never touched**.
- Every pre-existing fact in `DataRepositorySpecs/` still passes, including the inherited
  `EnsureCollection` throw.
- `git diff --name-only` shows **one** production file plus test files. No interface, no fake, no
  `FileSystemRepository`, no csproj.
- `dotnet build ToolKit.slnx` clean; `dotnet test ToolKit.slnx` green.
- The review loop passed.
- Exactly one commit, referencing `src/task/todo/mongo-batch-writes/01-batch-writes.md`. Nothing
  amended, squashed, pushed, published, or version-stepped.

## Report these explicitly

- **The red, quoted** — the three rewritten facts failing against the `foreach` implementation.
- **Whether the empty-list facts passed vacuously before step 4**, stated honestly either way.
- The five greps' output.
- Any place the target shape above did not survive contact with driver 3.10.0, and what you wrote
  instead.
- A plain statement that **nothing in this repository proves the batched path works against a real
  MongoDB** (ADR-5), and that the consumer re-measure in the publish flow is the only end-to-end
  evidence this change will have.

## Out of scope — stop and report if you find yourself here

- Adding `skip`/`limit`, a paged query, or a delete-by-filter. Interface change; separate work item.
- Touching `FileSystemRepository<T>` or `MongoFakeRepository<T>`.
- `IsOrdered = false`, or exposing any options parameter.
- Version stepping, `PushNugetPackages.ps1`, or publishing.
- Adding or upgrading any package, including the MongoDB driver.
- Adding a Testcontainers or live-MongoDB integration test (ADR-5 — it is a bigger piece of work than
  the change it would guard, and it is the human's call, not this phase's).
