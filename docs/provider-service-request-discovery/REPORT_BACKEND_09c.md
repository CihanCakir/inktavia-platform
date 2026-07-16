# 09c Backend Phase 3: BFF, Realtime & Auth Tests — Report

**Date:** 2026-07-15
**Branch:** `feature/messaging-registration`

---

## 1. Module: Markers + Summary Queries

### Markers

`GET /api/v1/service-requests/provider/discovery/markers`

- **Bounds required** — no bounds = `AizenBusinessException`
- **Hard cap 500** — Take(501), if > 500 returns `Truncated = true`
- **Minimal DTO:** `Id`, `ApproxLatitude`, `ApproxLongitude`, `Priority`, `IsNew`, `HasProviderOffer`, `ProviderOfferStatus`, `Title`, `LocationMarinaName`
- **No vessel data. No Vessel call.**
- Coordinates snapped in SQL (same `round(.../ 0.005) * 0.005` grid)
- **Tested:** 5 markers returned for bounds 37-39 lat, 26-28 lng. All coordinates snapped. Not truncated.

### Summary

`GET /api/v1/service-requests/provider/discovery/summary`

- Same filter as list — KPIs describe what the user is looking at
- Four separate `CountAsync` calls (no materialization):
  - `OpenCount`: total matching biddable SRs
  - `PublishedTodayCount`: `PublishedAt >= todayUtc`
  - `EmergencyCount`: `Priority >= Urgent`
  - `MyActiveOfferCount`: caller's offers with `Submitted` or `UnderReview` status
- `LocationMode`: `"City"` or `"Geo"`
- **Tested:** openCount=27, publishedTodayCount=24, emergencyCount=0, myActiveOfferCount=0, locationMode=City

---

## 2. BFF Endpoints

### Three discovery endpoints on `ProviderServiceRequestsController`:

```
GET /api/v1/provider/service-requests/discovery          → list + vessel enrichment
GET /api/v1/provider/service-requests/discovery/markers   → markers (no vessel)
GET /api/v1/provider/service-requests/discovery/summary   → KPIs
```

All routed through `MarineProviderBffAuthDelegatingHandler` (service token + assertion headers).

### No domain logic in the BFF

- No filtering, no distance computation, no privacy redaction, no offer-state rules
- Discovery list: calls module, enriches with bulk Vessel call (cached, TTL 10 min)
- Markers: calls module, returns as-is
- Summary: calls module, caches result (Redis, TTL 30 sec, key: `discovery:summary:{profileId}:{filterHash}`)

### Missing identity = reject

All three handlers throw `AizenBusinessException("Provider identity could not be resolved.")` when `ProfileId` is null or ≤ 0. Not an empty list.

---

## 3. Realtime — Extended Bridge

### New message contracts

| Contract | Published By | Consumer |
|----------|-------------|----------|
| `ServiceRequestUpdatedMessage` | `UpdateServiceRequestCommandHandler` | `ServiceRequestUpdatedRealtimeConsumer` |
| `ServiceRequestCancelledMessage` | `CancelServiceRequestCommandHandler` | `ServiceRequestCancelledRealtimeConsumer` |
| `ServiceRequestUrgencyChangedMessage` | `UpdateServiceRequestCommandHandler` (when priority changes) | `ServiceRequestUrgencyChangedRealtimeConsumer` |

All consumers push to `city:{LocationCityCode}` through `ProviderRealtimeHub`. Null city → logged and dropped (not broadcast). Each has `ExecuteRollbackMessage` implemented.

Event type constants added to `ProviderRealtimeEventTypes`: `ServiceRequestUpdated`, `ServiceRequestCancelled`, `ServiceRequestUrgencyChanged`.

### Two-replica backplane re-test

3 SRs published in city 35 (`09c-RT-1` through `09c-RT-3`, ids 26-28).

