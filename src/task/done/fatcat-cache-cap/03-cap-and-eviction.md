# Phase 03 — Enforce `MaximumItems`: batch trim, container resolution, concurrency

**Risk: HIGH, flagged for extra human review.** This changes the runtime behaviour of a published cache, adds concurrency-sensitive code to a `SingleInstance` shared by multi-threaded hosts, and is the actual denial-of-service mitigation Fog depends on.

- **Depends on:** 01.
- **Depended on by:** 05. Reverting this phase means reverting 05 first.
- **Wave:** 2.
- **Parallel with:** 04. Disjoint write sets: this phase writes `src/ToolKit/Caching/**` and `src/Tests.ToolKit/Caching/**`; 04 writes only the root `README.md`. This phase edits no hotspot.

## Read first

1. `src/task/todo/fatcat-cache-cap.md`: **R1** (constructor selection and container proof) and **R2** (eviction).
2. `src/task/todo/fatcat-cache-cap/00-overview.md`: ADR-1, 2, 6, 10 and **11**.
3. `src/CLAUDE.md` and `src/.claude/rules/csharp/*`.
4. The phase-01 result: `src/ToolKit/Caching/FatCatCache.cs`, `FatCatCacheOptions.cs`, `src/Tests.ToolKit/Caching/FatCatCacheSpecs/FatCatCacheTests.cs`.
5. `src/ToolKit/Injection/SystemScope.cs` (`BuildContainer`) and `src/ToolKit/Web/Api/SignalR/ToolkitModule.cs`. Prior art for a container test: `src/Tests.ToolKit/WebServer/SignalR/ToolkitHubTests.cs`.

## Goal

1. Adding a **new key** to a capped cache with `StoredCount >= MaximumItems`:
   - sweeps expired entries (ignoring the throttle);
   - if still full, evicts the oldest entries by `EntryTime` down to `MaximumItems - max(1, MaximumItems / 10)`;
   - then adds the new key.
2. Updating an **existing key** never evicts.
3. An unbounded cache (`MaximumItems == null`) never evicts.
4. Tests prove that Autofac resolves the right constructor with and without a registered `FatCatCacheOptions<T>`.
5. One real-thread `ConcurrencyTests` class (ADR-6).

## Tests first (red before green)

All tests go in `src/Tests.ToolKit/Caching/FatCatCacheSpecs/`, deriving from `FatCatCacheTests`, unless noted. Set `options.MaximumItems` in the derived constructor before touching `Cache`.

| Class | Facts |
|---|---|
| `CapacityTests` | `NeverExceedTheCapWhenAddingOneAtATime` (cap 50, add 500 distinct; `StoredCount <= 50`), `KeepTheNewestItem` (the last added is present), `TrimToTheLowWaterMark` (cap 100: when full, adding one new key leaves `StoredCount == 100 - 10 + 1`), `FreeAtLeastOneSlotForASmallCap` (cap 1: second add leaves only the second item), `NotEvictWhenUpdatingAnExistingKey` (full cache; re-add an existing id; `StoredCount` unchanged and the oldest still present), `RetainEveryItemWhenUnbounded` (no cap; add 10× a typical cap) |
| `EvictionOrderTests` | `EvictTheOldestEntryFirst` (advance the clock between adds; the first-added is gone), `KeepTheMostRecentlyUpdatedEntry` (update the oldest key so its `EntryTime` refreshes; the next trim evicts a different key), `RemoveExpiredEntriesBeforeEvicting` (full cache with some expired; a new add removes the expired ones and evicts **no** unexpired entry when that frees room), `EvictUnexpiredEntriesOnlyWhenExpiryFreesNoRoom` |
| `ResolveFromContainerTests` | see below |
| `ConcurrencyTests` | see below |

Also create `src/Tests.ToolKit/Caching/SecondTestCacheItem.cs` (namespace `Tests.FatCat.Toolkit.Caching`, the same shape as `TestCacheItem`) for the two-type container test.

**`ResolveFromContainerTests`**: build a fresh `ContainerBuilder` in each test; never touch the static `SystemScope`. For example `builder.RegisterModule<ToolkitModule>()` plus `builder.RegisterInstance(dateTimeUtilities).As<IDateTimeUtilities>()`, using the base's fake clock.

- `ResolveAnUnboundedCacheWithoutOptions`: add more items than any test cap; all are retained.
- `UseTheContainerClockWithoutOptions`: add with a timeout, advance the fake `now` past it, `Get` returns null. This proves the `(IDateTimeUtilities)` constructor was chosen.
- `HonourRegisteredOptions`: register `new FatCatCacheOptions<TestCacheItem> { MaximumItems = 10 }`; resolve; add 50; `StoredCount <= 10`. `StoredCount` is reachable on the resolved instance via a cast to `FatCatCache<TestCacheItem>`.
- `LeaveOtherTypesUnbounded`: options registered only for `TestCacheItem`; `IFatCatCache<SecondTestCacheItem>` retains every item.
- `ResolveAnUnboundedCacheWhenScannedLikeSystemScope`: mirror `SystemScope.BuildContainer`'s chain against the ToolKit assembly (`RegisterAssemblyModules`, then `RegisterAssemblyTypes(...).AsImplementedInterfaces().HasPublicConstructor().PublicOnly().PreserveExistingDefaults().AsSelf()`, excluding `ISystemScope`). Resolving `IFatCatCache<TestCacheItem>` succeeds and is unbounded. This proves that scanning never auto-registers `FatCatCacheOptions<T>`.

