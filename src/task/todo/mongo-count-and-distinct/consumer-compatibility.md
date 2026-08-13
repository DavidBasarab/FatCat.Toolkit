# mongo-count-and-distinct — Consumer compatibility

Verification for both consuming repositories, taken from their source on **2026-08-12** rather than
assumed.

## Summary

| Repo | Toolkit version | Implements `IMongoRepository<T>` itself? | Verdict |
|---|---|---|---|
| `C:\Code\Apostil` | `FatCat.Toolkit.WebServer` 1.0.345 | **No** — every reference injects it | **Safe. It is the requester**, and its `embedding_model_consistency` work item is blocked until this ships. |
| `C:\Code\Fog` | `FatCat.Toolkit.WebServer` 1.0.344 (`Common.WebServer`) | **No** — no `: IMongoRepository` implementation found | **Unaffected.** No code change required. |

**The change is additive.** Adding a member to an interface only breaks code that *implements* it, and
neither consumer does — both inject `IMongoRepository<T>` and use `MongoFakeRepository<T>` in tests.

## `C:\Code\Apostil` — 1.0.345

Every reference is a constructor injection of the interface (measured):

```
Api/Apostil.Api/Chunking/DocumentChunkStore.cs:35        IMongoRepository<ChunkData>
Api/Apostil.Api/Status/Checks/MongoSystemCheck.cs:9      IMongoRepository<SystemCheckProbeData>
Api/Apostil.Api/Auditing/Endpoints/GetAuditRecordsEndpoint.cs:11   IMongoRepository<AuditRecordData>
… plus DocumentUploader, DocumentIngestor, DocumentChunkLister, DocumentReingestor, RefreshTokenStore,
   RecordSignalReceiptEndpoint — all injected, none implementing.
```

**No class in `Apostil.Api`, `Apostil.Common`, or `Apostil.Site` implements `IMongoRepository<T>`**, and
its own coding rules forbid faking the interface (*"use the concrete `MongoFakeRepository<T>`"*), so its
test projects do not implement it either.

**What Apostil gets, and why it asked:** its I6 consistency check needs the distinct embedding-model
names recorded on a library scope's chunks, with a count each. The only shape available today loads every
matching chunk — **11,312 bytes each, measured, at a documented ceiling of ~50,000 chunks**, on a query
that would run on every upload and every admin status check. With these two methods the same answer is
`1 + k` small server-side queries.

**What Apostil must do:** bump `Api/Apostil.Api/Apostil.Api.csproj` to 1.0.346. Nothing else — no code
of its own changes because of this work item; its new code is written against the new members from the
start.

**Blocking relationship, stated plainly:** `tasks/todo/embedding_model_consistency/` in that repository
declares this a **hard precondition**. Its orchestrator checks for `CountByFilter` / `DistinctByFilter`
before phase 1 and **stops without touching the repository** if they are absent.

## `C:\Code\Fog` — 1.0.344

No `: IMongoRepository` implementation anywhere. Fog does not call any filtered count or distinct today
and gains nothing and loses nothing.

**Version gap.** Fog is on 1.0.344 and would land on 1.0.346, spanning `mongo-batch-writes` (345) and
this work. Non-breaking on paper — and the same caveat `mongo-batch-writes` recorded still applies:
**Fog's upgrade deserves its own build-and-smoke pass as a Fog-side task**, not an assumption made from
this repository. (`Spikes/dude2` pins 1.0.64 and is not a product project; it is left alone.)

## The one way this could break a consumer

**A consumer that implements `IMongoRepository<T>` in its own code** — a bespoke repository, a
hand-rolled test double, a decorator — stops compiling until it implements the two new members. Neither
consumer does. **A third consumer that does would fail at compile time, loudly, which is the failure
mode to prefer.**

Everything else is preserved by construction: no existing signature, body, or return value changes
(ADR-2), `Collection` on the fake stays `null`, and no behaviour of any existing member moves.
