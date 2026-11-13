# Phase 05 — Final verification (the E2E walk)

**Risk: Low (gate).** It changes only the hand-run `OneOffToolkitOnly` sample and, if needed, README wording. Its value is the evidence it gathers. **It runs alone.**

- **Depends on:** 01, 02, 03, 04.
- **Depended on by:** nothing.
- **Wave:** 3.
- **Parallel with:** none. It is the end-to-end gate and roll-up report.

## Read first

1. `src/task/todo/fatcat-cache-cap.md`: the full **Acceptance Criteria** list. This phase ticks every box or reports why not.
2. `src/task/todo/fatcat-cache-cap/00-overview.md` and `consumer-compatibility.md`.
3. The phase reports of 01–04, if the orchestrator passed them in.

## Steps

### 1. The full suite, twice

This library has no automated end-to-end suite, so the whole unit suite is the gate.

```bash
cd src
dotnet build ToolKit.slnx                          # zero warnings
dotnet test ToolKit.slnx                           # run 1
dotnet test ToolKit.slnx                           # run 2, no manual step between
dotnet format style ToolKit.slnx --verify-no-changes
dotnet format analyzers ToolKit.slnx --verify-no-changes
```

Both runs must be fully green, with no filter. If either is red, fix the cause (a flaky `ConcurrencyTests` is a **real** defect, not a flake) or **stop** and report.

### 2. Hand-run walk in `OneOffToolkitOnly`

1. Add `src/OneOffToolkitOnly/CacheCapWorker.cs`, a sample worker resolved from `SystemScope`. Register `FatCatCacheOptions<CappedSampleItem> { MaximumItems = 1_000 }` in a small module inside `OneOffToolkitOnly`. The sample item types also live there, one class per file.
2. The worker adds 10,000 distinct items to `IFatCatCache<CappedSampleItem>` and to an uncapped `IFatCatCache<UncappedSampleItem>`, then prints both `GetAll().Count` values with `ConsoleLog`.
3. Route to it from `Program.cs` only when the first argument is `cache-cap`. The default behaviour of the sample is unchanged.
4. Run `dotnet run --project src/OneOffToolkitOnly -- cache-cap`. Expected output: capped count `<= 1000` (and `>= 900`), uncapped `10000`. Paste the output into the walk report.

Samples are not held to TDD, but they still follow the coding rules and must build with zero warnings.

### 3. Fog consumer check (read only; never edit Fog)

Re-run the checks in `consumer-compatibility.md` against `C:\Code\Fog` **as it is now**, and record each result:

- every `new FatCatCache<` construction still compiles against the new constructors (the parameterless constructor exists);
- every `FakeFatCatCache` usage: `VerifyAdd` / `VerifyAddList` / `VerifyDidNotAdd*` calls keep their meaning; there are no `.Cache.Add(` assertions;
- Fog's own `RegisterGeneric(typeof(FatCatCache<>))` in `Common/Common/Infrastructure/CommonModule.cs` still resolves (same shape as `ToolkitModule`, proven by phase 03's container tests).
- every Fog **production** call to `Add(List<T>, timeout)`, listed so CG-01 can review it. These items will now expire as their callers intended, where before they never did.

**Optional and human-approved only:** pack the toolkit to a local folder and build or test Fog against it. Do not do this without asking, and never change Fog's package references in its repo.

### 4. README against the code

Check every claim in the README Caching section against the final `FatCatCache.cs`: names, the about-90% low-water mark, update-never-evicts, the transient overshoot, the 30-second default sweep. Fix any README wording that drifted. Fixing the README is in this phase's footprint. Changing the code to match the README is **not**; if the code is wrong, stop and report.

### 5. Acceptance criteria roll-up

Write `src/task/todo/fatcat-cache-cap/05-walk-report.md`. List every acceptance criterion from the task spec, each with its evidence: the test class and fact name, the walk output, or the grep result. It also records:

- final test counts;
- both suite-run results;
- the Fog check;
- any unmet criterion, with the reason.

## Touches

- `src/OneOffToolkitOnly/CacheCapWorker.cs`, `src/OneOffToolkitOnly/CappedSampleItem.cs`, `src/OneOffToolkitOnly/UncappedSampleItem.cs`, `src/OneOffToolkitOnly/CacheCapSampleModule.cs` (create)
- `src/OneOffToolkitOnly/Program.cs` (modify: argument routing only)
- `README.md` (modify, only if step 4 finds drift). Hotspot.
- `src/task/todo/fatcat-cache-cap/05-walk-report.md` (create)
- Exclusive runtime resources: runs the sample host locally (no ports, no database). Reads `C:\Code\Fog`.

## Definition of Done

- [ ] Two consecutive, unfiltered, fully green `dotnet test ToolKit.slnx` runs.
- [ ] Walk output matches expectations and is pasted in the report.
- [ ] Fog check recorded.
- [ ] README matches the code.
- [ ] Every acceptance criterion ticked with evidence, or explicitly reported as unmet.
- [ ] No `VersionPrefix` change anywhere in the branch (`git diff <base>..HEAD -- '*.csproj' | grep VersionPrefix` returns nothing).
- [ ] Review loop run on the sample code.
- [ ] Exactly one commit: `fatcat-cache-cap 05: final verification (src/task/todo/fatcat-cache-cap/05-final-verification.md)`. Not pushed.

## Rollback Procedure

`git revert <phase-05 commit>`. It only removes the sample worker, the README touch-ups and the report. No manual steps.

## Hand-off

- `05-walk-report.md` is the evidence the owner reads before bumping `VersionPrefix`, publishing, and recording the version in Fog's epic tracker (Prerequisite row) and in `vendor-cost-guards-decisions.md` (F-20). Those steps are the owner's, not this plan's.
