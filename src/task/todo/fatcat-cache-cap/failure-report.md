# fatcat-cache-cap — Failure report (wave 1)

**Date:** 2026-11-13. **Branch:** `upgrade-net11-slnx-packages`. **Pipeline status:** halted after wave 1.

## What happened

| Phase | Result | Integrated |
|---|---|---|
| 01 cache-foundation | **PHASE FAILED.** Hit the 2-self-correction cap inside the review loop. Not committed. | No |
| 02 fake-timeout-forwarding | Passed every orchestrator check | Yes: `29c7d5e`, cherry-picked from `a7b4917` |

**Integration gate after wave 1:** `dotnet build ToolKit.slnx` showed 0 errors and 2 warnings, both the baseline BL0016 in `OneOffBlazor`. `dotnet test ToolKit.slnx` passed 443 of 443.

## Phase 01: why it failed

The implementation was functionally green: 45 new facts, 451 tests passing, no new warnings, `IFatCatCache<T>` unchanged, no `DateTime.UtcNow`. The review loop is what used up the correction budget:

1. `unit-test-review` #1 found that `GetAll` / `InCache` / `Remove` were not proven to trigger the sweep. Facts were added (correction 1).
2. `code-review` #1 returned 10 findings. Its fixes, including handling a backward-moving clock in the sweep throttle, were correction 2.
3. `code-review` #2 found a **real concurrency bug introduced by correction 2**. The `ticksSinceLastSweep < 0` "clock moved backward" branch also fires when threads race, so a thread that read `now` just before another swept can run a second full sweep, breaking R3. It also found 5 smaller issues. `code-security-review` was never reached.

### Work saved

- Patch: `C:\Users\dbasa\AppData\Local\Temp\claude\1f20710c76fb16d4\07c37f89-f3e8-4cf1-aea2-99f984cca420\scratchpad\fatcat-cache-cap-01-attempt1.patch` (16 files, +738/−75). Apply with `git apply --index` in a tree at `bb798a7` or later.
- The worktree `C:\Code\FatCat.Toolkit-wt\fatcat-cache-cap-01` (branch `wt/fatcat-cache-cap/01`) is clean at `bb798a7` and **kept for a retry**.

### Outstanding `code-review` #2 findings a retry must resolve

1. **Bug:** remove the backward-clock branch and go back to `now.Ticks - lastSweep < sweepInterval.Ticks` (pass 1 judged it concurrency-safe). Alternatively, treat the clock as backward only when the gap is below `-sweepInterval.Ticks`. Drop or adjust `KeepSweepingAfterTheClockMovesBackward` to match.
2. Add `ArgumentNullException` guards and tests for null `options` / `dateTimeUtilities` (`errors-and-logging.md`).
3. Move the `FatCatCacheOptions` default-value facts to `Tests.ToolKit/Caching/FatCatCacheOptionsTests.cs`.
4. Make `options` in the abstract base `protected readonly`.
5. (Declinable) `SweepTests` is not named after a method; the plan's table mandates this name.
6. `AddListTests` should build an explicit list of 3–10 items. `Faker.Create<List<T>>()` yields only 1–2.

### Phase 01 facts worth keeping

- `FatCat.Toolkit.dll` **is** signed (token `1916db74250bf654`). The test assembly was signed with the same `.snk`.
- **CS8002 did not fire**, so no `NoWarn` was added. The spec allows this.
- `lastSweepTicks` starts at 0, as the phase file says. ADR-3's wording ("initialised to construction time") is stale.

## Phase 02: caveats carried forward (committed, but disclosed)

- **Review loop stopped at the cap too.** `code-review` #3 found no correctness bugs, but suggested test-hardening facts that were not added: add without a timeout, then `VerifyAdd(item)` / `VerifyAdd()`; `VerifyDidNotAdd` after an add without a timeout; a list add must not satisfy `VerifyAdd()`. No production change is needed.
- **Security gate interpretation.** `code-security-review` (full tree, screen only) found nothing introduced by phase 02, but reported **existing** findings elsewhere in the tree:
  - **H1:** `SecureClientConnection` accepts chain errors.
  - **H2:** `UseHttpClientHandler()` disables certificate validation for the whole process.
  - **M1–M4**, including M4: `FakeThread` / `FakeFatRetry` may be picked up by `SystemScope`'s assembly scan.
  - **L1–L10.**

  Read literally, the template's "any vulnerabilities found MUST be resolved before commit" would block every phase on findings outside its scope.
- **API note:** `VerifyAdd` / `VerifyAddList` now have two overloads each, so a consumer looking them up by name through reflection would hit an `AmbiguousMatchException`.
- **Do not release 02 without 01.** Until phase 01 lands, the real `FatCatCache.Add(List<T>, timeout)` still drops the timeout.

## Not started

Phases 03, 04 and 05.

## Resolution

Phase 01 was retried from the saved patch (owner decision) and **passed**. Commit `26d2c6c`, 491 tests green. All six code-review #2 findings were resolved; the `SweepTests` rename was declined because the plan mandates the name. Wave 1 is complete.