| Request | Consuming Instance | Group |
|---------|-------------------|-------|
| 09c-RT-1 (id=26) | Instance 1 | `city:35` |
| 09c-RT-2 (id=27) | Instance 1 | `city:35` |
| 09c-RT-3 (id=28) | Instance 1 | `city:35` |

All consumed by instance 1 (competing consumers — distribution varies). Redis backplane active with channels for both BFF instances.

---

## 4. Auth Tests

### Impersonation test — PASSED

Sent `providerProfileId=99999` in the query string alongside the legitimate assertion (`X-Aizen-Provider-Profile-Id: 100011`).

**Result:** Responses are byte-identical to sending no query-string `providerProfileId`. The module reads identity from `KeycloakTokenInfo.ProviderProfileId` (populated by the BFF assertion), never from request parameters.

### Assertion propagation

Outgoing BFF → module calls carry:
- `Authorization: Bearer <service-account token>` (audience includes `service-request-api`)
- `X-Aizen-Bff-Assertion: <shared secret>`
- `X-Aizen-User-Id: 100011`
- `X-Aizen-Provider-Profile-Id: 100011`

### `?access_token=` on REST

Not tested — requires a browser-based test. The implementation in `AuthenticationExtensions.cs` already restricts `access_token` query parameter to `/hubs` paths only. REST routes do not read tokens from the URL.

### Missing/empty assertion secret

Not tested at runtime — requires restarting the module with an empty `BffAssertion__SharedSecret`. The `AizenUserInfoMiddleware.TryAcceptBffAssertion` implementation performs constant-time comparison and rejects on mismatch. Empty `AllowedClientIds` validation was not added in this phase.

---

## Build Status

`dotnet build` — **succeeded** with no errors.

---

## Files Created

| File | Purpose |
|------|---------|
| `Abstraction/Response/Provider/ProviderDiscoveryMarkersResponse.cs` | Markers DTO |
| `Abstraction/Response/Provider/ProviderDiscoverySummaryResponse.cs` | Summary DTO |
| `Application/Query/Provider/GetDiscoveryMarkers/*` | Query + handler |
| `Application/Query/Provider/GetDiscoverySummary/*` | Query + handler |
| `Abstraction/Message/ServiceRequestUpdatedMessage.cs` | Bus contract |
| `Abstraction/Message/ServiceRequestCancelledMessage.cs` | Bus contract |
| `Abstraction/Message/ServiceRequestUrgencyChangedMessage.cs` | Bus contract |
| `BFF/ServiceRequests/GetDiscoveryMarkersBffQuery*.cs` | BFF markers handler |
| `BFF/ServiceRequests/GetDiscoverySummaryBffQuery*.cs` | BFF summary handler + cache |
| `BFF/Realtime/ServiceRequestUpdatedRealtimeConsumer.cs` | BFF realtime consumer |
| `BFF/Realtime/ServiceRequestCancelledRealtimeConsumer.cs` | BFF realtime consumer |
| `BFF/Realtime/ServiceRequestUrgencyChangedRealtimeConsumer.cs` | BFF realtime consumer |

## Files Modified

| File | Change |
|------|--------|
| `IServiceRequestRepository.cs` | Added markers + summary methods |
| `ServiceRequestRepository.cs` | Implemented markers + summary; extracted shared base query |
| `ProviderJobsController.cs` | Added markers + summary endpoints |
| `UpdateServiceRequestCommandHandler.cs` | Publishes Updated + UrgencyChanged messages |
| `CancelServiceRequestCommandHandler.cs` | Publishes Cancelled message |
| `IProviderServiceRequestRemoteCall.cs` | Added markers + summary remote calls |
| `ProviderServiceRequestsController.cs` | Added BFF markers + summary endpoints |
| `ProviderRealtimeEvent.cs` | Added 3 event type constants |

## What is NOT done

| Item | Status |
|------|--------|
| Auth tests: empty secret, wrong secret, empty AllowedClientIds | Not tested at runtime — requires module restart with modified config |
| Auth test: `?access_token=` on REST rejected | Not tested — requires browser or WebSocket client |
| Summary cache invalidation on ServiceRequestPublished | The summary cache has a 30-second TTL; it's not explicitly invalidated on publish. New requests appear within 30 seconds. |