**`ConcurrencyTests`**: the **only** real-thread test in the repo (ADR-6). Put a one-line comment at the top of the class citing the owner-approved exception (ADR-6 of `src/task/todo/fatcat-cache-cap/00-overview.md`).

- Cap 100. 8 workers, fixed via `Parallel.For` with `MaxDegreeOfParallelism = 8`, each adding 1,000 distinct ids and calling `Get` / `InCache` on random ids. Use the base's fake clock (fixed `now`; FakeItEasy fakes are safe to call concurrently). Capture any exception into a `ConcurrentBag<Exception>`.
- `NeverThrow`: the bag is empty.
- `StayWithinTheCapPlusConcurrentWriters`: `StoredCount <= 100 + 8`.

Quote the red: every `CapacityTests` and `EvictionOrderTests` fact, plus `HonourRegisteredOptions`, must fail before the implementation (phase 01 does not enforce the cap).

## Implementation guidance (`src/ToolKit/Caching/FatCatCache.cs` only)

- `private readonly Lock trimLock = new();` (`System.Threading.Lock`).
- In `Add(T, TimeSpan?)`, after `SweepIfDue(now)` and building the entry:
  - if `maximumItems` is null, **or** `cache.ContainsKey(id)` (an update), write and return;
  - otherwise, if `cache.Count >= maximumItems`, call `MakeRoom(now)`; then write.
- `MakeRoom(DateTime now)`, inside `lock (trimLock)`:
  1. Re-check `cache.Count >= maximumItems`; another thread may have trimmed already.
  2. `RemoveExpired(now)` and set `lastSweepTicks` to `now.Ticks` (a forced sweep counts as a sweep).
  3. If still `>= maximumItems`, take `cache.Count - LowWaterMark` entries ordered by `EntryTime`, oldest first, and `TryRemove(pair)` each (the `KeyValuePair` overload).
- `LowWaterMark` is a private computed value: `maximumItems - Math.Max(1, maximumItems / 10)`. Compute it once in a field initializer from the copied `maximumItems`, never from `options` in a method (CS9124).
- Reads (`Get` / `InCache` / `GetAll` / `Remove`) never take the lock.
- Known and accepted: the `ContainsKey`-then-write window lets a concurrently removed key be re-added without a room check, and threads that passed the count check before a trim finished still add. Both are bounded by the number of concurrent writers, as documented on `MaximumItems`.
- Keep each method short and well named (`IsUpdate`, `IsFull`, `MakeRoom`, `EvictOldest`). No comments explaining what code does.

## Touches

- `src/ToolKit/Caching/FatCatCache.cs` (modify)
- `src/ToolKit/Caching/FatCatCacheOptions.cs` (modify, only if the `MaximumItems` XML doc needs to reflect the final behaviour)
- `src/Tests.ToolKit/Caching/SecondTestCacheItem.cs` (create)
- `src/Tests.ToolKit/Caching/FatCatCacheSpecs/{CapacityTests,EvictionOrderTests,ResolveFromContainerTests,ConcurrencyTests}.cs` (create)
- `src/Tests.ToolKit/Caching/FatCatCacheSpecs/FatCatCacheTests.cs` (modify, only for shared helpers such as `FillToCapacity`)
- Exclusive runtime resources: none. `ConcurrencyTests` uses threads, not shared machine resources.

## Verification

```bash
cd src
dotnet build ToolKit.slnx
dotnet test ToolKit.slnx                       # run it 3 times; ConcurrencyTests must be green every time
dotnet test ToolKit.slnx --filter "FullyQualifiedName~FatCatCacheSpecs"
dotnet format style ToolKit.slnx --verify-no-changes
dotnet format analyzers ToolKit.slnx --verify-no-changes
```

Review by hand:

- `IFatCatCache<T>` is still unchanged.
- No new public member.
- No lock on any read path.
- `ToolkitModule.cs` is untouched.

## Definition of Done

- [ ] Every fact above exists, was red first (quoted), and is now green.
- [ ] `ConcurrencyTests` green on three consecutive full runs.
- [ ] `ResolveAnUnboundedCacheWhenScannedLikeSystemScope` passes. If it cannot, **stop**: ADR-1's premise is wrong, and that is a design question for the human.
- [ ] `code-security-review` explicitly confirms that no path grows a capped cache beyond `MaximumItems + concurrent writers`.
- [ ] All Quality Gates in the task spec, including the review loop.
- [ ] Exactly one commit: `fatcat-cache-cap 03: cap and batch eviction (src/task/todo/fatcat-cache-cap/03-cap-and-eviction.md)`. Not pushed.
- [ ] Phase report written.

## Rollback Procedure

`git revert <phase-03 commit>`, after reverting 05 if it exists. Phase 01's state (cap accepted but not enforced) returns. No manual steps.

## Hand-off

- `FatCatCache<T>` enforces `MaximumItems` by batch trim to `MaximumItems - max(1, MaximumItems / 10)`. Expired entries go first, then the oldest by `EntryTime`. Updates never evict.
- Proven: Autofac uses the options constructor only when `FatCatCacheOptions<T>` is registered, and assembly scanning never registers it.
- `SecondTestCacheItem` exists for multi-type tests.
