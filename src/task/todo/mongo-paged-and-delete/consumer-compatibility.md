# mongo-paged-and-delete — Consumer compatibility

Verification for both consuming repositories, taken from their source on **2026-08-17** rather than
assumed.

## Summary

| Repo | Toolkit version | Implements `IMongoRepository<T>` itself? | Verdict |
|---|---|---|---|
| `C:\Code\Apostil` | `FatCat.Toolkit.WebServer` 1.0.348 | **No** — every reference injects it | **Safe. It is the requester**, and its `repository_paged_queries` work item is blocked until this ships. |
| `C:\Code\Fog` | `FatCat.Toolkit.WebServer` (see below) | **No** — no `: IMongoRepository` implementation found at last check | **Unaffected.** No code change required; its version gap deserves its own build-and-smoke pass. |

**The change is additive.** Adding members to an interface only breaks code that *implements* it, and
neither consumer does — both inject `IMongoRepository<T>` and use `MongoFakeRepository<T>` in tests.
Adding a brand-new public type (`PagedResults<T>`) breaks nothing.

## `C:\Code\Apostil` — 1.0.348

Every reference is a constructor injection of the interface. **No class in `Apostil.Api`,
`Apostil.Common`, or `Apostil.Site` implements `IMongoRepository<T>`**, and its own coding rules forbid
faking the interface (*"use the concrete `MongoFakeRepository<T>`"*), so its test projects do not
implement it either.

**What Apostil gets, and why it asked:** three endpoints/stores load a whole Mongo collection into memory
to keep a slice — `GetAuditRecordsEndpoint` (a collection D12 exempts from the purge, so it never
shrinks), `GetSignalReceiptsEndpoint`, and `DocumentChunkStore.Page` — and a fourth, `DocumentChunkStore
.Replace`, loads a document's whole chunk set only to delete it. `QueryByFilter` and `DeleteByFilter`
replace all four with server-side calls.

**What Apostil must do:** bump `Api/Apostil.Api/Apostil.Api.csproj` from 1.0.348 to 1.0.349. Nothing else
— its new code is written against the new members from the start.

**Blocking relationship, stated plainly:** `tasks/todo/repository_paged_queries/` in that repository
declares this a **hard precondition** (its ADR-1). Its orchestrator checks for `QueryByFilter` /
`DeleteByFilter` before phase 1 and **stops without touching the repository** if they are absent.

## `C:\Code\Fog`

No `: IMongoRepository` implementation was found at the last cross-repo check (recorded by
`mongo-count-and-distinct`'s compatibility note). Fog neither pages nor deletes-by-filter today and gains
nothing and loses nothing from this change.

**Version gap.** Fog trails Apostil's toolkit version and would span several intervening work items on any
upgrade. Non-breaking on paper — and the same caveat every prior toolkit task recorded still applies:
**Fog's upgrade deserves its own build-and-smoke pass as a Fog-side task**, not an assumption made from
this repository. Re-verify Fog's `: IMongoRepository` count before publishing if any doubt remains.

## The one way this could break a consumer

**A consumer that implements `IMongoRepository<T>` in its own code** — a bespoke repository, a hand-rolled
test double, a decorator — stops compiling until it implements the two new members. Neither consumer does.
**A third consumer that does would fail at compile time, loudly, which is the failure mode to prefer.**

Everything else is preserved by construction: no existing signature, body, or return value changes (ADR-2
/ overview), `Collection` on the fake stays `null`, `PagedResults<T>` is new so it collides with nothing,
and no behaviour of any existing member moves.