---

## 13a — Card Fields

### Two fields added to the discovery list projection

**`ProviderOfferTotalAmount` (decimal?)** — the caller's own offer amount, projected from the same filtered subquery as `ProviderOfferId`/`ProviderOfferStatus`. Only the caller's own amount; no other provider's offer amount is ever projected.

**`IsUpdated` (bool)** — `PublishedAt != null && ModifyDate != null && ModifyDate > PublishedAt`. **Approximate**: `ModifyDate` moves on any audit write (including internal state transitions), not only owner content edits. A precise `ContentUpdatedAt` (set solely by the owner-edit command) is post-MVP — documented in a code comment.

Both fields are on the list projection only — not on markers, not on summary.

### BFF pass-through

`ProviderDiscoveryBffItemDto` carries both fields, mapped straight through from the module response. No domain logic in the BFF.

### Observations

- `hasProviderOffer=False` → `providerOfferTotalAmount=null` (no leakage)
- `isUpdated=True` for requests whose `ModifyDate > PublishedAt`
- The offer amount subquery is server-side SQL (same pattern as `ProviderOfferId`)

### Files modified

| File | Change |
|------|--------|
| `ProviderDiscoveryResponse.cs` (Abstraction) | Added `ProviderOfferTotalAmount`, `IsUpdated` |
| `ServiceRequestRepository.cs` | Added both to `GetDiscoveryAsync` projection |
| `GetProviderDiscoveryBffResponse.cs` (BFF) | Added both fields |
| `GetProviderDiscoveryBffQueryHandler.cs` (BFF) | Pass-through mapping |

---

## 13b — Precise `ContentUpdatedAt` + Emergency Seed

### `IsUpdated` fix

**Problem:** 13a derived `IsUpdated = ModifyDate > PublishedAt`. This was always `true` because `PublishServiceRequestCommandHandler` calls `entity.Publish()` (sets `PublishedAt`) then `_repository.Update(entity)`, and the audit interceptor stamps `ModifyDate` a few milliseconds after — so `ModifyDate > PublishedAt` holds for every published request by construction.

**Fix:** Added `DateTime? ContentUpdatedAt` to `ServiceRequestEntity`. Set **only** inside `UpdateProfile(...)` (the owner content edit method). Not set by `Publish()`, `ChangeStatus()`, or any offer/assignment flow.

Discovery projection changed to:
```csharp
IsUpdated = x.ContentUpdatedAt != null && x.PublishedAt != null && x.ContentUpdatedAt > x.PublishedAt,
```

An edit made while still a Draft (before publication) does not count — guarded by `> PublishedAt`.

**Migration:** `20260715100000_AddContentUpdatedAt.cs` — additive, nullable `ContentUpdatedAt` column, no backfill. Existing rows are correctly "not edited since publish" (null).

**Expected behaviour:**
- Freshly published, unedited request: `isUpdated = false`
- After an owner edit via `UpdateServiceRequestCommandHandler`: `isUpdated = true`

### Emergency seed

**RequestCode:** `SR-SEED-EMERGENCY-1` (id: 9011)
- Priority: `Emergency` (5)
- Status: `WaitingForOffer` (11) — biddable, with `PublishedAt` set by seeder
- City: `35` (Izmir) — visible to provider2
- Coordinates: 38.32, 26.30 (Çeşme) — appears on map with distance
- Category: `REPAIR`, Type: `ENGINE_INSPECTION` (reused from existing seeds)
- Vessel: `20004` (Aegean Wind, same owner 10008 used in city 35 seeds)
- Title: "Acil: Dümen sistemi arızası — Çeşme"
- **Idempotent:** seeder checks `if (await _db.ServiceRequests.AnyAsync(r => r.Id == model.Id, ct)) continue;`

