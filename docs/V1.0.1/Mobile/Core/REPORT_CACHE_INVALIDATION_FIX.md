# REPORT — Cache-invalidation key alignment (module ↔ framework read-cache key)

**Scope:** platform framework helpers (Core.Security key builder + Core.Cache eviction) + Vessel module (the reported bug) + ReferenceData module (same class, de-dup).
**Goal:** after a write, the corresponding LIST read is fresh immediately — **without** disabling caching, shortening TTLs, or flushing on read. Correct only the *eviction key* so writes invalidate the right entry.
**Status:** fixed + **live-verified** on the running stack (vessel-api rebuilt/redeployed; Redis DB 11).

---

## 1. Root cause — invalidation targeted the wrong physical Redis key (TWO independent mismatches)

The read path stores each cacheable query result in Redis and the invalidation path tries to delete it. They diverged on **two** independent axes — either one alone makes the delete miss, so both had to be fixed.

### The framework READ/WRITE key (source of truth)
1. `AizenQueryHandlerDecorator` computes a **query key**:
   `{HandlerTypeName}:{ sha256_lowerhex(propString) }`, where `propString` reflects over **every** non-`QueryId` property in declaration order as `"{PropName}_{Value}|"`.
2. It then stores via Microsoft's `RedisCache` (`SetAsync`), which **prepends the configured `DistributedCache:InstanceName`** to the key. So the **physical** Redis key is:

```
{InstanceName}{HandlerTypeName}:{sha256hex(propString)}
   e.g.  Vessel:GetUserVesselsQueryHandler:{hash of  UserId_x|PageIndex_0|PageSize_20|}
```

(Confirmed live: real keys in Redis DB 11 are `Vessel:GetVesselDetailQueryHandler:…`, `Vessel:GetUserVesselsQueryHandler:…`; ReferenceData in DB 12 are `ReferenceData:…`.)

### Mismatch A — propString content (the M4b-reported half)
`VesselCacheInvalidationService` hand-rolled SHA256 over only a **subset** of props:

```csharp
HandlerKey("GetUserVesselsQueryHandler", $"UserId_{userId}|")   // ← PageIndex/PageSize MISSING
```

The hashing bytes were correct, but the paged props were omitted → different hash → delete misses. Single-prop queries (`VesselId`-only: detail/by-id/by-code/spec/location) coincidentally matched; every **paged** query (list, owners, engines, status-history, documents, media) diverged. Matches the field report: create→detail immediate, create→list stale.

### Mismatch B — the InstanceName prefix (found at runtime; M4b's diagnosis missed this)
Invalidation deleted through `RemoveNoHash`, which uses a **separate raw StackExchange connection that does NOT prepend `InstanceName`**. So even with a correct hash it asked Redis to delete `GetUserVesselsQueryHandler:{hash}` while the read path had stored `Vessel:GetUserVesselsQueryHandler:{hash}` — a different physical key. **This made ALL invalidations miss, including the single-prop ones that "looked" correct.** (Live negative control in §4 reproduces this exactly: an unprefixed delete returns `del=0` and the key survives.)

**Divergent-key mismatch, paged queries (Mismatch A):**

| Query handler | Framework propString | Old module propString |
|---|---|---|
| `GetUserVesselsQueryHandler` | `UserId_x\|PageIndex_0\|PageSize_20\|` | `UserId_x\|` |
| `GetVesselOwnersQueryHandler` | `VesselId_x\|PageIndex_0\|PageSize_20\|` | `VesselId_x\|` |
| `GetVesselEnginesQueryHandler` | `VesselId_x\|PageIndex_0\|PageSize_20\|` | `VesselId_x\|` |
| `GetVesselStatusHistoryQueryHandler` | `VesselId_x\|PageIndex_0\|PageSize_20\|` | `VesselId_x\|` |
| `GetVesselDocumentsQueryHandler` | `VesselId_x\|PageIndex_0\|PageSize_20\|IncludeAccessUrls_False\|AccessUrlExpiresInMinutes_15\|` | `VesselId_x\|` |
| `GetVesselMediaQueryHandler` | `VesselId_x\|PageIndex_0\|PageSize_20\|IncludeAccessUrls_False\|AccessUrlExpiresInMinutes_15\|` | `VesselId_x\|` |

---

## 2. Scope decision — framework helpers; invalidation was per-module & hand-rolled

