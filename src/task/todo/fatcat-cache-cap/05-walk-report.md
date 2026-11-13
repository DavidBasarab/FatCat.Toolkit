# fatcat-cache-cap — Phase 05 walk report (final verification)

**Date:** 2026-11-13. **Branch:** `upgrade-net11-slnx-packages`. **Plan base:** `71e2a73`. **Tip before this phase:** `350dfbb`.
Phase commits: 02 `29c7d5e`, 01 `26d2c6c`, 03 `2d06fb2`, 04 `350dfbb`.

This report is the evidence the owner reads before bumping `VersionPrefix` and publishing. Releasing is not part of this plan.

## 1. Full suite, twice (no filter, no manual step between)

| Step | Result |
|---|---|
| `dotnet build ToolKit.slnx --no-incremental` | 0 errors, **2 warnings**: both `BL0016` in `OneOffBlazor/WebCryptoAesGcm.cs` (14,24) and (30,29). These are the baseline, not from this plan. |
| `dotnet test ToolKit.slnx` run 1 | **Passed: 523, Failed: 0, Skipped: 0** (Total 523) |
| `dotnet test ToolKit.slnx` run 2 | **Passed: 523, Failed: 0, Skipped: 0** (Total 523) |
| `dotnet format style ToolKit.slnx --verify-no-changes` | exit 0 |
| `dotnet format analyzers ToolKit.slnx --verify-no-changes` | exit 2, caused only by the baseline `BL0016` pair in `OneOffBlazor`. The same command restricted with `--include` to every file this phase touches exits 0. |

Test counts: 511 before this phase, **12 new**, **523 total**, 523 passing. `ConcurrencyTests` passed in both runs. These are the final runs, made after the last review-loop fix with no change in between. Earlier runs in the same loop were 517/517 twice and 521/521 twice.

## 2. Hand-run walk (`OneOffToolkitOnly`)

`CacheCapSampleModule` registers `FatCatCacheOptions<CappedSampleItem> { MaximumItems = 1_000 }`. `CacheCapWorker` is resolved from `SystemScope`. It adds 10,000 distinct keys to `IFatCatCache<CappedSampleItem>` and to the uncapped `IFatCatCache<UncappedSampleItem>`, then prints both `GetAll().Count` values and the `MaximumItems` of the options instance the container resolved. `Program.cs` routes to it only when the first argument is `cache-cap` (ignoring case); with any other arguments the sample behaves as before.

Command: `dotnet run --project src/OneOffToolkitOnly -- cache-cap` (final run, after the last fix)

```
2026-11-13 17:03:31:239 | SystemScope.cs @ 53 Initialize |     Using assembly Testably.Abstractions.FileSystem.Interface, Version=11.0.0.0, Culture=neutral, PublicKeyToken=f24346c8579fcb48
2026-11-13 17:03:31:251 | SystemScope.cs @ 53 Initialize |     Using assembly OneOffToolkitOnly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
2026-11-13 17:03:31:251 | SystemScope.cs @ 53 Initialize |     Using assembly FatCat.Toolkit, Version=1.0.356.0, Culture=neutral, PublicKeyToken=1916db74250bf654
2026-11-13 17:03:31:303 | SystemScope.cs @ 60 Initialize | Setting lifetime scope
2026-11-13 17:03:31:512 | CacheCapWorker.cs @ 18 DoWork | Added 10000 distinct items to each cache
2026-11-13 17:03:31:513 | CacheCapWorker.cs @ 19 DoWork | Capped cache (registered MaximumItems = 1000) count := <1000>
2026-11-13 17:03:31:517 | CacheCapWorker.cs @ 22 DoWork | Uncapped cache count := <10000>
```

The walk was run three times during the review loop (16:53, 17:00, 17:03), and each run printed capped `1000`, uncapped `10000`.

