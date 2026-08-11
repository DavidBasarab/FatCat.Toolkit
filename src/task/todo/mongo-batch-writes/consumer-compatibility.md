# mongo-batch-writes — Consumer compatibility

Verification for both consuming repositories, taken from their source on **2026-08-11** rather than
assumed.

## Summary

| Repo | Toolkit version | Uses a list overload? | Verdict |
|---|---|---|---|
| `C:\Code\Apostil` | `FatCat.Toolkit.WebServer` 1.0.344 | **Yes** — `Create(List)` and `Delete(List)` | **Safe, and the sole beneficiary.** No code change required. |
| `C:\Code\Fog` | `FatCat.Toolkit.WebServer` 1.0.339 | **No** — every call found is the single-item overload | **Unaffected.** No code change required. |

**No consumer compiles differently.** The three methods already exist on `IDataRepository<T>`
(`src/ToolKit/Data/DataRepository.cs:11,15,29`); only `MongoRepository<T>`'s implementations change.

## Why no consumer *unit test* can observe this

`MongoFakeRepository<T>` — what every consumer uses in its specs — does **not** wrap a real
`MongoRepository`:

```csharp
// src/ToolKit/Testing/MongoFakeRepository.cs:14,49
private readonly IMongoRepository<T> repository;
...
repository = A.Fake<IMongoRepository<T>>();
```

It fakes the **interface**. A consumer's `VerifyCreate`, `Items`, `FilterCapture` and friends all sit
above the level being changed, so consumer suites will neither catch a regression here nor need
updating. That cuts both ways and is stated in ADR-5: it is why consumer risk is low, and why consumer
suites are **not** evidence that this works.

## `C:\Code\Apostil` — 1.0.344

**Uses the list overloads in exactly one class**, `Api/Apostil.Api/Chunking/DocumentChunkStore.cs`:

```csharp
// line 52-56 — Delete(List<T>)
var existing = await repository.GetAllByFilter(chunk => chunk.DocumentId == documentId);

if (existing.Count > 0)
{
    await repository.Delete(existing);
}

// line 64 — Create(List<T>)
await repository.Create(chunks.ToList());
```

Every other repository call in `Apostil.Api` is a single-item overload: `AuditRecorder.Create(record)`,
`RefreshTokenStore.Create(...)` / `Delete(stored)` (a `GetByFilter` result — one item),
`DocumentUploader.Create(document)`, `DocumentIngestor.Update(ingested)`,
`MongoSystemCheck.Create/Delete(probe)`, `RecordSignalReceiptEndpoint.Create(receipt)`.
**`Update(List<T>)` is not used anywhere.**

**This is the repository that raised the task**, and the numbers in `00-overview.md` were measured
here. Both of its call sites already guard against an empty list — `Create` is only reached after
`chunks.Count == 0` returns early, and `Delete` sits inside `if (existing.Count > 0)`. So Apostil
would survive even without ADR-4's guard. **The guard is still required**, because the toolkit must not
depend on every caller happening to be careful, and because Apostil's own `Replace(id, [])` is
documented as a legal call.

**What Apostil gets:** its chunk stage drops from ~1.15 ms per chunk to roughly one round trip for the
whole set. On the measured cap-sized document that is 22,172 ms → expected low hundreds of
milliseconds. Its epic 06 re-ingest route, which will delete *and* create, gets the same on both sides.

**What Apostil must do:** bump to 1.0.345 and re-run the measurement. Nothing else.

## `C:\Code\Fog` — 1.0.339

A grep of every `repository.Create(` / `.Delete(` / `.Update(` call across `C:\Code\Fog` found **only
single-item calls** — `CreateNewBlock.Create(block)`, `SetMetaDataEndpoint.Update(stagingBlock)`,
`MarkHazeAsReadyForData.Update(stagingBlock)`, `MarkHazeAsVerifiedForBlock.Update(block)`,
`MetaTransmissionDataMessageProcessor.Update(stagingBlock)`, the `lokrRepository.Update(lokr, …)`
family, `EnqueueConsumerVerificationEndpoint.Create(job)`, and `ConsumerVerifyWorker.Update(job)`.

**Fog does not call any list overload**, so this change is inert for it in both directions.

**Version gap.** Fog is on 1.0.339 and would land on 1.0.345, spanning the `logging` hooks (340–342),
the SignalR hub improvements (343), `rate-limiting-hook` (344) and this work. Non-breaking on paper —
but that is the same conclusion `rate-limiting-hook`'s consumer-compatibility reached, and the same
caveat applies: **Fog's upgrade deserves its own build-and-smoke pass as a Fog-side task**, not an
assumption made from this repository.

## The one behavioural difference either consumer could ever see

**An empty list.** Before: silent successful no-op. After (without ADR-4's guard): a thrown driver
exception. With the guard: silent successful no-op, unchanged, and the collection is provably never
touched.

That is the whole of the compatibility surface. Everything else — return values, `EnsureCollection`
behaviour, ordered failure semantics (ADR-2), id matching (`Builders<T>.Filter.In` over unique ids) —
is preserved by construction.

**No phase in this plan is a breaking change for either repository, provided ADR-4 ships with it.**
