# Work Item

fatcat-cache-cap

# Specification

> **Origin:** Fog's Vendor Cost Guards epic, finding **F-20**. Brief: `C:\Code\Fog\tasks\planning\fatcat-toolkit-cache-cap-brief.md`. This is a **prerequisite**: no phase of the Fog epic starts until a toolkit version with this change is released. The release itself is not part of this task (see Non-Goals).

Fog keeps several caches that an attacker can grow: one entry per IP address (MaxMind geolocation), per phone number + name (Twilio Lookup, Trestle Reverse Phone), per phone number (sign-up codes), and per nonce. Rotating IPs or numbers adds an entry per request, so these caches grow without limit for up to 24 hours. Brume has crash-looped on out-of-memory before.

`FatCatCache<T>` ([FatCatCache.cs](../../ToolKit/Caching/FatCatCache.cs)) is the only `IFatCatCache<T>` implementation. It is registered as an open generic `SingleInstance` in [ToolkitModule.cs](../../ToolKit/Web/Api/SignalR/ToolkitModule.cs), so one instance per `T` lives for the life of the container.

## What is wrong today (verified against the code)

1. **Unbounded.** `FatCatCache<T>` is a plain `ConcurrentDictionary<string, CacheEntry<T>>` with no size limit.
2. **Every call scans the whole cache.** `Add`, `Get`, `GetAll`, `InCache` and `Remove` all call `RemoveExpiredItems()`, which walks every entry. At 100k entries every lookup is O(n).
3. **Bug: `Add(List<T> cacheItems, TimeSpan? timeout)` drops the timeout.** It calls `Add(item)` without passing `timeout`, so a list added with a timeout never expires.
4. **`timeoutEnabled` is a plain `bool` read and written from many threads** with no synchronisation. One thread can reset it to `false` (cache momentarily empty) while another adds a timed item, which turns expiry off for that item until the next timed add.
5. **Expiry reads `DateTime.UtcNow` directly** (inside the nested `CacheEntry<TType>`), so expiry and eviction cannot be tested deterministically. This also breaks the `IDateTimeUtilities` rule.
6. **Bug: `FakeFatCatCache<T>` drops the timeout too.** [FakeFatCatCache.cs](../../ToolKit/Testing/FakeFatCatCache.cs) `Add(T, TimeSpan?)` and `Add(List<T>, TimeSpan?)` call `Cache.Add(item)` without the timeout, and every `Verify*Add*` method matches the timeout against `default`. A consumer cannot assert which timeout their code used. (Found during planning; the owner decided to fix it here.)

## Required behaviour

### R1. Opt-in maximum size per cache type

- Add a new public class **`FatCatCacheOptions<T>`** (`where T : class, ICacheItem`) in `ToolKit/Caching/FatCatCacheOptions.cs`, namespace `FatCat.Toolkit.Caching`:
  - `MaximumItems` (`int?`). `null` (the default) means **unbounded**.
  - `SweepInterval` (`TimeSpan`), default `30.Seconds()` (Humanizer).
  - Invalid values (`MaximumItems <= 0`, `SweepInterval <= TimeSpan.Zero`) throw `ArgumentOutOfRangeException` naming the property, when `FatCatCache<T>` is constructed. This is a programming error, not an expected outcome.
- **The default stays unbounded.** Nothing changes for caches that must never lose an entry. Fog's `HazeConnectionCacheItem`, `UserConnectionCacheItem`, `IBlockUploadTransferHandler`, `IBlockDownloadHandler` and the block caches hold live connections and in-flight transfers.
- A consumer opts a specific `T` in by registering an instance in its own Autofac module, with **no other change**:
  ```csharp
  builder.RegisterInstance(new FatCatCacheOptions<IpGeoLocationCacheItem> { MaximumItems = 50_000 });
  ```