**Expected:** capped `<= 1000` and `>= 900`, uncapped `10000`. **Actual:** capped `1000`, uncapped `10000`. **Matches.**
(Arithmetic check: the cap fills at 1,000, and each trim goes down to the low-water mark of 900 and then adds 1. The 9,000 adds after the first 1,000 make 90 full cycles of 100, so the cache ends at exactly 1,000.)

## 3. Fog consumer check (`C:\Code\Fog`, read only, at `2ae15beab`)

Nothing in Fog was edited. Fog's working tree already had two modified `appsettings.json` files before this check; this phase did not touch them.

| Check | Result |
|---|---|
| `new FatCatCache<` constructions | 2, both in tests: `Brume/Tests.Brume/Users/DesktopLogin/DesktopNonceServiceSpecs/DesktopNonceServiceTests.cs:22` (`new FatCatCache<DesktopNonce>()`) and `Brume/Tests.Brume/Users/PassKeys/Reset/PasskeyResetTokensSpecs/PasskeyResetTokensTests.cs:21` (`new FatCatCache<PasskeyResetCacheItem>()`). The parameterless constructor exists (`ConstructorTests.StoreItemsWhenBuiltWithTheParameterlessConstructor`), so **both compile unchanged**. |
| `FakeFatCatCache` usage | 19 files. `Verify*Add*` calls: `GetBlockByIdMessageProcessorTests.cs:52` `VerifyAdd(expectedCacheItem)`, `ReadyForBlockRetrievalMessageProcessorTests.cs:44` `VerifyAdd(new RetrievalBlockObject {…})`, `HazeConnectionMessageProcessorTests.cs:36` `VerifyAdd(expectedCacheItem)`. All use the single-argument overload. With the matcher widened to `A<TimeSpan?>._`, they pass whatever timeout the code uses (`VerifyAddTests.PassForAnItemAddedWithATimeout`, `PassForAnItemAddedWithoutATimeout`). **No change needed.** |
| `.Cache.Add(` assertions | **None** (`grep -rn "\.Cache\.Add(" --include=*.cs`). No R9 hits to list. |
| Fog's own registration | `Common/Common/Infrastructure/CommonModule.cs:89`: `builder.RegisterGeneric(typeof(FatCatCache<>)).As(typeof(IFatCatCache<>)).SingleInstance();`. This is the same shape as `ToolkitModule`, which is proven by `ResolveFromContainerTests` (`ResolveAnUnboundedCacheWithoutOptions`, `HonourRegisteredOptions`, `LeaveOtherTypesUnbounded`, `UseTheContainerClockWithoutOptions`, `ResolveAnUnboundedCacheWhenScannedLikeSystemScope`). **Resolves.** |
| `IDateTimeUtilities` registrations and lifetimes | **No explicit registration anywhere in Fog** (no `RegisterType`/`RegisterInstance`/`.As<IDateTimeUtilities>`, no MS DI `Add*<…DateTime…>`, no other class implementing `IDateTimeUtilities`). Fog gets the toolkit's `DateTimeUtilities` from `SystemScope`'s assembly scan, with Autofac's default `InstancePerDependency` lifetime. Fog has **no** `InstancePerRequest` / `InstancePerMatchingLifetimeScope` / `InstancePerLifetimeScope` registrations at all, so the `SingleInstance` cache resolves its clock. `BrumeModule.cs` lines 184, 232, 435 and 707 call `systemScope.Resolve<IDateTimeUtilities>()`; these are consumers, not registrations. |
| Production `Add(List<T>, timeout)` calls | **None.** I listed every `IFatCatCache<…>` field or parameter in 48 production files and every `.Add(` on it (27 calls). All pass a single item; none passes a `List<T>`. So no Fog production behaviour changes from the R4 list-timeout fix. Nothing for CG-01 to review on this point. |
| Toolkit versions Fog references | `FatCat.Toolkit` 1.0.354 (4 projects) and 1.0.240 (2 projects); `FatCat.Toolkit.WebServer` 1.0.354 (1) and 1.0.64 (1). Unchanged until Fog upgrades. |

