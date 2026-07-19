# CI-1b — provider-scope the CargoDry Overview & Alerts (security correctness)

The provider endpoints now authenticate (fix 3), but the **handlers ignore `ProviderProfileId`** and return
**platform-global** data. A provider currently sees every provider's kit counts and alerts. Close this before CI-2.

Confirmed current state:
- `CargoDryProviderController` correctly resolves the provider id from the BFF assertion and passes it:
  `GetCargoDryOperationalOverviewQuery { ProviderProfileId = pid }` / `...AlertsQuery { ProviderProfileId = pid, ... }`.
- Both **queries already carry `long? ProviderProfileId`** — nothing to change there.
- Both **handlers drop it on the floor** — every repository call is unfiltered.
- `CargoDryKitEntity.ProviderProfileId` (`long?`) exists — the column to filter on.
- The overview **cache key is static** (`cargodry:operational:overview`) → providers and admin share one blob.

## Scope
Purely handler + repository changes. No controller, DTO, contract, BFF, or Keycloak change. Admin callers pass
`ProviderProfileId = null` and must keep getting **identical global** results (backward-compatible).

## 1. Repository — add an optional provider filter (default `null` = global/admin)
`ICargoDryKitRepository` + `CargoDryKitRepository` — add `long? providerProfileId = null` (last param, before
`CancellationToken`) to the four methods the two handlers use, and apply the filter only when set:

- `GetStatsAsync(long? providerProfileId = null, CancellationToken ct = default)`
- `GetExpiringAsync(int withinDays, long? providerProfileId = null, CancellationToken ct = default)`
- `GetExpiredUnmarkedAsync(long? providerProfileId = null, CancellationToken ct = default)`
- `GetPagedAsync(... existing filters ..., long? providerProfileId = null, CancellationToken ct = default)`

Implementation pattern — build the base query, then conditionally filter:
```csharp
IQueryable<CargoDryKitEntity> q = _db.Kits;
if (providerProfileId.HasValue) q = q.Where(x => x.ProviderProfileId == providerProfileId.Value);
```
- `GetStatsAsync`: derive **all** `CountAsync` calls from this filtered `q` (not `_db.Kits`), so Total/Available/
  Active/Expiring/Expired/Revoked/TodayActivations/WithRenewals are all provider-scoped.
- `GetExpiring`/`GetExpiredUnmarked`/`GetPaged`: add the same `if (providerProfileId.HasValue)` predicate to the
  existing `Where` chain.
- Default `null` ⇒ no predicate ⇒ existing admin behavior byte-for-byte. Do not change other repo methods.

## 2. Overview handler — thread the id through + scope the cache key
`GetCargoDryOperationalOverviewQueryHandler`:
- Pass `request.ProviderProfileId` into `GetStatsAsync`, `GetExpiringAsync(30, request.ProviderProfileId, ct)`,
  and both `GetPagedAsync(... , request.ProviderProfileId, ct)` (CommercialReviewRequired + Lost).
- **Cache key must include the scope** or a provider will serve admin's blob (and vice-versa):
  ```csharp
  var scope = request.ProviderProfileId?.ToString() ?? "global";
  var cacheKey = $"cargodry:operational:overview:{scope}";
  ```
  Use `cacheKey` for both `TryGetAsync` and `SetAsync`. Keep the 2-minute TTL.
- **Global-only counters that the provider board does not display must not leak platform scale.** When
  `request.ProviderProfileId.HasValue`, set `TotalBatches`, `ActiveBatches`, and `RecentLifecycleEventCount` to
  `0` (they are platform-level; provider-scoped batch allocation + lifecycle telemetry are post-MVP —
  `ProviderAllocatedBatches` is the correct future field). For the admin/global path leave them as they are today.
- `OpenOperationalAlertCount` needs no special-casing — it becomes provider-scoped automatically once the
  underlying `GetStats`/`GetExpiring`/`GetPaged` calls are scoped.

## 3. Alerts handler — thread the id through
`GetCargoDryOperationalAlertsQueryHandler`: pass `request.ProviderProfileId` into `GetExpiringAsync`,
`GetExpiredUnmarkedAsync`, and both `GetPagedAsync` (CommercialReviewRequired + Revoked). No cache here. The
per-kit `MapAlert` already carries `ProviderProfileId` — leave the mapping as-is.

## Cache invalidation note
Kit mutations (activate/renew/revoke/allocate) already invalidate the old static overview key. Update those
invalidations to also clear the **provider-scoped** key for the affected kit's `ProviderProfileId`
(`cargodry:operational:overview:{pid}`) in addition to `:global`. If invalidation is broad/prefix-based, ensure
it covers the new `:{scope}` suffix. (If the current code just lets the 2-min TTL lapse, keep that behavior but
clear both `:global` and `:{pid}` on mutation for freshness.)

## Acceptance (verify on screen + via the endpoints)
- **provider2** `GET /provider/cargodry/overview` returns counts that are a **strict subset** of admin's global
  overview (only provider2's kits). Alerts list contains only provider2's kits.
- **Admin** CargoDry dashboard (global, `ProviderProfileId = null`) returns the **same** totals as before — no
  regression.
- Two different providers see **different** overviews (no cross-tenant bleed via the cache key).
- Batch counters + recent-lifecycle count read `0` on the provider board; unchanged on admin.
- `tsc`/build + module builds; admin CargoDry endpoints unaffected.

## Report
Append to `REPORT_BACKEND.md` ("CI-1b"): overview/alerts now filter by `ProviderProfileId`; repo methods gained
an optional provider filter (default null = admin/global); overview cache key scoped per provider; non-displayed
global counters zeroed under provider scope. Note the provider board is now tenant-safe.
