# Phase 04 — README: Caching section

**Risk: Low.** Documentation only. The root `README.md` is also the NuGet package readme (`ToolKit.csproj` packs `..\..\README.md`), so consumers read it on nuget.org.

- **Depends on:** 01 (the `FatCatCacheOptions<T>` contract), 02 (the new `Verify*` overloads).
- **Depended on by:** 05.
- **Wave:** 2.
- **Parallel with:** 03. Disjoint writes: this phase writes only `README.md`; 03 writes only `src/ToolKit/Caching/**` and `src/Tests.ToolKit/Caching/**`. This phase is the only one in the wave that edits a hotspot. It documents the **specified** behaviour (task spec R1–R3), not 03's internals; phase 05 reconciles any drift.

## Read first

1. `src/task/todo/fatcat-cache-cap.md`: R1, R2, R3, R9.
2. `src/task/todo/fatcat-cache-cap/00-overview.md`: ADR-1 and ADR-2.
3. The root `README.md` (the "## Caching" section, around line 266) and `src/ToolKit/Caching/FatCatCacheOptions.cs` as phase 01 left it.

## Goal

Update the "## Caching" section of `C:\Code\FatCat.Toolkit\README.md`:

1. In the existing sample, replace `TimeSpan.FromMinutes(5)` with `5.Minutes()` and add `using Humanizer;`. Keep the sample otherwise as it is.
2. Add a short paragraph on **dependency injection**: `IFatCatCache<T>` is registered by `ToolkitModule` as one shared instance per `T`, so inject it rather than `new` it in container-built classes. Direct `new FatCatCache<T>()` still works.
3. Add a **"Bounded caches"** subsection covering:
   - Caches are unbounded by default. Opt a type in by registering `FatCatCacheOptions<T>` in your own Autofac module (show the one-line `RegisterInstance` example with `MaximumItems`).
   - When a new key arrives at a full cache, expired entries are removed first, then the oldest entries by entry time, down to about 90% of the cap. Updating an existing key never evicts.
   - The cap may be exceeded briefly by at most the number of concurrent writers.
   - Use a cap for caches keyed by caller-controlled values (IP addresses, phone numbers, nonces). Never cap caches holding live connections or in-flight work.
   - Expired entries are never returned. They are physically removed when read, or by a sweep that runs at most once per `SweepInterval` (default 30 seconds, configurable on the options).
4. If the README documents the testing helpers (`FatCat.Toolkit.Testing`), add the `FakeFatCatCache<T>` timeout behaviour and the `VerifyAdd(item, timeout)` / `VerifyAddList(items, timeout)` overloads there. If it does not, do not invent a section; say so in the report.

Keep the tone and density of the surrounding README. Code samples follow this repo's C# rules (no expression bodies, Humanizer durations).

## Touches

- `README.md` (repo root; modify). Hotspot.
- Exclusive runtime resources: none.

## Verification

- Every type and member named in the README exists with that exact name: `FatCatCacheOptions<T>`, `MaximumItems`, `SweepInterval`, `ToolkitModule`, `VerifyAdd`, `VerifyAddList`. Grep `src/` for each.
- `cd src && dotnet build ToolKit.slnx` still succeeds (the README is packed).
- `TimeSpan.From` no longer appears in the Caching section.

## Definition of Done

- [ ] README Caching section updated as above. Nothing outside the Caching (and, if present, testing-helpers) section changed.
- [ ] Names verified against the code.
- [ ] Code-quality gates that apply to a docs-only change: build passes. The `unit-test-review` / `code-review` / `code-security-review` loop is run and reports no findings for a docs-only diff. Record that in the report.
- [ ] Exactly one commit: `fatcat-cache-cap 04: README caching section (src/task/todo/fatcat-cache-cap/04-readme-caching.md)`. Not pushed.
- [ ] Phase report written.

## Rollback Procedure

`git revert <phase-04 commit>`, after reverting 05 if it exists. No manual steps.

## Hand-off

- README documents the bounded-cache contract. Phase 05 checks it against the final code.