The optional "pack and build Fog against the local package" step was **not run**, because it needs human approval and none was given.

## 4. README against the code

Each claim in the README `## Caching` and `### Bounded Caches` sections was checked against `src/ToolKit/Caching/FatCatCache.cs` and `FatCatCacheOptions.cs`:

| Claim | Code | Verdict |
|---|---|---|
| `Get` returns null if expired or not found | `FindLiveEntry` | OK |
| `ToolkitModule` registers one shared instance per `T`; `new FatCatCache<T>()` works | `ToolkitModule` `SingleInstance`; parameterless constructor | OK |
| Register `FatCatCacheOptions<T>` in your own module; "nothing else changes" | Autofac picks the `(IDateTimeUtilities, FatCatCacheOptions<T>)` constructor only when **both** resolve and the registering module is loaded | **Drift, fixed.** Added: the cap applies only when the container loads the module that registers the options (with `SystemScope`, its assembly must be passed to `Initialize`) and can resolve `IDateTimeUtilities`. Otherwise the cache stays unbounded. Confirmed with a throwaway scratch program outside the repo (`ToolkitModule` plus options, no clock): 50 adds against `MaximumItems = 10` kept 50. |
| Expired entries go first, then oldest by entry time, down to about 90% | `MakeRoom` → `SweepNow` → `EvictOldest` to `MaximumItems - max(1, MaximumItems / 10)`. `EntryTime` is reset on every update. Ties keep `ConcurrentDictionary` enumeration order. | **Clarified:** "entry time (when the key was added or last updated; entries with the same entry time go in no particular order)". |
| Updating an existing key never evicts | `NeedsRoomFor` excludes `IsUpdate` | OK |
| Concurrent overshoot "briefly", at most the number of concurrent writers | Eviction runs only on a new-key add (`MaximumItems` XML doc). Expiry, `Remove` and `Clear` still remove entries. | **Drift, fixed:** "Eviction runs only when a new key is added, so that excess stays until then unless entries expire or are removed." |
| Expired entries "physically removed when read, or by a sweep at most once per `SweepInterval` (default 30 s)" | `Get`/`InCache` remove the expired entry they read. `GetAll` filters but does not remove. The throttled sweep is lazy: it runs in `Add`/`Get`/`GetAll`/`InCache`/`Remove` once the interval has passed, and `lastSweepTicks` starts at 0, so a new cache sweeps on its first call. A full capped cache sweeps immediately. `Clear` does not sweep. | **Drift, fixed:** now names `Get`/`InCache`, the calls that trigger the sweep, the first-call sweep, the full-cache sweep, and "no background thread". |
| Default sweep 30 s | `SweepInterval = 30.Seconds()` | OK |

The code was not changed. The final README sentences were reviewed by three `code-review` passes. The third pass left one edge case, declined in section 7.

## 5. Phase 02's deferred test hardening (ADR-15)

Twelve facts were added: the six the phase file asks for, plus six that `code-review` asked for so all four `Verify*` helpers are covered symmetrically (see the deviation log). `src/ToolKit/Testing/FakeFatCatCache.cs` is unchanged.

