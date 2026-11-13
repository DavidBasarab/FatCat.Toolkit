# fatcat-cache-cap (FatCat.Toolkit) — Overview

> **Origin:** Fog's Vendor Cost Guards epic, finding **F-20** (`C:\Code\Fog\tasks\planning\fatcat-toolkit-cache-cap-brief.md`). It is a prerequisite: no Fog epic phase starts until a toolkit release contains this change.
>
> **Status:** planned (2026-11-13). Five phases in three waves. Task spec: [`../fatcat-cache-cap.md`](../fatcat-cache-cap.md). Runbook: [`orchestrator.md`](orchestrator.md). Consumer check: [`consumer-compatibility.md`](consumer-compatibility.md).

## Work Item

Make `FatCatCache<T>` safe to use for attacker-growable keys, and fix the related bugs:

1. An **opt-in, per-`T` maximum size** (`FatCatCacheOptions<T>`). The default stays unbounded.
2. **Batch eviction** when a capped cache is full: expired entries go first, then the oldest by entry time, down to a low-water mark.
3. **No full scan per call.** Each read checks only its own key, and the full sweep is throttled to once per `SweepInterval`.
4. `Add(List<T>, timeout)` **applies the timeout** to every item.
5. The racy `timeoutEnabled` flag is **removed**.
6. Time comes from the injected **`IDateTimeUtilities`**.
7. `FakeFatCatCache<T>` **forwards the timeout**, and its `Verify*` methods keep their meaning.

`IFatCatCache<T>` does not change. `ToolkitModule` does not change. No `VersionPrefix` changes.

The full requirement list (R1–R9) and acceptance criteria are in the task spec. **Every phase reads it.**

## Phase index

| # | File | Title | Risk | Wave |
|---|---|---|---|---|
| 01 | [01-cache-foundation.md](01-cache-foundation.md) | Options contract, clock injection, per-key expiry, throttled sweep, list-timeout fix | **High** | 1 |
| 02 | [02-fake-timeout-forwarding.md](02-fake-timeout-forwarding.md) | `FakeFatCatCache<T>` forwards the timeout; widened and new `Verify*` | **High** | 1 |
| 03 | [03-cap-and-eviction.md](03-cap-and-eviction.md) | `MaximumItems` enforcement, batch trim, container resolution, concurrency test | **High** | 2 |
| 04 | [04-readme-caching.md](04-readme-caching.md) | README Caching section | Low | 2 |
| 05 | [05-final-verification.md](05-final-verification.md) | Full suite ×2, OneOff walk, Fog check, README-vs-code reconciliation | Low (gate) | 3 |

## Dependency graph

```
01 cache-foundation ──┬──► 03 cap-and-eviction ──┐
                      │                          ├──► 05 final-verification
                      └──► 04 readme-caching ────┤
02 fake-timeout ──────────► 04 readme-caching    │
02 fake-timeout ─────────────────────────────────┘
```

| Phase | Depends on | Depended on by |
|---|---|---|
| 01 | — | 03, 04, 05 |
| 02 | — | 04, 05 |
| 03 | 01 | 05 |
| 04 | 01, 02 | 05 |
| 05 | 01, 02, 03, 04 | — |

## Waves

| Wave | Phases | Why parallel-safe |
|---|---|---|
| 1 | 01, 02 | No dependency between them. Disjoint write sets: 01 writes `ToolKit/Caching/**`, `ToolKit/ToolKit.csproj`, `Tests.ToolKit/Tests.ToolKit.csproj` and `Tests.ToolKit/Caching/**`; 02 writes `ToolKit/Testing/FakeFatCatCache.cs` and `Tests.ToolKit/Testing/FakeFatCatCacheSpecs/**`. Only 01 edits hotspots (both `.csproj` files). 02's tests compile whether or not the test assembly is signed. 02 *reads* `Tests.FatCat.Toolkit.Caching.TestCacheItem`, which 01 moves to its own file **with its name, namespace and shape unchanged** (ADR-10), so both trees compile and the cherry-pick does not conflict. Neither needs a runtime resource. |
| 2 | 03, 04 | Both depend only on wave-1 phases. Disjoint write sets: 03 writes `ToolKit/Caching/**` and `Tests.ToolKit/Caching/**`; 04 writes only the root `README.md`. Only 04 edits a hotspot (`README.md`). 04 documents the contract fixed by the spec and by 01's `FatCatCacheOptions<T>`, not 03's internals; phase 05 reconciles any drift. |
| 3 | 05 | Runs alone: it is the end-to-end gate and roll-up report. |

**Critical path:** 01 → 03 → 05 (three sequential steps). Wave 1 and wave 2 each save one step.

## Cross-cutting decisions (ADRs)

### ADR-1 — Opt-in through `FatCatCacheOptions<T>`, chosen by Autofac's constructor selection

