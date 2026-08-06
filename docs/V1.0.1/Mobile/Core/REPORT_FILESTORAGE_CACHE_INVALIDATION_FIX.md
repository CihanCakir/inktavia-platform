# REPORT — FILESTORAGE_CACHE_INVALIDATION_AUDIT (Mismatch-B check)

**Goal:** confirm whether FileStorage's `FileCacheInvalidationService` has the same **Mismatch-B** the Vessel/ReferenceData fix (commit `e1899af`) corrected — a READ-QUERY cache invalidated via a raw, un-prefixed StackExchange delete that misses the decorator's `{InstanceName}{Handler}:{sha256}` physical key, leaving reads stale to TTL — and if so, fix it with the canonical `AizenQueryCacheKey` + `RemoveReadCacheEntry` pattern, **without** breaking the legit raw (un-prefixed) uses.

**Result: NO real Mismatch-B. Nothing changed.** FileStorage performs **no read-query caching whatsoever** — no query handler is cacheable, nothing writes the `filestorage:*` keys, and the running Redis holds zero FileStorage cache keys. The `FileCacheKeyService` + `FileCacheInvalidationService` are scaffolding for a read-cache that was never wired; their `RemoveNoHash` deletes hit keys that are never populated (harmless no-ops). Applying the prefixed fix here would be **actively wrong** — see §4.

**Scope of change:** this report only. No code/config/TTL/UoW touched.

---

## 1. How the read-cache actually works (the discriminator)

The CQRS read decorator (`AizenQueryHandlerDecorator`, Core.CQRS) caches a query **only if** its handler implements `IAizenQueryHandlerCacheable`; the store key is `AizenQueryCacheKey.ForQuery(HandlerName, request)` → `{Handler}:{sha256(propString)}`, physically prefixed with the module's Redis `InstanceName` on write. **Mismatch-B only exists when such a decorator-cached read is invalidated by a raw hand-rolled key** (the Vessel/ReferenceData case).

If a module instead caches *manually* (`_cache.SetAsync(cacheKeyService.X(...))`) and deletes with `RemoveNoHash(cacheKeyService.X(...))`, the keys match byte-for-byte — that is a **legit raw** use and must stay `RemoveNoHash`.

So the audit reduces to one question per invalidation: **is the key it deletes ever written, and by what?**

---

## 2. Per-invalidation audit (`FileCacheInvalidationService`)

| Invalidation | Key (`FileCacheKeyService`) | Read that would populate it | Is that read cached? | Classification |
|---|---|---|---|---|
| `InvalidateFileAsync` | `filestorage:file:id/guid/code:{…}` | `GetFileByIdQueryHandler` | **No** — not `IAizenQueryHandlerCacheable`; no manual `SetAsync` | phantom key (never written) |
| `InvalidateOwnerFilesAsync` | `filestorage:owner:{mod}:{type}:{id}:files` | `GetFilesByOwnerQueryHandler` | **No** — not cacheable; no manual set | phantom key |
| `InvalidateUploadSessionAsync` | `filestorage:session:{code}` | (upload-session read) | **No** cacheable handler / no set | phantom key |
| `InvalidateReadUrlAsync` | `filestorage:readurl:{guid}:{userId}` | `CreateReadUrlCommandHandler` → `FileAccessService.CreateReadUrlAsync` | **No** — the presigned URL is regenerated on every call; nothing is cached | phantom key |
| `InvalidateProcessingJobsAsync` | `filestorage:jobs:{fileId}` | `GetFileProcessingJobsQueryHandler` | **No** — not cacheable; no set | phantom key |

**None** targets a decorator-cached read (no Mismatch-B), and **none** targets an existing manual cache either — the keys are simply never written.