| Fact | Pins | Source |
|---|---|---|
| `VerifyAddTests.PassForAnItemAddedWithoutATimeout` | `Add(item)` then `VerifyAdd(item)` passes | phase file |
| `VerifyAddTests.PassForAnyItemAddedWithoutATimeout` | `Add(item)` then `VerifyAdd()` passes | phase file |
| `VerifyAddListTests.PassForAListAddedWithoutATimeout` | `Add(items)` then `VerifyAddList(items)` passes | phase file |
| `VerifyAddListTests.PassForAnyListAddedWithoutATimeout` | `Add(items)` then `VerifyAddList()` passes | phase file |
| `VerifyDidNotAddTests.FailWhenTheItemWasAddedWithoutATimeout` | `Add(item)` then `VerifyDidNotAdd(item)` throws `ExpectationException` | phase file |
| `VerifyAddTests.FailWhenOnlyAListWasAdded` | `Add(items)` then `VerifyAdd()` throws | phase file |
| `VerifyAddListTests.FailWhenOnlyAnItemWasAdded` | `Add(item)` then `VerifyAddList()` throws | code-review #1 |
| `VerifyDidNotAddTests.FailWhenAnyItemWasAddedWithoutATimeout` | `Add(item)` then `VerifyDidNotAdd()` throws | code-review #1 |
| `VerifyDidNotAddListTests.FailWhenTheListWasAddedWithoutATimeout` | `Add(items)` then `VerifyDidNotAddList(items)` throws | code-review #1 |
| `VerifyDidNotAddListTests.FailWhenAnyListWasAddedWithoutATimeout` | `Add(items)` then `VerifyDidNotAddList()` throws | code-review #1 |
| `VerifyDidNotAddTests.PassWhenOnlyAListWasAdded` | `Add(items)` then `VerifyDidNotAdd()` passes | code-review #2 |
| `VerifyDidNotAddListTests.PassWhenOnlyAnItemWasAdded` | `Add(item)` then `VerifyDidNotAddList()` passes | code-review #2 |

These pin existing behaviour, so they passed on first run. Instead of a red-first step, each one was **proven able to fail** with a temporary mutation of `FakeFatCatCache.cs`. After every mutation the file was restored with `git checkout`, and `git diff --quiet` confirmed it:

- **Mutation A:** every `A<TimeSpan?>._` in `VerifyAdd` / `VerifyAddList` / `VerifyDidNotAdd` / `VerifyDidNotAddList` became `A<TimeSpan?>.That.Not.IsNull()`, so a timeout was required. Across `FakeFatCatCacheSpecs` the result was `Failed: 8, Passed: 38` (46 facts at the time). The 8 failures are exactly the 8 `…WithoutATimeout` facts above. The `Pass…` facts threw `ExpectationException` (expected `timeout: <not NULL>`, found `timeout: NULL`). The `Fail…` facts reported "should throw ExpectationException but no exception was thrown". The first run of this mutation, with only the first 5 facts present, gave `Failed: 5`.
- **Mutation B:** the no-argument branch of `VerifyAdd()` became `A.CallTo(Cache).Where(call => call.Method.Name == nameof(Cache.Add)).MustHaveHappened();`. Result: `Failed: 1, Passed: 41`, and the only failure was `VerifyAddTests.FailWhenOnlyAListWasAdded`.
- **Mutation C:** the same change in the no-argument branch of `VerifyAddList()`. Result: `Failed: 1, Passed: 45`, and the only failure was `VerifyAddListTests.FailWhenOnlyAnItemWasAdded`.
- **Mutation D:** the no-argument branch of `VerifyDidNotAdd()` became `A.CallTo(Cache).Where(call => call.Method.Name == nameof(Cache.Add)).MustNotHaveHappened();`. Result: `Failed: 1, Passed: 47`, and the only failure was `VerifyDidNotAddTests.PassWhenOnlyAListWasAdded`.
- **Mutation E:** the same change in the no-argument branch of `VerifyDidNotAddList()`. Result: `Failed: 1, Passed: 47`, and the only failure was `VerifyDidNotAddListTests.PassWhenOnlyAnItemWasAdded`.

With the file restored, all 48 `FakeFatCatCacheSpecs` facts pass, and the full suite passes (section 1).

## 6. Acceptance criteria roll-up

Test classes are under `src/Tests.ToolKit/Caching/FatCatCacheSpecs/` unless another path is given.

**Defaults and opt-in**