- **Decision:** a public generic options class the consumer registers per `T` (`builder.RegisterInstance(new FatCatCacheOptions<X> { MaximumItems = … })`). `FatCatCache<T>` has a primary constructor `(IDateTimeUtilities, FatCatCacheOptions<T>)` and two chained secondaries: `(IDateTimeUtilities)` and `()`. Autofac uses the constructor with the most parameters it can resolve, so a registered options instance is used when present and the clock-only constructor is used when not.
- **Context:** the brief forbids consumers having to change their registration, and `ToolkitModule` registers an open generic `SingleInstance`. `SystemScope.BuildContainer` scans assemblies with `RegisterAssemblyTypes`, which skips open generics, so `FatCatCacheOptions<T>` is never registered by accident. **Phase 03 proves this with a test that mirrors the scan chain.**
- **Rejected:** a non-generic `Type → options` map (stringly, and one registration shared by all consumers); a static or attribute on the `ICacheItem` type (puts host policy on data, and cannot vary per host); a separate `BoundedFatCatCache<T>` (consumers would have to change their registrations); a single constructor taking `IEnumerable<FatCatCacheOptions<T>>` (awkward for direct construction and in tests).
- **Accepted wrinkle:** the parameterless constructor does `new DateTimeUtilities()`, which the "no `new` for dependencies" rule normally forbids. The brief requires that path (Fog's tests call `new FatCatCache<T>()`), and it is the only one; DI never takes it while `IDateTimeUtilities` is registered.

### ADR-2 — Batch trim to a low-water mark (owner decision)

- **Decision:** when a new key arrives at a full capped cache: sweep expired entries; if still full, evict the oldest entries by `EntryTime` down to `MaximumItems - max(1, MaximumItems / 10)`. Updating an existing key never evicts.
- **Rejected:** exactly one eviction per add (an O(n) scan on every add when full, which brings back the defect); an insertion-order `ConcurrentQueue` (O(1), but more state, and repeated updates grow the queue unless it is compacted).

### ADR-3 — Lazy, throttled sweep claimed with `Interlocked`

- **Decision:** a `long` holding the ticks of the last sweep, initialised to `0` (so the first call sweeps a near-empty cache, which is harmless; this matches phase 01). At the start of each public call, if `now - lastSweep >= SweepInterval`, `Interlocked.CompareExchange` claims the sweep; only the winning thread walks the dictionary. No timer, no background thread.
- **Rejected:** a timer or `IThread` loop (lifetime and disposal on a `SingleInstance`, and unobserved exceptions on a background path); keeping `timeoutEnabled` (racy).

### ADR-4 — Per-key expiry using `TryRemove(KeyValuePair)`

`Get`, `InCache` and `Remove` read only their key. An expired entry is removed with the `KeyValuePair<string, CacheEntry>` overload of `TryRemove`, so a concurrent re-add of the same key (a different entry instance) survives.

### ADR-5 — Test seam: `InternalsVisibleTo` + internal `StoredCount` (owner decision)

The first `InternalsVisibleTo` in this repo (`Tests.FatCat.Toolkit` only), added in phase 01. `StoredCount` counts physically held entries, including expired entries not yet swept. **Rejected:** a public `Count` (permanent surface); clock-call-count assertions (weak and implementation-coupled).

**Strong-name consequence (owner decision, 2026-11-13):** `FatCat.Toolkit.dll` is strong-name signed and `Tests.FatCat.Toolkit` is not, and a signed assembly cannot grant friend access to an unsigned name (CS1726). Phase 01 therefore:

- signs `Tests.ToolKit.csproj` with the existing `ToolKit/FatCatToolKit.snk` (`SignAssembly` + `AssemblyOriginatorKeyFile`);
- reads the full public key from the built, signed test DLL and puts it in `<InternalsVisibleTo Include="Tests.FatCat.Toolkit" Key="…" />`;
- adds `CS8002` to the test project's `NoWarn` with an XML comment explaining why (a signed test assembly references unsigned packages and the unsigned `FatCat.Toolkit.WebServer`), mirroring `ToolKit.csproj`. Leave it out only if the build proves `CS8002` does not fire.

`ToolKit.csproj` sets `SignAssembly` but no key file is visible in any project or props file, yet the built DLL is signed. Phase 01 confirms the signing state before it starts. If `FatCat.Toolkit.dll` turns out to be unsigned, the plain `InternalsVisibleTo` form is enough, the test project is not signed, and the difference goes in the deviation log.

### ADR-6 — One approved real-thread test (owner decision)

The testing rules ban real threads in unit tests. The owner approved exactly **one** test class, `ConcurrencyTests` in phase 03, with fixed worker and iteration counts and outcomes that do not depend on scheduling: no exception, and `StoredCount <= MaximumItems + workerCount`. A comment in the class cites this ADR. No other test may use real threads.

### ADR-7 — Fake fix: forward the timeout, widen the matchers, add timeout-asserting overloads (owner decision)

The existing `VerifyAdd` / `VerifyAddList` / `VerifyDidNotAdd` / `VerifyDidNotAddList` move from `default` to `A<TimeSpan?>._`, which preserves their meaning (the fake never forwarded a timeout, so `default` always matched). New overloads `VerifyAdd(T, TimeSpan?)` and `VerifyAddList(List<T>, TimeSpan?)` take both parameters as required, so they never clash with the optional-parameter overloads.

### ADR-8 — Phase 01 defines the whole options contract

`FatCatCacheOptions<T>` lands complete in phase 01, with `MaximumItems`, `SweepInterval` and validation at construction, even though `MaximumItems` is enforced only from phase 03. This is the template's "shared contract first" rule: phases 03 and 04 fan out against it. Between 01 and 03 the branch accepts a cap it does not enforce. That state is never released.

### ADR-9 — Test construction of the system under test is lazy

Many scenarios configure `options` before the cache exists, and validation happens at construction. So the abstract base exposes the cache through a lazily built property (`field ??= new FatCatCache<TestCacheItem>(dateTimeUtilities, options)`, which the `field` keyword rule allows) instead of building it in the base constructor. Derived classes set `options` in their own constructors before first use.

### ADR-10 — `TestCacheItem` moves, but does not change

Phase 01 deletes `Tests.ToolKit/Caching/CacheTests.cs` (its only test asserts nothing) and moves `TestCacheItem` to `Tests.ToolKit/Caching/TestCacheItem.cs`, keeping its namespace (`Tests.FatCat.Toolkit.Caching`) and members. Phase 02 uses it and must not edit it. Phase 03 adds a second item type, `SecondTestCacheItem`, in its own file, for the "Y capped, X unbounded" container test.

### ADR-11 — Trim is serialised with a `System.Threading.Lock`; reads never lock

Only the sweep-and-trim block inside "make room" takes a private `Lock` (C# 13+ `Lock` type), and it re-checks the count inside the lock. Reads and ordinary adds stay lock-free on the `ConcurrentDictionary`. This is not the forbidden "plain `Dictionary` with manual locking": the lock only serialises a maintenance pass. `ConcurrentDictionary.Count` takes all stripe locks, so it costs O(stripes), not O(n). It is read only on the new-key path of a capped cache.

### ADR-12 — No version bump, no publish

The owner releases after all phases are done and records the version in Fog's tracker. No phase edits a `VersionPrefix` or runs `PushNugetPackages.ps1`.

### ADR-13 — Security gate: only new findings block (owner decision, 2026-11-13, after wave 1)

`code-security-review` scans the whole tree, and phase 02's run reported findings that were already there and that this plan does not touch: H1/H2 (TLS certificate validation disabled), M1–M4 (including M4, test fakes possibly registered by `SystemScope`'s scan), and L1–L10. **A phase passes the security gate when it introduces or worsens no vulnerability.** Existing findings do not block, but each phase report lists any it sees, for a separate toolkit task. Run the review screen-only, so no report file lands outside a phase's `## Touches`.

### ADR-14 — Thread-safe throttle comparison (from wave-1 phase 01 failure)

The sweep throttle compares `now.Ticks - lastSweep < sweepInterval.Ticks` and has **no** "clock moved backward" branch. Attempt 1 added a `< 0` branch, and code review showed it also fires when threads race: a thread holding a `now` read just before another thread swept sees a negative gap and runs a second sweep. A backward wall clock is an accepted limitation: sweeps pause until the clock passes the last sweep again, and per-key expiry on reads still works.

### ADR-15 — Phase 02's deferred test hardening moves to phase 05 (owner decision)

Phase 02's third `code-review` suggested test-only facts that were not added because the correction cap was reached. Phase 05 adds them; see its step 6.

## Assumptions

- `IDateTimeUtilities` is resolvable in every consumer container. `SystemScope` always includes the ToolKit assembly (`EnsureAssembly`) and scanning registers `DateTimeUtilities`. A consumer building a container without `SystemScope` and without `IDateTimeUtilities` falls back to the parameterless constructor (unbounded, real clock), which is what happens today.
- `FatCat.Testing` has a synchronous `Should().Throw<TException>()` counterpart to the `ThrowAsync` used in `Tests.ToolKit/Data/Mongo/DataRepositorySpecs/`. If it does not, use xUnit `Assert.Throws<T>` and log the deviation.
- The repo's `CSharpier`, `dotnet format` and warnings-as-errors settings accept `System.Threading.Lock` and the `field` keyword (both on .NET 11 / C# 15).
- Plans live under `src\task\` (singular). The template's `tasks\todo` / `tasks\done` mean `src\task\todo` / `src\task\done` here.

## Open questions (none block the plan)

- **Branch.** At planning time the checkout was `upgrade-net11-slnx-packages`, an unrelated open PR. The template says to use the current branch when it is not `main`. The orchestrator asks the human once, before wave 1, whether to proceed there or on a dedicated `task/fatcat-cache-cap` branch.
- **Default `SweepInterval`.** 30 seconds comes from the brief's example. Fog's CG-01 may want a different value per cache; it is already an option.