- **Read key generation:** centralized in the CQRS decorator (one place). Modules re-implemented invalidation keys by hand → drift.
  - `VesselCacheInvalidationService` — **buggy** on Mismatch A (+ B).
  - `ReferenceDataCacheInvalidationService` — propStrings were correct (it enumerated `OnlyActive_True|`/`False|` etc.) but it duplicated the SHA256 primitive **and** was silently broken by Mismatch B (its `ReferenceData:`-prefixed reads were never evicted either).
  - (Identity OTP/rate-limit + FileStorage use `SetNoHash`/`GetNoHash`/`RemoveNoHash` as a self-consistent **unprefixed** trio — those are correct as-is and must stay raw; see §3.)

Both the key format and the prefix live in the framework, so the fix adds **one canonical key generator** and **one prefix-aware eviction method**, and routes both modules through them. Framework-helper change (platform blast radius) → full-solution build + cross-module spot-check (§5).

---

## 3. The change (minimal, architecture-preserving)

**New — `Core/Security/.../AizenQueryCacheKey.cs`** (single source of truth for the query key):
- `ForQuery(handlerName, query)` — reflects over a query instance (read path). The decorator's original algorithm was **moved here verbatim** → read keys byte-identical.
- `For(handlerName, params (name, value)[])` — for invalidators without a query instance; builds the identical propString + hash.
- `FromPropString(handlerName, propString)` — pre-built-propString overload; empty → `"{handler}:"`.

**`AizenQueryHandlerDecorator`** — `GetCacheKey` now delegates to `AizenQueryCacheKey.ForQuery`. No format change.

**`Core/Cache/.../AizenDistributedCache.cs` + `IAizenDistributedCache`** — new `RemoveReadCacheEntry(key)` that deletes the **`InstanceName`-prefixed** physical key (captures `options.Value.InstanceName`, deletes `_instanceName + key` on the same Redis DB the read path uses). `RemoveNoHash` is left untouched — the raw-key consumers (Identity OTP tickets/rate-limits, FileStorage) rely on its unprefixed semantics.

**`VesselCacheInvalidationService`** — every method now: `RemoveReadCacheEntry( AizenQueryCacheKey.For(...) )` with the **full** prop list in declaration order and the **read-path default values** (`PageIndex 0`, `PageSize 20`, `IncludeAccessUrls false`, `AccessUrlExpiresInMinutes 15`). Fixes Mismatch A (full props) + B (prefix). Stops hand-rolling SHA256.

**`ReferenceDataCacheInvalidationService`** — `HandlerKey`/`HandlerKeyNoProps` delegate to `AizenQueryCacheKey`; all deletes switched to `RemoveReadCacheEntry`. Fixes its latent Mismatch B and removes the duplicated primitive; call sites/behavior otherwise unchanged.

Caching stays fully ON; TTLs unchanged; no read-time flush; UoW/save untouched.