- [x] No options, 10× a test cap retained: `AddTests.RetainEveryItemWhenUnbounded`, `CapacityTests.RetainEveryItemWhenUnbounded`, `ResolveFromContainerTests.ResolveAnUnboundedCacheWithoutOptions` (500 items). The walk's uncapped cache kept 10,000.
- [x] `MaximumItems` 0 or negative, or `SweepInterval <= 0`, throws `ArgumentOutOfRangeException` naming the property: `ConstructorTests.ThrowWhenMaximumItemsIsZero`, `ThrowWhenMaximumItemsIsNegative`, `ThrowWhenSweepIntervalIsZero`, `ThrowWhenSweepIntervalIsNegative`, `NameMaximumItemsInTheException`, `NameSweepIntervalInTheException`.
- [x] Container with `ToolkitModule` and `IDateTimeUtilities` but no options gives an unbounded cache on the container clock: `ResolveFromContainerTests.ResolveAnUnboundedCacheWithoutOptions`, `UseTheContainerClockWithoutOptions`, `ResolveAnUnboundedCacheWhenScannedLikeSystemScope`.
- [x] The same container plus `FatCatCacheOptions<Y>` honours the cap for Y and leaves X unbounded: `ResolveFromContainerTests.HonourRegisteredOptions`, `LeaveOtherTypesUnbounded`. Walk: capped 1,000, uncapped 10,000 through `SystemScope`.
- [x] `new FatCatCache<T>()` compiles and works: `ConstructorTests.StoreItemsWhenBuiltWithTheParameterlessConstructor`. Fog's two direct constructions compile unchanged (section 3).

**Cap and eviction**

- [x] Capped at N, one add at a time, the count never exceeds N: `CapacityTests.NeverExceedTheCapWhenAddingOneAtATime`. Walk: 1,000 after 10,000 adds.
- [x] When full, the oldest entries are evicted first down to `N - max(1, N/10)` and the new key is present: `CapacityTests.TrimToTheLowWaterMark`, `FreeAtLeastOneSlotForASmallCap`, `KeepTheNewestItem`, `EvictionOrderTests.EvictTheOldestEntryFirst`, `KeepTheMostRecentlyUpdatedEntry`.
- [x] Updating an existing key evicts nothing and refreshes its entry time: `CapacityTests.NotEvictWhenUpdatingAnExistingKey`, `KeepTheOldestEntryWhenUpdatingAnExistingKey`, `AddTests.RestartTheTimeoutWhenTheItemIsReplaced`, `EvictionOrderTests.KeepTheMostRecentlyUpdatedEntry`.
- [x] Expired entries are removed before any unexpired eviction, and no eviction happens if that frees room: `EvictionOrderTests.RemoveExpiredEntriesBeforeEvicting`, `KeepEveryUnexpiredEntryWhenExpiryFreesRoom`, `EvictUnexpiredEntriesOnlyWhenExpiryFreesNoRoom`.

**Expiry and sweep (fake clock)**

- [x] Past the timeout, `Get` returns null, `InCache` returns false, and the entry is gone: `GetTests.ReturnNullOnceExpired`, `InCacheTests.ReturnFalseOnceExpired`, `GetTests.RemoveTheExpiredEntry`, `InCacheTests.RemoveTheExpiredEntry`.
- [x] At exactly the timeout the item is still returned: `GetTests.ReturnTheItemAtExactlyTheTimeout`, `InCacheTests.ReturnTrueAtExactlyTheTimeout`.
- [x] With an expired entry A and the interval not elapsed, `Get(B)` leaves A stored: `SweepTests.LeaveOtherExpiredEntriesStoredBeforeTheSweepInterval` (`StoredCount`), plus `SweepAtMostOncePerInterval` and `KeepTheThrottleWhenACallReadTheClockBeforeTheLastSweep`.
- [x] Once the interval has elapsed, any public call removes all expired entries: `SweepTests.RemoveEveryExpiredEntryOnceTheSweepIntervalHasPassed`, `SweepOnceExactlyTheSweepIntervalHasPassed`, `SweepAgainOnceTheNextIntervalHasPassed`, `SweepWhenAnItemIsAdded`, `SweepWhenAllItemsAreRead`, `SweepWhenAnIdIsCheckedInTheCache`, `SweepWhenAnIdIsRemoved`.
- [x] `GetAll` never returns an expired item: `GetAllTests.ExcludeExpiredItemsBeforeTheSweepIsDue`, `ReturnEveryItem`.
- [x] `Add(List<T>, timeout)` applies the timeout to every item: `AddListTests.ExpireEveryItemAfterTheTimeout`, `KeepEveryItemBeforeTheTimeout`, `StoreEveryItem`.
- [x] `Remove` removes and `Clear` empties: `RemoveTests.RemoveTheItem`, `RemoveThroughTheItemOverload`, `IgnoreAnUnknownId`, `ClearTests.EmptyTheCache`.

