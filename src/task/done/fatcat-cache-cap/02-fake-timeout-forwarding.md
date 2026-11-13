# Phase 02 — `FakeFatCatCache<T>` forwards the timeout; widened and new `Verify*`

**Risk: HIGH, flagged for extra human review.** `FakeFatCatCache<T>` ships in `FatCat.Toolkit.Testing` and is used by consumer test suites (19 test files in `C:\Code\Fog`). This phase changes what its fake records and adds public overloads.

- **Depends on:** nothing.
- **Depended on by:** 04, 05.
- **Wave:** 1.
- **Parallel with:** 01. Disjoint write sets, and this phase edits no hotspot. It reads `Tests.FatCat.Toolkit.Caching.TestCacheItem` and must **not** edit it: phase 01 moves it, unchanged, from `CacheTests.cs` to `TestCacheItem.cs`. The type and namespace are identical in both trees.

## Read first

1. `src/task/todo/fatcat-cache-cap.md`: problem 6 and **R9**.
2. `src/task/todo/fatcat-cache-cap/00-overview.md`: ADR-7.
3. `src/task/todo/fatcat-cache-cap/consumer-compatibility.md`: the Fog section on the fake.
4. `src/CLAUDE.md` and `src/.claude/rules/csharp/*`.
5. `src/ToolKit/Testing/FakeFatCatCache.cs`, and prior art for testing a shipped fake: `src/Tests.ToolKit/Testing/MongoFakeRepositorySpecs/`.

## Goal

- `Add(T cacheItem, TimeSpan? timeout)` calls `Cache.Add(cacheItem, timeout)`. `Add(List<T> cacheItems, TimeSpan? timeout)` calls `Cache.Add(cacheItems, timeout)`.
- `VerifyAdd(T? expectedItem = null)`, `VerifyAddList(List<T>? expectedItems = null)`, `VerifyDidNotAdd(T? expectedItem = null)` and `VerifyDidNotAddList(List<T>? expectedItems = null)` match the timeout with `A<TimeSpan?>._` instead of `default`. Their meaning ("added / not added, with any timeout") is unchanged for every existing caller.
- New: `public void VerifyAdd(T expectedItem, TimeSpan? expectedTimeout)` and `public void VerifyAddList(List<T> expectedItems, TimeSpan? expectedTimeout)`. Both parameters are required, so a one-argument call still binds to the existing overload. The file is `#nullable enable`; annotate honestly.
- Nothing else in the fake changes.

## Tests first (red before green)

Create `src/Tests.ToolKit/Testing/FakeFatCatCacheSpecs/`, namespace `Tests.FatCat.Toolkit.Testing.FakeFatCatCacheSpecs`.

**`FakeFatCatCacheTests.cs`: the abstract base.**

- `protected readonly FakeFatCatCache<TestCacheItem> fake = new();`
- `protected readonly TestCacheItem item = Faker.Create<TestCacheItem>();`
- `protected readonly List<TestCacheItem> items = Faker.Create<List<TestCacheItem>>();`
- `protected readonly TimeSpan timeout = Faker.Create<TimeSpan>();` plus a different `otherTimeout` (for example `timeout + 1.Minutes()`).

Add `using Tests.FatCat.Toolkit.Caching;` for `TestCacheItem`.

A failing verification throws FakeItEasy's `ExpectationException`. Assert it with the FatCat.Testing synchronous `Should().Throw<ExpectationException>()` on an `Action`. If that API does not exist, use `Assert.Throws` and log the deviation.

| Class | Facts |
|---|---|
| `AddTests` | `ForwardTheTimeoutToTheInnerCache` (`A.CallTo(() => fake.Cache.Add(item, timeout)).MustHaveHappened()`), `ForwardANullTimeoutWhenNoneIsGiven` |
| `AddListTests` | `ForwardTheTimeoutToTheInnerCache`, `ForwardANullTimeoutWhenNoneIsGiven` |
| `VerifyAddTests` | `PassForAnItemAddedWithATimeout`, `PassForAnyItemAddedWithATimeout` (null argument), `PassForTheMatchingTimeout` (new overload), `FailForADifferentTimeout` (new overload), `FailWhenNothingWasAdded` |
| `VerifyAddListTests` | the same five, for lists |
| `VerifyDidNotAddTests` | `PassWhenNothingWasAdded`, `FailWhenTheItemWasAddedWithATimeout` (proves the widened matcher) |
| `VerifyDidNotAddListTests` | the same two, for lists |

Quote the red: the forwarding facts, the timeout-overload facts (compile failure counts) and `FailWhenTheItemWasAddedWithATimeout` must fail before the change.

## Consumer check (before committing)

Search Fog for code this change could break:

```bash
grep -rn "\.Cache\.Add(" --include=*.cs /c/Code/Fog | grep -v "/bin/\|/obj/"
```

At planning time this returned **no hits**. List the result in the phase report. If there are hits, list each one, and say whether its production code passes a timeout (which would make the assertion fail after this change). **Do not edit Fog.**

## Touches

- `src/ToolKit/Testing/FakeFatCatCache.cs` (modify)
- `src/Tests.ToolKit/Testing/FakeFatCatCacheSpecs/*.cs` (create)
- Exclusive runtime resources: none. Reads `C:\Code\Fog` but never writes to it.

## Verification

```bash
cd src
dotnet build ToolKit.slnx
dotnet test ToolKit.slnx
dotnet format style ToolKit.slnx --verify-no-changes
dotnet format analyzers ToolKit.slnx --verify-no-changes
```

Diff review: only the two `Add` bodies, the four matchers and the two new methods changed in `FakeFatCatCache.cs`.

## Definition of Done

- [ ] Every fact above exists, was red first (quoted), and is now green.
- [ ] Existing `Verify*` signatures unchanged; only their timeout matcher changed.
- [ ] Fog grep result recorded in the report.
- [ ] All Quality Gates in the task spec's "Definition of Done (every phase)", including the review loop.
- [ ] Exactly one commit: `fatcat-cache-cap 02: FakeFatCatCache forwards timeouts (src/task/todo/fatcat-cache-cap/02-fake-timeout-forwarding.md)`. Not pushed.
- [ ] Phase report written.

## Rollback Procedure

`git revert <phase-02 commit>`, after reverting 05 and 04 if they exist. No manual steps.

## Hand-off

- `FakeFatCatCache<T>.Add(...)` now forwards the timeout to `Cache`.
- New public overloads: `VerifyAdd(T expectedItem, TimeSpan? expectedTimeout)` and `VerifyAddList(List<T> expectedItems, TimeSpan? expectedTimeout)`. Phase 04 documents them if the README lists testing helpers.
- Test scaffolding: abstract `Tests.FatCat.Toolkit.Testing.FakeFatCatCacheSpecs.FakeFatCatCacheTests`.
