# Phase 01 — Cache foundation: options contract, clock injection, per-key expiry, throttled sweep, list-timeout fix

**Risk: HIGH, flagged for extra human review.** This phase adds a public type (`FatCatCacheOptions<T>`) and public constructors to `FatCatCache<T>` in a published NuGet package. It also changes the runtime expiry behaviour of the cache every toolkit consumer resolves, and it signs the test assembly.

- **Depends on:** nothing.
- **Depended on by:** 03, 04, 05. Reverting this phase means reverting 05, 04 and 03 first.
- **Wave:** 1.
- **Parallel with:** 02. Disjoint write sets. Only this phase edits a hotspot (`ToolKit.csproj`, `Tests.ToolKit.csproj`). 02 reads `TestCacheItem`, which this phase moves **unchanged**.

## Read first (in this order)

1. `src/task/todo/fatcat-cache-cap.md`: the task spec. R1, R3–R8 are this phase. R2 (cap enforcement) is **not**, apart from the options contract.
2. `src/task/todo/fatcat-cache-cap/00-overview.md`: ADR-1, 3, 4, 5, 8, 9 and 10 are binding here.
3. `src/CLAUDE.md` and every file in `src/.claude/rules/csharp/`.
4. The code: `src/ToolKit/Caching/FatCatCache.cs`, `src/ToolKit/DateTimeUtilities.cs`, `src/ToolKit/ToolKit.csproj`, `src/Tests.ToolKit/Tests.ToolKit.csproj`, `src/Tests.ToolKit/Caching/CacheTests.cs`.

## Goal

At the end of this phase, `FatCatCache<T>`:

- takes `IDateTimeUtilities` and `FatCatCacheOptions<T>` through a primary constructor, with chained `(IDateTimeUtilities)` and `()` constructors;
- validates its options at construction;
- never reads `DateTime.UtcNow`;
- checks only the requested key in `Get` / `InCache` / `Remove`;
- runs the full expiry sweep at most once per `SweepInterval`;
- has no `timeoutEnabled` flag;
- applies the timeout in `Add(List<T>, TimeSpan?)`;
- exposes an `internal int StoredCount` to the (now signed) test assembly.

`MaximumItems` exists and is validated, but is **not enforced** until phase 03 (ADR-8).

## Step 0 — Confirm the signing state (before any change)

```bash
cd src && dotnet build ToolKit.slnx
pwsh -NoProfile -Command '[System.Reflection.AssemblyName]::GetAssemblyName("C:\Code\FatCat.Toolkit\src\ToolKit\bin\Debug\net11.0\FatCat.Toolkit.dll").GetPublicKeyToken().Length'
```