**Concurrency (ADR-6)**

- [x] Parallel workers add distinct keys and call `Get` / `InCache` with no exception and a final count `<= N + workerCount`: `ConcurrencyTests.NeverThrow`, `StayWithinTheCapPlusConcurrentWriters`. Green in both full-suite runs.

**Fake** (`src/Tests.ToolKit/Testing/FakeFatCatCacheSpecs/`)

- [x] `Add(item, timeout)` and `Add(list, timeout)` forward the timeout: `AddTests.ForwardTheTimeoutToTheInnerCache`, `ForwardANullTimeoutWhenNoneIsGiven`, `AddListTests.ForwardTheTimeoutToTheInnerCache`, `ForwardANullTimeoutWhenNoneIsGiven`.
- [x] The existing `Verify*` methods pass whatever timeout was used:
  - `VerifyAddTests.PassForAnItemAddedWithATimeout`, `PassForAnItemAddedWithoutATimeout`*, `PassForAnyItemAddedWithATimeout`, `PassForAnyItemAddedWithoutATimeout`*
  - `VerifyAddListTests.PassForAListAddedWithATimeout`, `PassForAListAddedWithoutATimeout`*, `PassForAnyListAddedWithATimeout`, `PassForAnyListAddedWithoutATimeout`*
  - `VerifyDidNotAddTests.FailWhenTheItemWasAddedWithATimeout`, `FailWhenTheItemWasAddedWithoutATimeout`*, `FailWhenAnyItemWasAddedWithATimeout`, `FailWhenAnyItemWasAddedWithoutATimeout`*
  - `VerifyDidNotAddListTests.FailWhenTheListWasAddedWithATimeout`, `FailWhenTheListWasAddedWithoutATimeout`*, `FailWhenAnyListWasAddedWithATimeout`, `FailWhenAnyListWasAddedWithoutATimeout`*
  - Overload separation*: `VerifyAddTests.FailWhenOnlyAListWasAdded`, `VerifyAddListTests.FailWhenOnlyAnItemWasAdded`, `VerifyDidNotAddTests.PassWhenOnlyAListWasAdded`, `VerifyDidNotAddListTests.PassWhenOnlyAnItemWasAdded`

  (* = added in this phase.)
- [x] The new `VerifyAdd(item, timeout)` / `VerifyAddList(items, timeout)` pass on a matching timeout and fail on a different one: `VerifyAddTests.PassForTheMatchingTimeout`, `FailForADifferentTimeout`, `FailForANullTimeoutWhenOneWasGiven`, `FailForATimeoutWhenNoneWasGiven`, `PassForANullTimeoutWhenNoneWasGiven`. `VerifyAddListTests` has the same set.

**Quality**

