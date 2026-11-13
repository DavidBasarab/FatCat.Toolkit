# fatcat-cache-cap — Consumer compatibility

Checked against the source of `C:\Code\Fog` on **2026-11-13**, not assumed. Phase 05 checks again against the code as it stands then.

## Summary

| Change | Compile impact | Runtime impact | Fog verdict |
|---|---|---|---|
| New public `FatCatCacheOptions<T>` | none (additive) | none unless registered | Unaffected until CG-01 registers caps |
| New public constructors on `FatCatCache<T>`; parameterless kept | none | DI now injects `IDateTimeUtilities` (same clock) | Unaffected |
| `IFatCatCache<T>` | **unchanged** | — | Unaffected |
| Throttled sweep instead of a full scan on every call | none | Expired entries are never returned. They may stay in memory up to `SweepInterval` (30 s) unless read. | Unaffected (memory use improves under load) |
| `Add(List<T>, timeout)` now applies the timeout | none | Lists added with a timeout now expire, **as callers intended** | See "List adds" below |
| `FakeFatCatCache<T>` forwards the timeout; widened `Verify*`; new overloads | none (additive overloads) | Consumer tests asserting on `fake.Cache.Add(x, default)` would now see the real timeout | No such assertion in Fog |
| Test assembly signed / `InternalsVisibleTo` | none for consumers | none | n/a |

## `C:\Code\Fog`

- **Toolkit version in use:** `FatCat.Toolkit` / `FatCat.Toolkit.WebServer` **1.0.354** in the current projects. Some older projects pin 1.0.240 / WebServer 1.0.64; they are untouched by this change until they upgrade.
- **Registration:** `Common/Common/Infrastructure/CommonModule.cs:89` re-registers `builder.RegisterGeneric(typeof(FatCatCache<>)).As(typeof(IFatCatCache<>)).SingleInstance();`. This is the same shape as `ToolkitModule`, and Autofac's most-parameters constructor selection behaves the same through either one. Phase 03's container tests cover this shape.
- **Direct construction:** `Brume/Tests.Brume/Users/DesktopLogin/DesktopNonceServiceSpecs/DesktopNonceServiceTests.cs:22` (`new FatCatCache<DesktopNonce>()`) and `Brume/Tests.Brume/Users/PassKeys/Reset/PasskeyResetTokensSpecs/PasskeyResetTokensTests.cs:21` (`new FatCatCache<PasskeyResetCacheItem>()`). The parameterless constructor is kept, so these compile. They now use the real clock through `DateTimeUtilities`, which is what they did before.
- **`FakeFatCatCache<T>`:** 19 test files use it. `VerifyAdd(item)` is called in `GetBlockByIdMessageProcessorTests.cs:52`, `ReadyForBlockRetrievalMessageProcessorTests.cs:44` and `HazeConnectionMessageProcessorTests.cs:36`. With the matcher widened to `A<TimeSpan?>._`, they keep passing whether or not the production code passes a timeout.
- **`fake.Cache.Add(...)` assertions:** none found (`grep -rn "\.Cache\.Add(" --include=*.cs`).
- **Direct `A.Fake<IFatCatCache<T>>()` assertions** (for example `ClientConnectedTests`, `NewDownloadTests`, `SendTests`): they do not go through `FakeFatCatCache<T>`, so they are unaffected.

### List adds

Any Fog production call to `Add(List<T>, timeout)` that relied, unknowingly, on items **never** expiring will now see them expire. That is the bug fix the brief asks for. Phase 05 greps Fog for `.Add(` calls with a `List<` argument and a timeout, and lists them in the walk report so CG-01 can review them.

## What Fog does next (CG-01, not this plan)

Take the released version and register `FatCatCacheOptions<T>` with a `MaximumItems` for: `IpGeoLocationCacheItem`, `TwilioLookupCacheItem`, `ReversePhoneCacheItem`, `PhoneVerificationCodeCacheItem`, `VerifyNonce`, `FindLokrNonce`, `DesktopNonce`, `TNonce`, `ZipCodeInfo`. Leave the connection, transfer and block caches unbounded.