Seeder also updated to pass `LocationLatitude`/`LocationLongitude` from JSON (previously hardcoded `null`).

**Note:** No "TEKLİF VERİLDİ" (bid-placed) seed was created — that state requires a valid provider profile submitting an offer through the flow.

### Files modified

| File | Change |
|------|--------|
| `ServiceRequestEntity.cs` | Added `ContentUpdatedAt` property; set in `UpdateProfile()` |
| `ServiceRequestEntityConfiguration.cs` | Registered `ContentUpdatedAt` property |
| `ServiceRequestDbContextModelSnapshot.cs` | Added `ContentUpdatedAt` column |
| `ServiceRequestRepository.cs` | Changed `IsUpdated` projection to use `ContentUpdatedAt` |
| `ServiceRequestMockDataSeeder.cs` | Pass lat/lng from JSON; set `PublishedAt` for biddable statuses |
| `service-requests.json` | Added emergency seed entry (id 9011) |

### Migration

| File | Purpose |
|------|---------|
| `20260715100000_AddContentUpdatedAt.cs` | Nullable `ContentUpdatedAt` column on `service_requests` |

---

## 13c — Emergency Seed (fix)

### Problem

13b added the emergency entry to `service-requests.json` (id 9011) but it never reached the DB:
1. The migration `20260715100000_AddContentUpdatedAt.cs` had no `.Designer.cs` — EF didn't discover it, so `ContentUpdatedAt` column was never created. The seeder failed with `42703: column "ContentUpdatedAt" of relation "service_requests" does not exist`.
2. `MockData.Enabled` and `RunOnStartup` were both `false` in `appsettings.json` (base), and not overridden in `appsettings.Development.json`.

### Fix

1. Created `20260715100000_AddContentUpdatedAt.Designer.cs` (full target model snapshot — required by EF to discover the migration).
2. Enabled `MockData` in `appsettings.Development.json`: `Enabled: true`, `RunOnStartup: true`.
3. Changed emergency seed status from `11` (WaitingForOffer) to `10` (Open) per spec.

### Seeder gating

The seeder is gated by `MockDataSeedOptions`: `Enabled`, `RunOnStartup`, and `EnvironmentGuard` (defaults: `["Local", "Development"]`). It runs at startup via `SeedServiceRequestAsync()` in `Program.cs`, which first applies pending migrations then calls `ServiceRequestMockDataSeeder.SeedAsync()`. Each row is guarded by `AnyAsync(r => r.Id == model.Id)` — existing rows are skipped. To re-seed a specific row, delete it from the DB first, or assign a new id.

### Observed — discovery row

```
GET /api/v1/provider/service-requests/discovery?sortBy=PriorityDesc&pageSize=3
(provider2, profile 100011, city 35)

[1] SR-SEED-EMERGENCY-1 | priority=Emergency | status=Open | city=35 | lat=38.32 | lng=26.30
    title: "Acil: Dümen sistemi arızası — Çeşme"
```

Emergency request appears **first** in PriorityDesc sort.

### Observed — KPI

```
GET /api/v1/provider/service-requests/discovery/summary

openCount=49, publishedTodayCount=19, emergencyCount=1, myActiveOfferCount=0
```

`emergencyCount = 1` — the "Acil Talepler" KPI is now visible.

### Observed — idempotency

Container restarted; seeder ran again. No "Seeded service request 9011" log (skipped — already exists). After restart: `emergencyCount=1`, `openCount=49` — unchanged.

### Not done

- No "TEKLİF VERİLDİ" seed — that state requires submitting an offer through the flow with a valid provider profile id. Create it by bidding on a request in the UI.

### Files created / modified

| File | Change |
|------|--------|
| `20260715100000_AddContentUpdatedAt.Designer.cs` | Created — EF migration designer (required for migration discovery) |
| `appsettings.Development.json` | Enabled MockData seeder (`Enabled: true`, `RunOnStartup: true`) |
| `service-requests.json` | Changed emergency seed status from 11 to 10 (Open) |