- [x] `IFatCatCache<T>` member list byte-for-byte unchanged: the interface block extracted from `71e2a73` and from `HEAD` with `awk '/^public interface IFatCatCache/,/^}/'` and compared with `diff` is identical (27 lines). `#nullable enable` is still the first line. `ToolkitModule.cs` has an empty diff since `71e2a73`.
- [x] `FatCatCache<T>` no longer references `DateTime.UtcNow` / `DateTime.Now`: `grep -nE "DateTime\.(UtcNow|Now)" src/ToolKit/Caching/FatCatCache.cs` finds no match.
- [x] Test-first and `unit-test-review` PASS. Evidence: phases 01–03 were accepted by the orchestrator against a Definition of Done that requires red-before-green and a final `Unit test review: PASS`. `failure-report.md` records the phase 01 retry passing. This phase did not re-verify the red step: each phase is one commit, so git history cannot show it separately. Phase 05 adds no production code; its twelve facts were each proven able to fail by mutation (section 5), and its `unit-test-review` ended in `PASS`.
- [x] No `VersionPrefix` change: `git diff 71e2a73..HEAD -- '*.csproj' | grep VersionPrefix` returns nothing (exit 1). The same check including the working tree also returns nothing. `ToolKit.csproj` stays at 1.0.356.
- [x] E2E walk: (1) two consecutive green unfiltered runs (section 1); (2) capped and uncapped caches resolved through `SystemScope` in `OneOffToolkitOnly` (section 2); (3) Fog grep: no `FatCatCache` construction or `FakeFatCatCache` assertion needs a change, and there are no `.Cache.Add(x, default)` hits (section 3).

**Unmet criteria:** none.

## 7. Review loop (this phase)

The loop ran `unit-test-review` → `code-review` (medium) → `code-security-review` (screen only, ADR-13). Each fix sent it back to `unit-test-review`. Two self-corrections were used, which is the cap.

| Pass | Result | Action |
|---|---|---|
| `unit-test-review` #1 | PASS | — |
| `code-review` #1 | 8 findings | **Correction 1.** Fixed: README sweep wording; README tie order; the worker prints the resolved options' `MaximumItems` instead of a module const; 4 symmetric facts; `[ExcludeFromCodeCoverage(Justification)]` on the module; `IsCacheCapWalk` renamed to `IsCacheCapSampleRequested`. **Declined:** (1) the silent fallback to unbounded without `IDateTimeUtilities` needs a production-code change in `FatCatCache.cs`, which this phase forbids ("if the code is wrong, stop and report"). It is pre-existing phase 01 design, now documented in the README and raised in section 8. (7) An updated key evicted mid-update re-enters without making room. This is pre-existing phase 03 behaviour, and the documented bound still holds because it is one in-flight writer. |
| `unit-test-review` #2 | PASS | — |
| `code-review` #2 | 7 low findings, no correctness bugs | **Correction 2.** Fixed: README "entry time" now says "added or last updated"; the excess wording now covers expiry and removal; the precondition now includes "the module must be loaded"; 2 `PassWhenOnly…` facts; the sample argument is a const with an ordinal ignore-case compare. **Declined:** `Pass*` facts that assert through the fake's own `Verify*` (`MustHaveHappened`), which is the established phase 02 pattern and the API under test; `RegisterInstance` without `.As<>()` (the options type has no interface, and this is the README's documented consumer pattern). |
| `unit-test-review` #3 | PASS | — |
| `code-review` #3 | 3 low notes, no correctness bugs or blocking rule violations | **Declined, because the correction cap was reached and none blocks:** (a) "a new cache sweeps on its first call" is false only when the clock reads within one `SweepInterval` of `DateTime.MinValue` (for example an unconfigured fake), and a brand-new cache has nothing to sweep anyway; (b) `[UsedImplicitly]` on `CacheCapSampleModule` would need a JetBrains.Annotations package reference in the sample `.csproj`, which is outside `## Touches`, and the sibling sample modules (`OneOffModule`, `SampleDockerModule`) omit it too; (c) the label says "registered MaximumItems", which is accurate, and the printed count is the evidence the cap applied. |
| `code-security-review` (full tree, screen only) | **No new findings.** It confirmed no code path grows a capped cache beyond `MaximumItems` plus concurrent writers (`Add` has the only write, one entry per in-flight call; trimming runs under `trimLock` with a re-check). It listed pre-existing findings only: H1, H2, CORS `AllowAnyOrigin` (`ApplicationStartUp.cs:82`, `ToolkitWebApplication.cs:118`), `RequireHttpsMetadata = false` (`ApplicationStartUp.cs:137`), the committed `ToolKit/FatCatToolKit.snk`, M1–M4, L1–L10. | — |

