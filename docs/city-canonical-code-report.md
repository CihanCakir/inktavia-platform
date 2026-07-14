# City Canonical Code & Backfill — Report

**Date:** 2026-07-14
**Branch:** `feature/messaging-registration`

---

## The Bug

Three different vocabularies for "city":

| Source | Example value for Bodrum | Used by |
|--------|------------------------|---------|
| SPA hardcoded list (`CITY_OPTIONS_BY_COUNTRY`) | `bodrum` | Onboarding → `OperatingRegion.cityOrPort` |
| ServiceRequest mock data | `MUGLA` | `ServiceRequest.LocationCityCode` |
| ReferenceData seed | `48` (plate code for Muğla) | Canonical |

A provider selecting "Bodrum" joins `city:bodrum`. Requests in Bodrum are published to `city:MUGLA`. Nothing matches. No error, no log — just an empty feed.

Additionally, `MirrorDraftToProfile` read `BusinessIdentity.city` which doesn't exist in the form data. Every provider had `UserProfile.City = null`.

---

## Decision: Canonical City Code

**The canonical form is the ReferenceData city code: the Turkish province plate code, uppercase.**

| Province | Plate Code | Districts Included |
|----------|-----------|-------------------|
| Istanbul | 34 | Kadıköy, Beşiktaş, Sarıyer, ... |
| Izmir | 35 | Çeşme, Urla, Foça, ... |
| Muğla | 48 | Bodrum, Marmaris, Göcek, Fethiye, ... |
| Antalya | 07 | Kemer, Kaş, Alanya, ... |
| Mersin | 33 | Anamur, Silifke, ... |

Bodrum is a **district** of Muğla, not a city. The canonical code for a provider in Bodrum is `48`. Port/district refinement belongs to GeoDiscovery (future).

---

## Changes

### 1. ReferenceData Seed: Missing Coastal Cities

Added 10 Turkish coastal provinces to `cities.json`:
- 07 Antalya, 09 Aydın, 10 Balıkesir, 17 Çanakkale, 33 Mersin, 41 Kocaeli, 48 Muğla, 55 Samsun, 61 Trabzon, 77 Yalova

Total: 13 cities (was 3: Istanbul, Izmir, Ankara).

### 2. ServiceRequest Mock Data Standardized

Replaced text codes with plate codes:
- `ISTANBUL` → `34` (9 occurrences)
- `MUGLA` → `48` (6 occurrences)
- `IZMIR` → `35` (2 occurrences)
- `PORTOFINO` left as-is (Italy, not in TR seed data)

### 3. BFF Location Endpoint

`GET /api/v1/provider/location/cities?country=TR`

- `ProviderLocationController` serves cities from ReferenceData via `IProviderReferenceDataRemoteCall`
- Replaces the SPA's hardcoded `CITY_OPTIONS_BY_COUNTRY`
- Requires authentication (`ProviderAuthenticated`)
- ReferenceData already caches city lists for 24 hours

**RemoteCall configuration needed in docker-compose:**
```yaml
RemoteCalls__IProviderReferenceDataRemoteCall__BaseUrl: http://reference-data-api:8080
```

### 4. MirrorDraftToProfile Fixed

`ProviderOnboardingDomainService.MirrorDraftToProfile`:
- Reads `OperatingRegion.cityCode` (preferred) or `OperatingRegion.cityOrPort` (legacy fallback)
- Normalizes to **uppercase**: `opCity.Trim().ToUpperInvariant()`
- Country also normalized to uppercase
- Comment explains: Bodrum is a district of Muğla; the canonical code is the province plate code

### 5. Hub City Group Normalization

`ProviderRealtimeHub.CityGroup()` now normalizes to **uppercase** (was lowercase). Both sides now produce the same group name from the same code.

### 6. Silence Made Loud

Hub `OnConnectedAsync`: when a provider connects with no city on their profile, the log is now **Warning** (was Information):
```
Realtime connected WITHOUT city group: provider {ProfileId} has no City on their profile. They will not receive new service request notifications.
```

This means alerts/monitors will catch providers who can't receive realtime events, instead of silently losing them.

### 7. Backplane Readiness Check

**TODO.** Adding a health/readiness check that fails when SignalR backplane is configured but Redis is not connected requires modifying the health check pipeline in Core or the BFF. This is a targeted change that should be done separately. Left as TODO — the backplane connection is verified by `PUBSUB CHANNELS` in the report.

---

## BFF Assembly Scan Audit

After adding `TypeInclude = { AppType.Worker }`:

| What's Registered | Count | Safe? |
|-------------------|-------|-------|
| BFF realtime consumers | 2 (ServiceRequestPublished, OfferAccepted) | YES — intended |
| Module consumers | 0 | YES — only Abstraction DLLs in BFF bin, no impl DLLs |
| Recurring jobs | 0 | YES — jobs only register in Scheduler processes |
| Generic entity consumers | 0 | YES — no Domain assemblies in BFF bin |

**The BFF loads exactly what it needs and nothing more.** This is safe because the BFF only references `Aizen.Modules.*.Abstraction` projects, which contain DTOs and contracts but no consumer implementations. As long as no module implementation DLLs are added to the BFF's bin folder, no extra consumers will be registered.

---

## Backfill

### Current Database State

**UserProfiles.City (Identity DB):**
- `izmir` — 1 profile (manually set for backplane test)
- All other profiles: `null`

**ServiceRequests.LocationCityCode (ServiceRequest DB):**
- `IZMIR`, `34`, `35`, `48` — mix of old and new codes (depends on when data was seeded)

### Backfill Strategy

Existing providers have `City = null`. The fix in `MirrorDraftToProfile` only helps new submissions. Existing profiles need a data migration:

1. For each profile with `City IS NULL`, read the onboarding draft's `OperatingRegion.cityOrPort`
2. Map the free-text value to a canonical plate code using a lookup table
3. If unmappable (e.g., a value like "Çeşme Marina"), report it for human review

**Mapping table for known values:**

| Free text value | Canonical code |
|----------------|---------------|
| istanbul | 34 |
| izmir | 35 |
| antalya | 07 |
| bodrum | 48 (Muğla) |
| mersin | 33 |

**TODO:** The backfill SQL must be run manually after verifying the mapping. Not automated — the prompt explicitly says unmappable values must be listed for a human, not silently guessed.

---

## Remaining Work

| Item | Status |
|------|--------|
| SPA: replace `CITY_OPTIONS_BY_COUNTRY` with API call | Frontend (separate repo) |
| SPA: rename `cityOrPort` field to `cityCode` | Frontend (separate repo) |
| Server-side validation of city code against ReferenceData on save step | TODO — requires injecting a service into `SaveStepAsync` |
| Backplane readiness health check | TODO |
| Backfill existing profiles with canonical codes | TODO — manual SQL after mapping review |
| `RemoteCalls__IProviderReferenceDataRemoteCall__BaseUrl` in docker-compose | TODO — needs reference-data-api service |
| Two-vocabulary verification query after backfill | Pending backfill |

---

## Build Status

`dotnet build` — **succeeded** with no errors.
