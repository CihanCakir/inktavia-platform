# BE — Vessel document/media stale read cache (register/approve not reflected)

Symptom: after uploading/approving a vessel document (or media), the admin documents list shows the old data until the
15-min cache expires. FE React-Query invalidation is correct; the staleness is in the **module distributed read cache**.

## Root cause (verified)
`GetVesselDocumentsQueryHandler` (and `GetVesselMediaQueryHandler`) are `IAizenQueryHandlerCacheable`
(Distributed, 15 min). Their cache key is derived from the **full** query parameter set:
`GetVesselDocumentsQuery(VesselId, PageIndex, PageSize, IncludeAccessUrls, AccessUrlExpiresInMinutes)`.

The admin BFF reads with **`IncludeAccessUrls=true`** and **`AccessUrlExpiresInMinutes=60`**
(`GetVesselDocumentsBff` / `GetVesselMediaBff` pass `includeAccessUrls: true`), so the cached entry is keyed on
`(VesselId, 0, 20, true, 60)`.

But `VesselCacheInvalidationService.InvalidateDocumentsAsync` (called by Add/Update/Remove/Approve/Register) removes a
key built **without** `IncludeAccessUrls` / `AccessUrlExpiresInMinutes` (following the owners/engines pattern of
`(VesselId, PageIndex, PageSize)` only). Key ≠ key → the eviction misses the entry the BFF actually reads → **stale**.

Two problems, really: (1) key mismatch, and (2) caching a response that embeds **time-limited presigned URLs** is
unsafe anyway — a cached URL can expire before the 15-min cache entry does.

## Fix (preferred: don't cache the presigned variant)
1. In `GetVesselDocumentsQueryHandler` / `GetVesselMediaQueryHandler`, **bypass the cache when `IncludeAccessUrls == true`**
   (presigned URLs must be fresh). Only cache the metadata-only variant (`IncludeAccessUrls=false`). Implement via the
   cacheable-handler's skip mechanism (return not-cacheable when IncludeAccessUrls is true), or split the read so the
   URL presigning happens outside the cached metadata read.
   - This removes the expired-URL-in-cache hazard **and** means the admin BFF (which always requests access URLs) never
     hits a stale cached entry.
2. Fix the invalidation key regardless: make `InvalidateDocumentsAsync` / `InvalidateMediaAsync` remove the **exact**
   key the read handler uses. If any cached variant keeps `IncludeAccessUrls`/`AccessUrlExpiresInMinutes` in the key,
   the eviction must include them (or use a pattern/prefix removal for the `GetVesselDocumentsQueryHandler:VesselId=…`
   namespace so all parameter permutations are cleared).
3. Confirm `RegisterVesselDocument` / `RegisterVesselMedia` / `ReplaceVesselDocument` / `ApproveVesselDocument` all call
   the (now-correct) invalidation — the report showed a fresh upload only appeared after a manual Redis flush.

## Verify
- Upload a document via the admin two-step flow → it appears in `GET …/documents` **immediately** (no manual Redis
  flush, no 15-min wait). Same for media upload, replace (new version), approve (status flips immediately).
- Confirm no presigned URL is served from cache past its expiry (the IncludeAccessUrls=true path is uncached).
- Metadata-only reads (if any internal caller uses IncludeAccessUrls=false) still cache + invalidate correctly.
- `dotnet build` 0 errors; add/adjust a cache-invalidation unit test if the module test project supports it.
- **DO NOT COMMIT.**