After the last correction, the gate was re-run with no change in between: build (2 baseline warnings), walk, `dotnet test` ×2 (523/523), `dotnet format style` (exit 0), and `dotnet format analyzers` (baseline `BL0016` only; this phase's files exit 0).

## 8. Risks for the owner

- **A registered cap is silently ignored without `IDateTimeUtilities`, or when the registering module is not loaded.** This is fail-open for a DoS mitigation. In a container that registers `FatCatCacheOptions<T>` but cannot resolve `IDateTimeUtilities`, Autofac selects the parameterless constructor and the cache is unbounded, with no error. A module in an assembly not passed to `SystemScope.Initialize` never registers its options. A future toolkit task could make this fail loudly, for example with a `FatCatCache(FatCatCacheOptions<T>)` constructor that defaults the clock. `SystemScope` always registers the clock, and Fog uses `SystemScope`, so Fog is not affected. The README now says this. No test covers the fallback; the scratch check in section 4 confirmed the behaviour.
- **Backward wall clock** (phase 03 note, not in the XML doc): eviction orders by `EntryTime`, so after a backward step it removes the newest-stamped entries first until the clock catches up. Sweeps pause until the clock passes the last sweep again (ADR-14).
- `Count` takes every stripe lock on each capped new-key add (ADR-11, accepted).
- `VerifyAdd` / `VerifyAddList` now have two overloads each, so a consumer looking them up by name through reflection would hit an `AmbiguousMatchException` (phase 02 note).
- Pre-existing security findings are not addressed by this plan (ADR-13): H1, H2, CORS `AllowAnyOrigin`, `RequireHttpsMetadata = false`, the committed `.snk`, M1–M4, L1–L10.
- **Update/eviction race** (code-review #1): a key classed as an update can be evicted by a concurrent trim before its write lands, and it then re-enters without making room. This stays within the documented `MaximumItems + writers` bound.
- `VerifyDidNotAddList` / `VerifyDidNotAdd` and the other `Pass*` facts assert through the helper's own `MustHaveHappened` / `MustNotHaveHappened`, as phase 02 established.

## 9. Deviation log (phase 05)

1. **Six facts beyond the phase file**, from code-review #1 and #2: `VerifyAddListTests.FailWhenOnlyAnItemWasAdded`, `VerifyDidNotAddTests.FailWhenAnyItemWasAddedWithoutATimeout` and `PassWhenOnlyAListWasAdded`, `VerifyDidNotAddListTests.FailWhenTheListWasAddedWithoutATimeout`, `FailWhenAnyListWasAddedWithoutATimeout` and `PassWhenOnlyAnItemWasAdded`. These make all four `Verify*` helpers symmetric.
2. **Footprint miss:** `src/Tests.ToolKit/Testing/FakeFatCatCacheSpecs/VerifyDidNotAddListTests.cs` was modified. It is not listed in `## Touches`. This phase runs alone, so no parallel sibling is affected.
3. **README additions beyond fixing drift:** the "cap needs the module loaded and `IDateTimeUtilities`" precondition and the tie-order note. Both describe existing code behaviour; no code changed.
4. **Sample additions:** `CacheCapWorker` injects `FatCatCacheOptions<CappedSampleItem>` to print the resolved cap. `CacheCapSampleModule` carries `[ExcludeFromCodeCoverage]`. `Program.cs` matches `cache-cap` ignoring case. All three come from code-review.
5. **Red-first replaced by mutation evidence** for the step 5 facts, as the phase file directs (section 5).
6. **A scratch program outside the repo** (in the session scratchpad) checked the no-clock fallback. Nothing was added to the repo for it.