### Paged-list caveat (documented, not a silent cap)
Paged reads cache one entry per `(pageIndex, pageSize, …)` tuple; the read path has no wildcard eviction. Each method evicts the **default first-page variant** the mobile/list surfaces request. Non-default pages fall back to their TTL. (Mirrors ReferenceData's existing documented behavior.)

---

## 4. Evidence

### 4a. Byte-level key-equivalence (root cause + no regression) — `scratchpad/keyproof`
A harness reimplemented the **pre-refactor** decorator and the **old** hand-rolled module key and compared to the new canonical builder. All pass:

```
PASS  GetUserVessels: read key == new .For(full props)
PASS  GetUserVessels: OLD buggy module key MISSED the read key      ← reproduces Mismatch A
PASS  GetVesselDocuments: read key == new .For (5 props incl bool False)
PASS  GetVesselDetail: read key == new .For
PASS  GetVesselDetail: OLD single-prop module key == new .For        ← single-prop hash unchanged
PASS  Decorator refactor byte-identical: ForQuery == old CreateCacheKey  ← no platform read-cache regression
PASS  ReferenceData: FromPropString == old HandlerKey
PASS  ReferenceData: no-props key == "{handler}:"
8 passed, 0 failed
```

### 4b. Live — invalidation key == real physical Redis key (incl. prefix)
A real read of vessel 100009 produced the Redis key
`Vessel:GetVesselDetailQueryHandler:72dd2ba38d5a878485be66e1732d9b73a89728c88f73819b08d356245de9ec39`.
The invalidation builder computes, **byte-identical**:
`"Vessel:" + AizenQueryCacheKey.For("GetVesselDetailQueryHandler", ("VesselId", 100009))` → same string. ✅

### 4c. Live — create evicts the correct key; caching stays selective (Redis DB 11)
```
seed  Vessel:GetUserVesselsQueryHandler:{hash(UserId_100029|PageIndex_0|PageSize_20|)}   exists=1
POST /api/v1/mobile/vessels  → vessel-api module creates vessel (owner UserId=100029, Active; rows 100010/100011 in DB)
                               → CreateVesselCommandHandler.InvalidateUserVesselListAsync(100029)
AFTER create:  list-key(100029) exists=0   ← EVICTED ✅
               detail-key         exists=1   ← survived → selective, cache still ON
```
(The BFF create *wrapper* returns failure only because it resolves the new id by re-reading the caller's list, which is empty under a **separate, pre-existing identity quirk** — see §6; the vessel-api module create + invalidation ran and committed regardless.)

### 4d. Live — negative control (why the OLD code missed = Mismatch B)
```
del  GetUserVesselsQueryHandler:{hash}        (old: unprefixed)  → del=0, key SURVIVES ❌  ← the bug
del  Vessel:GetUserVesselsQueryHandler:{hash} (new: prefixed)    → del=1, key GONE     ✅
```

### 4e. Live — cache still works (second identical read served from cache)
```
GET /vessels/100009  read#1 → +1 "FROM vessel.vessels" DB SELECT (miss → query → cache)
GET /vessels/100009  read#2 → +0 DB SELECT             (served from cache ✅)
detail key present after both reads.
```

---

## 5. Cross-module note (framework helpers changed)
- **Full solution build:** `dotnet build Aizen.sln -c Release` → **0 errors**.
- **Single interface implementer:** `AizenDistributedCache` is the only `IAizenDistributedCache`; the new method compiles platform-wide.
- **ReferenceData spot-check:** its `ReferenceData:`-prefixed reads (DB 12) were also silently un-evicted by Mismatch B; now fixed via `RemoveReadCacheEntry` (keys byte-identical to before — §4a). Its cached reads stay cached.
- **Read path unchanged platform-wide:** `ForQuery == old CreateCacheKey` (§4a) → every module's existing cache entries remain valid; no key churn on deploy.
- **Likely same latent bug elsewhere (not in scope, worth auditing):** `FileStorage`'s `FileCacheInvalidationService` invalidates read-decorator caches via `RemoveNoHash` (unprefixed). If FileStorage sets a `DistributedCache:InstanceName`, its invalidations miss for the same reason — recommend switching it to `RemoveReadCacheEntry`.

---

## 6. Out-of-scope observation (not a cache issue)
The mobile GET `/vessels` list returns 0 for `qa.owner.aug5` even for a freshly-created, correctly-owned vessel (DB has `UserId=100029, OwnershipStatus=Active`; a fresh, never-cached page-size read still returns 0). The list read's resolved `CurrentUserId` differs from the `UserId=100029` the create path writes — a **create-vs-read identity-resolution mismatch**, independent of caching, which also causes the BFF create wrapper's id-resolution to fail (§4c). Flagged for a separate ticket; it does not affect the cache-invalidation fix (verified above at the Redis-key level).

---

## 7. Files touched (isolated commit)
- `Core/Security/src/Aizen.Core.Security/AizenQueryCacheKey.cs` *(new)*
- `Core/CQRS/src/Aizen.Core.CQRS/Decorator/AizenQueryHandlerDecorator.cs`
- `Core/Cache/src/Aizen.Core.Cache.Abstraction/IAizenDistributedCache.cs`
- `Core/Cache/src/Aizen.Core.Cache/AizenDistributedCache.cs`
- `Modules/Vessel/src/Aizen.Modules.Vessel.Repository/Service/VesselCacheInvalidationService.cs`
- `Modules/Vessel/src/Aizen.Modules.Vessel.Repository/Aizen.Modules.Vessel.Repository.csproj`
- `Modules/ReferenceData/src/Aizen.Modules.ReferenceData.Repository/Service/ReferenceDataCacheInvalidationService.cs`
- `Modules/ReferenceData/src/Aizen.Modules.ReferenceData.Repository/Aizen.Modules.ReferenceData.Repository.csproj`
- `docs/V1.0.1/Mobile/Core/REPORT_CACHE_INVALIDATION_FIX.md` *(this report)*

No vessel-feature or UoW code in this diff.