### Static evidence
- `grep -r "IAizenQueryHandlerCacheable" Modules/FileStorage/src` → **0 matches**. All 6 query handlers (`GetFileById`, `GetFilesByOwner`, `GetFileMetadata`, `GetFileProcessingJobs`, `FilterFiles`, `ValidateFileOwnership`) are plain `AizenQueryHandler` → the decorator's `_isCacheable` is `false` for every one → the read path never calls `SetAsync`.
- `grep -r "Cacheable"` in FileStorage → **0**. No `SetAsync` / `GetOrSet` / `.Set(` anywhere in FileStorage source (excluding the key/invalidation services). `FileAccessService` and `FileRepository` do no caching.
- The invalidation service **is** invoked (from `FileStorageService`, `FileOwnershipService`, `FileProcessingService`, `FileVirusScanRequestedConsumer`) — so the deletes run on writes; they just target keys nothing sets.

### Runtime evidence (shared Redis, FileStorage `InstanceName: "FileStorage:"`, DB 13)
```
redis-cli -n 13 --scan --pattern '*File*'      → (empty)
redis-cli -n 13 --scan --pattern 'FileStorage*' → (empty)
redis-cli -n 13 --scan --pattern 'filestorage:*'→ (empty)
redis-cli -n 0  (same patterns)                 → (empty)
DB13 dbsize = 3 → only:
  provider:otplogin:rl:…            ← Identity OTP rate-limit (legit raw)
  admin:otplogin:rl:…               ← Identity OTP rate-limit (legit raw)
  inktavia:admin-panel-bff:keycloak-service-token:…  ← service token (legit raw)
```
No FileStorage read-query key exists at runtime — matching the static finding. (Runtime was checked because, per the vessel fix, the `InstanceName` prefix only appears at runtime; here there is nothing to prefix.)

---

## 3. Contrast with the genuine Mismatch-B (why FileStorage differs)

Vessel/ReferenceData reads **were** decorator-cached under `{InstanceName}{Handler}:{sha256}`, but invalidated via a hand-rolled key + raw delete → the physical key survived → stale reads to TTL. FileStorage has **no decorator-cached read** (no cacheable handler) and **no manual cache**, so there is no surviving physical key and no staleness: FileStorage reads always hit the DB and are **always fresh**. The premise that avatar re-upload (M3c) / vessel docs+media (M4e/M4f) go stale via a *FileStorage* cache does not hold — if such staleness is ever observed it originates in the **consuming** side (e.g. a profile module caching the avatar `fileId`, or a BFF response cache), not in FileStorage. That is out of this audit's scope.

---

## 4. Why NOT to "fix" it anyway

Rebuilding these on `AizenQueryCacheKey` + `RemoveReadCacheEntry` would:
- build keys as `{InstanceName}{HandlerName}:{sha256}` for handlers that **never write** such a key → still deletes nothing, and
- silently imply a caching contract that doesn't exist, and
- add the `FileStorage:` prefix that corresponds to no stored entry.

Leaving them as raw `RemoveNoHash(customKey)` is the correct shape for the manual caches these keys were *designed* for — so if anyone later marks a FileStorage read cacheable **via the same custom key** (manual `SetAsync`), the existing raw deletes already match. (If instead they mark it cacheable via the decorator, the invalidator must then move to `RemoveReadCacheEntry` — but that is a change to make *with* that future caching change, not now.)

---

## 5. Verification against the task checklist

- **Freshness:** N/A / trivially satisfied — reads are uncached, so a replaced file is reflected on the very next read (no TTL, no flush). No cache stands between the read and the DB.
- **Cache still works:** there is no read-query cache to preserve; nothing was disabled.
- **Legit un-prefixed uses intact:** confirmed present and untouched — Identity OTP/rate-limit keys (`provider:otplogin:rl:*`, `admin:otplogin:rl:*`) and the admin-panel-bff service token live in DB 13 written raw; no code touched, so `RemoveNoHash` semantics for these are unchanged.
- **Isolated change:** none — `git status` shows only this report.

**Conclusion:** No Mismatch-B in FileStorage. No code change. The invalidation service's raw deletes are correct for its (currently unwired) manual-cache design; the prefixed fix does not apply here.