(Use the worktree's own path if you are running in a worktree.)

- Output `8`: the DLL is signed. Follow "Signing the test assembly" below.
- Output `0`: the DLL is unsigned. Use plain `<InternalsVisibleTo Include="Tests.FatCat.Toolkit" />`, do **not** sign the test project, and record this in the deviation log.

Also record the baseline: `dotnet test ToolKit.slnx` must be green before you change anything. Write down the test count.

## Signing the test assembly (ADR-5)

1. In `Tests.ToolKit.csproj`, add `<SignAssembly>true</SignAssembly>` and `<AssemblyOriginatorKeyFile>..\ToolKit\FatCatToolKit.snk</AssemblyOriginatorKeyFile>`.
2. Build. If `CS8002` fails the build, add it to the test project's `NoWarn` with an XML comment directly above, for example `<!-- CS8002: the test assembly is strong-named so FatCat.Toolkit can grant it InternalsVisibleTo; its test packages and FatCat.Toolkit.WebServer are not strong-named -->`. Extend the existing `NoWarn` (`$(NoWarn);…`); do not replace it. Do not add any other suppression.
3. Read the full public key of the built test DLL:
   ```bash
   pwsh -NoProfile -Command '[Convert]::ToHexString([System.Reflection.AssemblyName]::GetAssemblyName("<path>\Tests.ToolKit\bin\Debug\net11.0\Tests.FatCat.Toolkit.dll").GetPublicKey()).ToLower()'
   ```
4. In `ToolKit.csproj`, add an `ItemGroup` with `<InternalsVisibleTo Include="Tests.FatCat.Toolkit" Key="<that hex>" />`.
5. Build again: zero warnings, and `dotnet test` still green **before** writing any new test. This proves the signing change on its own.

## Tests first (red before green)

Replace `src/Tests.ToolKit/Caching/CacheTests.cs`:

- Move `TestCacheItem` **unchanged** (same namespace `Tests.FatCat.Toolkit.Caching`, same members) to `src/Tests.ToolKit/Caching/TestCacheItem.cs`.
- Delete `CacheTests.cs`. Its only test, `CanCacheItems`, asserts nothing.

Create `src/Tests.ToolKit/Caching/FatCatCacheSpecs/`, namespace `Tests.FatCat.Toolkit.Caching.FatCatCacheSpecs`.

**`FatCatCacheTests.cs`: the abstract base.**

- `protected readonly IDateTimeUtilities dateTimeUtilities = A.Fake<IDateTimeUtilities>();`
- `protected DateTime now = Faker.Create<DateTime>();` returned via `A.CallTo(() => dateTimeUtilities.UtcNow()).ReturnsLazily(() => now)`.
- `protected FatCatCacheOptions<TestCacheItem> options = new();`
- A lazily built system under test (ADR-9): `protected FatCatCache<TestCacheItem> Cache { get { return field ??= new FatCatCache<TestCacheItem>(dateTimeUtilities, options); } }`. Derived classes set `options` before first touching `Cache`.
- Helpers: `protected void AdvanceClock(TimeSpan amount)`; `protected TestCacheItem AddItem(TimeSpan? timeout = null)` (creates with `Faker.Create<TestCacheItem>()`, adds, returns it); `protected void PassTheSweepInterval()` (advances the clock by `options.SweepInterval + 1.Seconds()`).
- Durations use Humanizer (`5.Minutes()`), never `TimeSpan.From*`.

**One class per scenario.** Each `[Fact]` is verb-first with one assertion. The names below are the expected minimum; add more if a behaviour is uncovered.

| Class | Facts |
|---|---|
| `ConstructorTests` | `ThrowWhenMaximumItemsIsZero`, `ThrowWhenMaximumItemsIsNegative`, `ThrowWhenSweepIntervalIsZero`, `ThrowWhenSweepIntervalIsNegative`, `NameMaximumItemsInTheException` (`ParamName` / message), `NameSweepIntervalInTheException`, `StoreItemsWhenBuiltWithTheParameterlessConstructor`, `StoreItemsWhenBuiltWithTheClockOnlyConstructor`, `DefaultToUnboundedOptions` (`new FatCatCacheOptions<T>().MaximumItems` is null), `DefaultTheSweepIntervalToThirtySeconds` |
| `AddTests` | `StoreTheItem`, `ReplaceTheItemWithTheSameCacheId`, `KeepAnItemWithoutATimeoutIndefinitely` (advance the clock by `3650.Days()`), `RestartTheTimeoutWhenTheItemIsReplaced`, `RetainEveryItemWhenUnbounded` (add, for example, 5,000 items; assert all present) |
| `AddListTests` | `StoreEveryItem`, `KeepEveryItemBeforeTheTimeout`, `ExpireEveryItemAfterTheTimeout` (**the R4 bug: red today**) |
| `GetTests` | `ReturnTheItem`, `ReturnNullForAnUnknownId`, `ReturnTheItemAtExactlyTheTimeout`, `ReturnNullOnceExpired`, `RemoveTheExpiredEntry` (`StoredCount` drops) |
| `InCacheTests` | `ReturnTrueWhenPresent`, `ReturnFalseForAnUnknownId`, `ReturnFalseOnceExpired`, `RemoveTheExpiredEntry`, `ReturnTrueForTheItemOverload` (the default interface member `InCache(T)`, called through `IFatCatCache<T>`) |
| `RemoveTests` | `RemoveTheItem`, `IgnoreAnUnknownId`, `RemoveThroughTheItemOverload` |
| `GetAllTests` | `ReturnEveryItem`, `ExcludeExpiredItemsBeforeTheSweepIsDue` |
| `ClearTests` | `EmptyTheCache` |
| `SweepTests` | `LeaveOtherExpiredEntriesStoredBeforeTheSweepInterval` (expired A; `Get(B)` → `StoredCount` still includes A), `RemoveEveryExpiredEntryOnceTheSweepIntervalHasPassed`, `SweepAtMostOncePerInterval` (after a sweep, a newly expired entry survives further calls until the next interval), `NeverReadTheSystemClock` (optional, if it can be asserted cleanly; otherwise rely on the AC grep below) |

Run them and **quote the red** in your report. `AddListTests.ExpireEveryItemAfterTheTimeout`, the `SweepTests` and every constructor test must fail (or fail to compile) for the right reason before you implement.

## Implementation guidance

**`src/ToolKit/Caching/FatCatCacheOptions.cs`** (new, namespace `FatCat.Toolkit.Caching`, no `#nullable` directive):

- `public class FatCatCacheOptions<T> where T : class, ICacheItem`
- `public int? MaximumItems { get; set; }`. XML doc: null means unbounded; a capped cache may exceed the cap transiently by at most the number of concurrent writers.
- `public TimeSpan SweepInterval { get; set; } = 30.Seconds();` (`using Humanizer;`)

**`src/ToolKit/Caching/FatCatCache.cs`** (keep `#nullable enable`; **do not touch `IFatCatCache<T>`**):

- `public class FatCatCache<T>(IDateTimeUtilities dateTimeUtilities, FatCatCacheOptions<T> options) : IFatCatCache<T>`
- `public FatCatCache(IDateTimeUtilities dateTimeUtilities) : this(dateTimeUtilities, new FatCatCacheOptions<T>()) { }`
- `public FatCatCache() : this(new DateTimeUtilities(), new FatCatCacheOptions<T>()) { }`. This is the one accepted `new` (ADR-1).
- **Primary constructors have no body.** Validate and **copy** values in readonly field initializers, for example `private readonly int? maximumItems = RequirePositive(options.MaximumItems, nameof(FatCatCacheOptions<T>.MaximumItems));` and `private readonly TimeSpan sweepInterval = …`. Use `options` **only** in initializers, never in methods, to avoid `CS9124` (a parameter captured and also used in an initializer), which warnings-as-errors turns into a build break. `dateTimeUtilities` is used only in methods.
- Throw `ArgumentOutOfRangeException(paramName, value, message)` with a message naming the property and the rule (for example "FatCatCacheOptions.MaximumItems must be greater than zero when set").
- `private long lastSweepTicks;` starts at 0, so the first call after construction sweeps an empty or near-empty dictionary, which is harmless. The tests must not assume otherwise.
- `SweepIfDue(DateTime now)`: if `now.Ticks - Interlocked.Read(ref lastSweepTicks) < sweepInterval.Ticks`, return. Otherwise only the thread whose `Interlocked.CompareExchange` succeeds runs `RemoveExpired(now)`.
- `RemoveExpired(DateTime now)`: enumerate the `ConcurrentDictionary` (safe during writes); for each pair whose entry `HasExpired(now)`, `cache.TryRemove(pair)` (the `KeyValuePair` overload, ADR-4).
- Each public member reads `now` **once** via `dateTimeUtilities.UtcNow()`, calls `SweepIfDue(now)`, then does its own work:
  - `Add(T, TimeSpan?)`: `cache[cacheItem.CacheId] = new CacheEntry(cacheItem, timeout, now)` (or `AddOrUpdate`).
  - `Add(List<T>, TimeSpan?)`: `Add(item, timeout)` for each item. **This is the R4 fix.**
  - `Get`: `TryGetValue`. If the entry expired at `now`, `TryRemove(pair)` and return null.
  - `InCache(string)`: the same check, returning a bool. Share a private helper with `Get` (for example `TryGetLiveEntry`).
  - `Remove(string)`: `TryRemove(cacheId, out _)`.
  - `GetAll`: `cache.Values.Where(entry => !entry.HasExpired(now)).Select(entry => entry.Item).ToList()`.
  - `Clear`: `cache.Clear()`.
- `internal int StoredCount { get { return cache.Count; } }` (block body; expression bodies are banned).
- The nested `CacheEntry` becomes `CacheEntry(T item, TimeSpan? timeout, DateTime entryTime)` with `public bool HasExpired(DateTime now)`, using strict `now - EntryTime > Timeout`. Keep it nested and private, and drop its redundant second type parameter if that stays clean.
- **Delete `timeoutEnabled`** and every read or write of it.
- No `DateTime.UtcNow` / `DateTime.Now` anywhere in the file.

## Touches

- `src/ToolKit/Caching/FatCatCache.cs` (modify)
- `src/ToolKit/Caching/FatCatCacheOptions.cs` (create)
- `src/ToolKit/ToolKit.csproj` (modify: `InternalsVisibleTo` item only; **never `VersionPrefix`**). Hotspot.
- `src/Tests.ToolKit/Tests.ToolKit.csproj` (modify: signing + `NoWarn CS8002`). Hotspot.
- `src/Tests.ToolKit/Caching/CacheTests.cs` (delete)
- `src/Tests.ToolKit/Caching/TestCacheItem.cs` (create, moved verbatim)
- `src/Tests.ToolKit/Caching/FatCatCacheSpecs/*.cs` (create)
- Exclusive runtime resources: none.

## Verification

```bash
cd src
dotnet build ToolKit.slnx                         # zero warnings
dotnet test ToolKit.slnx                          # green; count = baseline − 1 (CanCacheItems) + new facts
dotnet format style ToolKit.slnx --verify-no-changes
dotnet format analyzers ToolKit.slnx --verify-no-changes
```

Then check by hand:

- `git diff -- src/ToolKit/Caching/FatCatCache.cs`: the `IFatCatCache<T>` block is unchanged, character for character.
- `grep -n "DateTime.UtcNow\|DateTime.Now\|timeoutEnabled" src/ToolKit/Caching/FatCatCache.cs` returns nothing.
- `git diff -- src/ToolKit/ToolKit.csproj` shows only the `InternalsVisibleTo` item.

## Definition of Done

- [ ] Signing state confirmed in Step 0 and recorded in the report.
- [ ] Baseline test count recorded before any change.
- [ ] Test-assembly signing and `InternalsVisibleTo` build green **before** new tests were written.
- [ ] Every fact listed above exists, was red first (quoted in the report), and is now green.
- [ ] `IFatCatCache<T>` unchanged; no `VersionPrefix` change.
- [ ] No `DateTime.UtcNow` / `DateTime.Now` / `timeoutEnabled` in `FatCatCache.cs`.
- [ ] All Quality Gates in the task spec's "Definition of Done (every phase)", including the `unit-test-review` → `code-review` → `code-security-review` loop.
- [ ] Exactly one commit, message `fatcat-cache-cap 01: cache foundation (src/task/todo/fatcat-cache-cap/01-cache-foundation.md)`. Not pushed.
- [ ] Phase report written (files, test counts, deviation log, risks).

## Rollback Procedure

`git revert <phase-01 commit>`, after reverting 05, 04 and 03 (in that order) if they exist. No data, config or flags are involved. The revert restores `CacheTests.cs` and unsigns the test assembly.

## Hand-off

- `FatCat.Toolkit.Caching.FatCatCacheOptions<T>`: `int? MaximumItems` (validated > 0 when set; **not yet enforced**), `TimeSpan SweepInterval` (default `30.Seconds()`, validated > 0).
- `FatCatCache<T>` constructors: `(IDateTimeUtilities, FatCatCacheOptions<T>)` (primary), `(IDateTimeUtilities)`, `()`.
- Internals visible to tests: `internal int StoredCount`. Private helpers phase 03 builds on: `SweepIfDue(now)`, `RemoveExpired(now)`, `CacheEntry.EntryTime`, `CacheEntry.HasExpired(now)`.
- Test scaffolding: abstract `Tests.FatCat.Toolkit.Caching.FatCatCacheSpecs.FatCatCacheTests` (fake clock `now`, `options`, lazy `Cache`, `AdvanceClock`, `AddItem`, `PassTheSweepInterval`); `Tests.FatCat.Toolkit.Caching.TestCacheItem` (unchanged shape).
- Build: the test assembly is strong-named with `ToolKit/FatCatToolKit.snk`.