- **Constructors on `FatCatCache<T>`** (the primary constructor plus chained secondaries; no explicit constructor bodies):
  - Primary: `FatCatCache(IDateTimeUtilities dateTimeUtilities, FatCatCacheOptions<T> options)`.
  - `FatCatCache(IDateTimeUtilities dateTimeUtilities)` chains with `new FatCatCacheOptions<T>()`. This is the constructor DI uses when no options are registered, so the container's clock is always injected.
  - `FatCatCache()` chains with `new DateTimeUtilities()` and default options. It keeps `new FatCatCache<T>()` working (Fog's tests use it, for example `DesktopNonceServiceTests`, `PasskeyResetTokensTests`).
  - Autofac picks the constructor with the most parameters it can resolve. `FatCatCacheOptions<T>` is an open generic, so assembly scanning in [SystemScope.cs](../../ToolKit/Injection/SystemScope.cs) (`RegisterAssemblyTypes`) does **not** register it. Without an explicit registration Autofac falls back to the `(IDateTimeUtilities)` constructor. **A container-level test must prove both paths** (with and without a registered `FatCatCacheOptions<T>`).
- `ToolkitModule`'s registration line does not change.

### R2. Eviction when full: batch trim (owner decision)

- Updating an **existing key never evicts**. Its entry time is refreshed (as today, `AddOrUpdate` creates a new entry).
- Adding a **new key** to a capped cache whose stored count is `>= MaximumItems`:
  1. Run the full expiry sweep (ignoring the throttle).
  2. If still `>= MaximumItems`, **evict the oldest entries by entry time down to a low-water mark**, then add.
- **Low-water mark:** `MaximumItems - max(1, MaximumItems / 10)`, which is about 90% of the cap and always frees at least one slot. It is an internal constant of the algorithm, not an option. One O(n) pass pays for about `MaximumItems / 10` later adds, so under attack the amortised cost per add stays low.
- Only one thread trims at a time (an `Interlocked` guard or a `lock` around the sweep-and-trim block only, never around reads). Other threads keep adding. The cap may be **exceeded transiently under concurrent adds** by at most the number of concurrent writers. This is documented in the XML doc comment of `MaximumItems`.

### R3. No full scan per call

- `Get`, `InCache` and `Remove` look at **only the requested entry**. An expired entry is removed and treated as absent. Remove with the `KeyValuePair` overload of `TryRemove` so a concurrent re-add of the same key is not removed by mistake.
- The full expiry sweep runs **at most once per `SweepInterval`**. It is triggered lazily at the start of a public call when the interval has elapsed (last-sweep time held in a `long` of ticks and claimed with `Interlocked.CompareExchange`, so only one thread sweeps). It also runs when a capped cache is full (R2). **No timer and no background thread.**
- `GetAll` returns only unexpired items. It filters with the current time even if no sweep is due.
- `Clear` empties the cache.

### R4. Fix `Add(List<T>, TimeSpan?)`

Each item gets the timeout.

### R5. Thread safety

Remove the `timeoutEnabled` flag entirely. It only existed to skip scans, and the throttled sweep makes it unnecessary. All shared mutable state is the `ConcurrentDictionary` plus `Interlocked`-managed fields.

### R6. Testable time

Entry time and expiry use the injected `IDateTimeUtilities.UtcNow()`. `CacheEntry` takes its entry time as a constructor argument, and `HasExpired(DateTime now)` takes the current time; it never reads the clock itself. Expiry keeps today's strict semantics: expired when `now - EntryTime > Timeout`.

### R7. Public interface unchanged

`IFatCatCache<T>` keeps exactly its current members, including the two default interface members (`InCache(T)`, `Remove(T)`) and its `#nullable enable` state. Fog compiles untouched.

### R8. Test seam: `InternalsVisibleTo` (owner decision)

- Add `<InternalsVisibleTo Include="Tests.FatCat.Toolkit" Key="…" />` to [ToolKit.csproj](../../ToolKit/ToolKit.csproj). This is the first use of this pattern in the repo. The owner approved it for this task.
- **Strong naming (found during phased planning; owner decision):** `FatCat.Toolkit.dll` is strong-name signed (`SignAssembly` is true; the built DLL carries a public key token) and `Tests.FatCat.Toolkit` is not. A signed assembly cannot name an unsigned friend (CS1726). So [Tests.ToolKit.csproj](../../Tests.ToolKit/Tests.ToolKit.csproj) is signed with the existing `ToolKit/FatCatToolKit.snk`, the `InternalsVisibleTo` item carries the test assembly's full public key, and `CS8002` (referenced assembly has no strong name) goes into the test project's `NoWarn` with an explanatory comment, the same suppression `ToolKit.csproj` already carries. Only if the build proves `CS8002` does not fire is the `NoWarn` left out.
- Expose an **internal** stored-entry count on `FatCatCache<T>` (for example `internal int StoredCount`). It counts entries physically held, including expired entries not yet swept, so a test can prove an expired entry for another key is **not** removed by an unrelated `Get` before the sweep interval elapses. Nothing new becomes public for this.

### R9. Fix `FakeFatCatCache<T>` (owner decision; public test-helper behaviour change)

- `Add(T, TimeSpan?)` and `Add(List<T>, TimeSpan?)` forward the timeout to `Cache.Add`.
- Existing `VerifyAdd`, `VerifyAddList`, `VerifyDidNotAdd` and `VerifyDidNotAddList` widen their timeout matcher from `default` to `A<TimeSpan?>._`. Their meaning (was an item added, with any timeout) is preserved for every existing caller.
- Add overloads that assert the timeout: `VerifyAdd(T expectedItem, TimeSpan? expectedTimeout)` and `VerifyAddList(List<T> expectedItems, TimeSpan? expectedTimeout)`. Both parameters are required, so these do not clash with the existing optional-parameter overloads.
- **Consumer compatibility risk:** a consumer test that asserts directly on `fake.Cache.Add(x, default)` while its production code passes a timeout will start failing. Before this phase is committed, grep Fog (`C:\Code\Fog`) for `.Cache.Add(` / `.Cache.Add(A<` used on a `FakeFatCatCache<T>` and list every hit in the phase report. Fog's direct `A.Fake<IFatCatCache<T>>()` assertions are unaffected.

## Acceptance Criteria

**Defaults and opt-in**
- [ ] Given a `FatCatCache<T>` with no options, when many more items are added than any realistic cap (for example 10× a test cap), then all of them are retained.
- [ ] Given `FatCatCacheOptions<T>` with `MaximumItems = 0` or negative, or `SweepInterval <= 0`, when the cache is constructed, then `ArgumentOutOfRangeException` naming the property is thrown.
- [ ] Given an Autofac container built with `ToolkitModule` and `IDateTimeUtilities` and **no** `FatCatCacheOptions<X>`, when `IFatCatCache<X>` is resolved, then it is an unbounded `FatCatCache<X>` using the container's `IDateTimeUtilities`.
- [ ] Given the same container **plus** a registered `FatCatCacheOptions<Y>`, when `IFatCatCache<Y>` is resolved, then it honours that `MaximumItems`, and `IFatCatCache<X>` is still unbounded.
- [ ] `new FatCatCache<T>()` still compiles and works.

**Cap and eviction**
- [ ] Given a cache capped at N, when more than N distinct keys are added one at a time, then the stored count never exceeds N.
- [ ] Given a full capped cache, when a new key is added, then the oldest entries by entry time are evicted first, down to the low-water mark `N - max(1, N / 10)`, and the new key is present.
- [ ] Given a full capped cache, when an **existing** key is updated, then nothing is evicted and the entry time of that key is refreshed.
- [ ] Given a full capped cache containing expired entries, when a new key is added, then expired entries are removed **before** any unexpired entry is evicted, and if that frees room no unexpired entry is evicted.

**Expiry and sweep (fake clock)**
- [ ] Given an item added with a timeout, when the clock moves past the timeout, then `Get` returns null, `InCache` returns false, and the entry is no longer stored.
- [ ] Given an item added with a timeout, when the clock moves to exactly the timeout (not past it), then the item is still returned.
- [ ] Given an expired entry for key A and a sweep interval that has not elapsed, when `Get(B)` is called, then entry A is still physically stored (`StoredCount` unchanged), proving no full scan ran.
- [ ] Given the sweep interval has elapsed, when any public call is made, then all expired entries are removed.
- [ ] `GetAll` never returns an expired item, whether or not a sweep is due.
- [ ] `Add(List<T>, timeout)` applies the timeout to every item (each expires after the clock moves past it).
- [ ] `Remove` removes the item; `Clear` empties the cache.

**Concurrency (the one approved real-thread test; see ADR-6)**
- [ ] Given a cache capped at N, when parallel workers add distinct keys and call `Get` / `InCache` concurrently, then no exception is thrown and the final stored count is `<= N + workerCount` (the documented transient).

**Fake**
- [ ] `FakeFatCatCache<T>.Add(item, timeout)` and `Add(list, timeout)` forward the timeout to `Cache`.
- [ ] Existing `VerifyAdd` / `VerifyAddList` / `VerifyDidNotAdd` / `VerifyDidNotAddList` pass whatever timeout was used.
- [ ] New `VerifyAdd(item, timeout)` / `VerifyAddList(items, timeout)` pass on a matching timeout and fail on a different one.

**Quality**
- [ ] `IFatCatCache<T>`'s member list is byte-for-byte unchanged (review the diff).
- [ ] `FatCatCache<T>` no longer references `DateTime.UtcNow` or `DateTime.Now`.
- [ ] All new and changed production code is covered test-first (red before green), and `unit-test-review` ends in `PASS`.
- [ ] No `VersionPrefix` change in either `.csproj`.
- [ ] **E2E walk (final phase):** this library has no automated end-to-end suite, so the final phase (1) runs `dotnet test ToolKit.slnx` fully green twice in a row with no manual step between, (2) in `OneOffToolkitOnly` (hand-run sample, not shipped), resolves one capped and one uncapped cache through `SystemScope`, adds past the cap and prints the counts, and (3) greps `C:\Code\Fog` for every `FatCatCache` construction and `FakeFatCatCache` assertion and confirms none needs a code change except the `.Cache.Add(x, default)` hits listed under R9. Results go in the final phase report.

## Out of Scope / Non-Goals

- **Releasing.** No `VersionPrefix` bump, no `PushNugetPackages.ps1`, no publish. The owner releases when all phases are done and records the version in Fog's epic tracker and `vendor-cost-guards-decisions.md` (F-20).
- **Any Fog change.** Bumping Fog's package references and registering caps for `IpGeoLocationCacheItem`, `TwilioLookupCacheItem`, `ReversePhoneCacheItem`, `PhoneVerificationCodeCacheItem`, the nonce caches and `ZipCodeInfo` is Fog's CG-01.
- **Size by bytes or weight.** The cap is an item count only.
- **LRU-on-read.** Eviction is by entry (write) time; `Get` does not refresh recency.
- **Sliding expiration**, per-item priorities, eviction callbacks, or eviction metrics and logging.
- **A background or timer sweep.** The sweep is lazy and throttled.
- **Changing `IFatCatCache<T>`** in any way, including adding members.
- **Changing `ToolkitModule`**, moving it out of its odd `Web/Api/SignalR` folder, or changing its `SingleInstance` lifetime.
- **Microsoft DI support.** Per project memory, MS DI registration exists only on the `DISupport` branch. When that branch merges, its cache registration must honour `FatCatCacheOptions<T>` the same way, but that is not this task.
- **Replacing `FatCatCache` with `Microsoft.Extensions.Caching.Memory`** or any other new package.

## Feature Context

### Code this touches

| File | Role |
|---|---|
| [ToolKit/Caching/FatCatCache.cs](../../ToolKit/Caching/FatCatCache.cs) | `IFatCatCache<T>` + `FatCatCache<T>` + nested `CacheEntry<TType>`. File is `#nullable enable`; keep it so. |
| `ToolKit/Caching/FatCatCacheOptions.cs` (new) | Public opt-in options. |
| [ToolKit/Caching/ICacheItem.cs](../../ToolKit/Caching/ICacheItem.cs) | Unchanged. |
| [ToolKit/Testing/FakeFatCatCache.cs](../../ToolKit/Testing/FakeFatCatCache.cs) | Shipped test fake (R9). |
| [ToolKit/Web/Api/SignalR/ToolkitModule.cs](../../ToolKit/Web/Api/SignalR/ToolkitModule.cs) | Open-generic `SingleInstance` registration. Read only; must not change. |
| [ToolKit/Injection/SystemScope.cs](../../ToolKit/Injection/SystemScope.cs) | `BuildContainer`: `RegisterAssemblyModules` + `RegisterAssemblyTypes(...).AsImplementedInterfaces().AsSelf()`. Explains why options are not auto-registered and why `IDateTimeUtilities` resolves. |
| [ToolKit/DateTimeUtilities.cs](../../ToolKit/DateTimeUtilities.cs) | `IDateTimeUtilities` (`UtcNow()`). |
| [ToolKit/ToolKit.csproj](../../ToolKit/ToolKit.csproj) | Add `InternalsVisibleTo`. `VersionPrefix` 1.0.356 stays put. Shared hotspot. |
| [Tests.ToolKit/Caching/CacheTests.cs](../../Tests.ToolKit/Caching/CacheTests.cs) | Holds `TestCacheItem` and one assertion-free test. Replace with `Tests.ToolKit/Caching/FatCatCacheSpecs/` (abstract `FatCatCacheTests` + one class per scenario) and move `TestCacheItem` to its own file, `Tests.ToolKit/Caching/TestCacheItem.cs`. |
| `Tests.ToolKit/Testing/FakeFatCatCacheSpecs/` (new) | Tests for the fake, mirroring `ToolKit/Testing` (prior art: `Tests.ToolKit/Testing/MongoFakeRepositorySpecs/`). |
| `OneOffToolkitOnly/` | Hand-run sample for the E2E walk only. |
| [README.md](../../../README.md) (repo root, around line 269) | "Thread-safe in-memory cache" section. Add the opt-in cap and eviction behaviour. Its sample uses `TimeSpan.FromMinutes(5)`; switch it to `5.Minutes()` while there. |

### Consumer (Fog) facts gathered during planning

- Fog's own tests construct `new FatCatCache<DesktopNonce>()` and `new FatCatCache<PasskeyResetCacheItem>()` directly, so the parameterless constructor must stay.
- 19 Fog test files use `FakeFatCatCache<T>`. `VerifyAdd(item)` is called in `GetBlockByIdMessageProcessorTests`, `ReadyForBlockRetrievalMessageProcessorTests` and `HazeConnectionMessageProcessorTests`. These keep passing with the widened matcher. No Fog test asserts on `fake.Cache.Add(...)` directly (checked at planning time).
- Fog's own `Common/Common/Infrastructure/CommonModule.cs:89` re-registers `FatCatCache<>` as an open generic `SingleInstance`, with the same shape as `ToolkitModule`. Autofac's constructor selection works the same through either registration.
- Several Fog tests assert `A.CallTo(() => someCache.Add(x, default))` on a plain `A.Fake<IFatCatCache<T>>()`. These are unaffected by R9.

### ADR guidance for the planner

- **ADR-1: Opt-in through `FatCatCacheOptions<T>` resolved by Autofac constructor selection.** *Decision:* a per-`T` options instance the consumer registers, and constructor overloads so Autofac picks the richest constructor it can satisfy. *Rejected:* a non-generic options dictionary keyed by `Type` (stringly, and every consumer would share one registration); an `ICacheItem` static or attribute declaring the cap (puts host policy on the data type and cannot vary per host); a separate `BoundedFatCatCache<T>` type (consumers would need to change their registrations, which the brief forbids).
- **ADR-2: Batch trim to a low-water mark** (owner decision). *Rejected:* exactly-one eviction per add (O(n) per add when full, which defeats the point); an insertion-order `ConcurrentQueue` (O(1), but more state, and repeated updates grow the queue without compaction).
- **ADR-3: Lazy, throttled sweep claimed with `Interlocked`.** *Rejected:* a timer or background loop (needs `IThread`, adds a lifetime and disposal story to a `SingleInstance`, and exceptions on a background path go unobserved); keeping `timeoutEnabled` (racy, see problem 4).
- **ADR-4: Per-key expiry check on `Get` / `InCache` / `Remove`, using `TryRemove(KeyValuePair)`** so a concurrent re-add is never removed by mistake.
- **ADR-5: `InternalsVisibleTo` + internal stored count as the sweep seam** (owner decision). *Rejected:* a public `Count` (permanent public surface); behaviour-only assertions through clock call counts (weak and implementation-coupled).
- **ADR-6: One approved real-thread test.** The testing rules ban real threads in unit tests. The owner approved **one** bounded concurrency test class for `FatCatCache<T>`, with fixed iteration counts and an outcome that does not depend on scheduling (no exception, count `<= N + workers`). It carries a comment citing this decision. No other test may use real threads.
- **ADR-7: Fix the fake's timeout forwarding with widened matchers plus new timeout-asserting overloads** (owner decision), preserving every existing `Verify*` meaning.
- **Recommended phase shape** (for the phased plan; the planner decides): (1) options type + constructors + time abstraction + `InternalsVisibleTo` + list-timeout fix + per-key expiry + throttled sweep; (2) cap and batch eviction + concurrency test; (3) fake fix (independent of 1–2: disjoint files, so it can run in parallel with phase 1, but **not** if both edit `ToolKit.csproj`; keep the csproj edit in phase 1 only); (4) README; (5) final verification / E2E walk. Phases 1 and 2 both edit `FatCatCache.cs` and must be serial.

### Risk flags

- **High: public API contract.** New public type `FatCatCacheOptions<T>`, new public constructors on `FatCatCache<T>`, changed runtime behaviour of the default registration that every toolkit consumer resolves (sweep timing), and a behaviour change in the shipped test helper `FakeFatCatCache<T>`. Every phase touching `ToolKit/Caching/` or `ToolKit/Testing/FakeFatCatCache.cs` is **automatically high-risk** and needs extra human review.
- **Concurrency correctness** in a `SingleInstance` shared by multi-threaded hosts (web servers, SignalR hubs). Review the sweep and trim guards line by line.
- **Security relevance:** this change is a denial-of-service (memory exhaustion) mitigation for Fog. `code-security-review` should explicitly confirm that no code path can grow a capped cache beyond `MaximumItems + concurrent writers`.

# Planning Instructions

This is a **specification and planning task. No production code is written in this session.**

- [ ] Read all references before planning
- [ ] Write a phased implementation plan to `tasks\todo\{WORK_ITEM}` (one file per phase plus `00-overview.md`)
- [ ] Record cross-cutting decisions in `00-overview.md` (treat these as lightweight ADRs — architecture decision records: decision, context, alternatives rejected)
- [ ] List **assumptions and open questions** in the overview. If an ambiguity materially changes the design, stop and ask rather than guess
- [ ] Keep commits small.
- [ ] **Plan for parallelism.** Shape the phases so as much work as possible can run at the same time. Put the shared contract (service models, requests/responses, interfaces, routes, `Errors` keys, module/projector registrations) in an early, small phase. Then fan the independent work out against that contract, for example the API endpoint, the public site page and the admin page as separate phases. Do not split a phase artificially just to parallelize: every phase must still be a coherent, atomic commit.
- [ ] Group the phases into **waves** in `00-overview.md`. A wave is a set of phases whose dependencies all sit in earlier waves and that are pairwise **parallel-safe** (see Phase Requirements). Include the dependency graph, a wave table (`Wave | Phases | Why parallel-safe`) and the critical path: the longest dependency chain, which sets the minimum number of sequential steps.
- [ ] **High-risk (task-specific):** every phase that changes `ToolKit/Caching/` or `ToolKit/Testing/FakeFatCatCache.cs` changes a public API contract of a published NuGet package and is automatically rated **high** and flagged for extra human review, per the Risk-rated rule below.

## Phase Requirements

Each phase must be:

- [ ] **Context-isolated** — executable in a fresh session with no state from prior sessions; the phase file is the complete handoff document
- [ ] **Atomic** — exactly one commit per phase; the commit message references the phase file
- [ ] **Independently revertible** — the phase declares its position in the **dependency graph** (`Depends on:` / `Depended on by:`) so a revert can cascade correctly (reverting phase 3 tells me to also revert 4 and 5 if they depend on it)
- [ ] **Reversible** — each phase includes a `## Rollback Procedure` section (the "down migration"): exact `git revert` target plus any manual steps (data, config, feature flags)
- [ ] **Verifiable** — each phase includes verification steps and a `## Definition of Done` checklist
- [ ] **Contract-defining** — each phase ends with a `## Hand-off` section: the interfaces, types, and routes it exposes to later phases
- [ ] **Risk-rated** — each phase declares a risk level (low / medium / high) and why. Anything touching auth, anonymous endpoints, data migration, or public API contracts is automatically high and flagged for extra human review
- [ ] **Footprint-declared:** each phase has a `## Touches` section. It lists every file or folder the phase creates, modifies or deletes (best estimate, globs allowed), plus any shared runtime resource it needs exclusively: local Mongo data, a running API or site, the browser, fixed ports, or a real external service.
- [ ] **Parallel-classified:** each phase declares `Wave:` and `Parallel with:` (phase numbers, or `none` and why). Two phases are parallel-safe only when **all** of these hold:
  - neither depends on the other, directly or transitively;
  - their `Touches` write sets are disjoint;
  - they do not both edit a **shared hotspot**: the `Errors.cs` error-key class, `*Module.cs`, `*CustomProjectors.cs`, `GlobalUsings.cs`, `*.csproj` / `Directory.Build.props` / `ToolKit.slnx`, `README.md`, the anonymous-route session-check exclusion list(s), the shared design tokens (CSS variables / Tailwind theme), or shared UI components;
  - neither needs an exclusive runtime resource (above);
  - neither is a human-gated phase, an E2E walk, or a report that rolls up other phases. These always run alone.
  When in doubt, serialize. A wrong "parallel" claim costs a merge conflict and a halted pipeline. A wrong "serial" claim only costs time.

## Orchestrator

- [ ] Create a phase orchestrator (runbook or script) that executes phases **wave by wave**, in dependency order
- [ ] The orchestrator MUST spawn a **fresh session per phase** (context isolation is not optional — never run multiple phases in one session/context window)
- [ ] **Parallel waves:** a wave with one phase runs in the main working tree, as before. A wave with several phases runs them concurrently, **at most 3 at a time**, with each phase in its own fresh session and its own git worktree. Two sessions never share a working tree, because builds, tests and the git index collide.
  - Create each worktree from the current task-branch tip: `git worktree add ..\FatCat.Toolkit-wt\{WORK_ITEM}-{NN} -b wt/{WORK_ITEM}/{NN}`. Copy the git-excluded local files a build needs (for example `global.json` and local `appsettings.json` overrides) into it.
  - Each phase meets its full Definition of Done and commits once, inside its worktree.
  - When every phase in the wave has committed, cherry-pick the phase commits onto the task branch **in phase-number order**. Do not squash, amend or rewrite them.
  - **Integration gate** after each multi-phase wave, on the task branch: `dotnet build` with no new warnings, then `dotnet test ToolKit.slnx` green apart from known flakes. A cherry-pick conflict or a red integration gate means the plan's parallel claim was wrong, so treat it as a failure (halt policy below). Do not hand-merge it.
  - After a successful integration, remove the wave's worktrees and `wt/` branches (`git worktree remove`, `git branch -D`).
  - If one phase in a wave fails, let its siblings finish. Integrate only the phases that passed and do not depend on the failed one, then halt.
- [ ] Commit boundary between every phase; the orchestrator never squashes or amends
- [ ] **Move the plan to done:** after the last phase is committed and its integration gate is green, move the whole plan folder with `git mv tasks\todo\{WORK_ITEM} tasks\done\{WORK_ITEM}`. This includes the phase files, reports, overview and orchestrator. Commit the move as its own commit, `{WORK_ITEM} complete - move plan to tasks/done`. Only move it when every phase is committed. If the pipeline halted or is paused at a human gate, the folder stays in `tasks\todo`. If `tasks\done\{WORK_ITEM}` already exists, stop and ask instead of overwriting it.
- [ ] **Halt-on-failure policy:** if a phase's Definition of Done cannot be met after 2 self-correction attempts, STOP the pipeline, leave the working tree clean (stash or discard), write a failure report, and do not start dependent phases
- [ ] The orchestrator never force-pushes, never rebases published history, and never pushes to remote

# Definition of Done (every phase)

## Quality Gates

- [ ] Tests written before implementation (TDD — red before green)
- [ ] No compiler warnings introduced
- [ ] Namespaces match folder paths exactly
- [ ] Must follow all rules in `.claude\rules\csharp` — no exceptions
- [ ] No banned patterns used (see `.claude/rules/not-allowed.md`)
- [ ] All tests pass (`dotnet test`)
- [ ] `dotnet format` run on all modified files
- [ ] `dotnet build` to apply CSharpier changes
- [ ] **Unit tests are the per-phase gate; end-to-end is the plan's final-phase gate.** If the project has an end-to-end suite, it is excluded from `dotnet test` and is **not** run per phase. A phase that adds or changes a deployed surface (an endpoint, a Hub push, a site page) may author or extend end-to-end scenarios alongside it, but does not run the suite green. The plan's **final phase** runs the **whole** suite (never a filtered subset) and it must finish fully green **twice in a row, with no manual step between the runs**. If the plan changed the end-to-end tests and the final phase cannot get a clean run, it fixes them or STOPS and reports to the human; never leave the suite red or silently skip it
- [ ] Run `unit-test-review` on uncommitted code; it must end with `Unit test review: PASS` — resolve every gap, defect, and conformance finding (write the missing tests) before moving to `code-review`
- [ ] Run `code-review` on uncommitted code; resolve any findings before commit
- [ ] Run `code-security-review` after `code-review` passes; any vulnerabilities found MUST be resolved before commit
- [ ] **Review loop:** code changes → `unit-test-review` until it passes → `code-review` until it passes → `code-security-review`. Any fix made at a later stage sends you back to the start of the loop (`unit-test-review`), because fixes are code changes that need tests too. Repeat until all three reviews pass with no findings — only then commit

## Commit Policy

- [ ] Work happens on the task's feature branch (`task/{WORK_ITEM}`), never on main  (only create the branch if not on main if you are on a different branch then main use the current branch)
- [ ] Create the phase commit locally; **do not push to remote** — human review gates the push

## Phase Report

Before finishing, produce a structured report:

- [ ] Files added / changed / deleted, and any file touched outside the phase's declared `## Touches` (a footprint miss can break parallel siblings)
- [ ] Test counts (new, total, passing)
- [ ] **Deviation log** — every place the implementation departed from the plan file, and why (an empty deviation log is a claim, not a default)
- [ ] Open questions or discovered risks for the human reviewer

# References

- Read the `README.md` for an overview of the application
- Source brief: `C:\Code\Fog\tasks\planning\fatcat-toolkit-cache-cap-brief.md`
- Fog epic: `C:\Code\Fog\tasks\planning\vendor-cost-guards-epic.md` (Prerequisite row; CG-01 is the Fog-side follow-up)
- Fog decisions: `C:\Code\Fog\tasks\planning\vendor-cost-guards-decisions.md` (F-20)
- Coding rules: `src/.claude/rules/csharp/` (especially `types-and-di.md` for Autofac, `IDateTimeUtilities` and Humanizer, and `testing.md` for the Specs layout and `FakeThread`)
- Key files: [FatCatCache.cs](../../ToolKit/Caching/FatCatCache.cs), [FakeFatCatCache.cs](../../ToolKit/Testing/FakeFatCatCache.cs), [ToolkitModule.cs](../../ToolKit/Web/Api/SignalR/ToolkitModule.cs), [SystemScope.cs](../../ToolKit/Injection/SystemScope.cs), [DateTimeUtilities.cs](../../ToolKit/DateTimeUtilities.cs), [CacheTests.cs](../../Tests.ToolKit/Caching/CacheTests.cs)
- Prior-art plan layout in this repo: `src/task/todo/mongo-batch-writes/` (overview, phases, consumer-compatibility, orchestrator)

# Notes

- Use components as needed to simplify changes and for look and feel.
- I control FatCat.Toolkit the source is found here `C:\Code\FatCat.Toolkit\src` if making a change to the toolkit would then suggest those changes.  A task would be written in `C:\Code\FatCat.Toolkit\src\task\todo` following the same rules found in this document.
- **Paths in this repo:** plans live under `src\task\` (singular), not `tasks\`. Read every `tasks\todo\{WORK_ITEM}` / `tasks\done\{WORK_ITEM}` above as `src\task\todo\fatcat-cache-cap` / `src\task\done\fatcat-cache-cap`. The solution is `src\ToolKit.slnx`, and `dotnet` commands run from `src\`.
- **No version bump in any phase.** The owner releases after all phases are done (Non-Goals).
- Include a `consumer-compatibility.md` in the plan folder, as `mongo-batch-writes` does, covering the Fog impact of the new constructors and of R9.
