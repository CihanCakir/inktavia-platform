# City Codes: Close the Loop — Report

**Date:** 2026-07-14
**Branch:** `feature/messaging-registration`

---

## What Was Broken

The previous pass changed the backend to plate codes (`34`, `35`, `48`) but left the SPA sending lowercase city names (`istanbul`, `izmir`, `bodrum`). `MirrorDraftToProfile` uppercased whatever it received → `UserProfile.City = "IZMIR"`. Requests were published to `city:35`. The groups never matched.

**Every newly onboarded provider received nothing.** Same bug, new spelling.

---

## Fixes Applied

### 1. SPA: Hardcoded City List → API (separate repo)

**Repo:** `/Users/cihancakir/Desktop/Mine/DEV/inktavia-marine-provider-web`

- Deleted `CITY_OPTIONS_BY_COUNTRY` and `getCityOptions` from `onboardingFieldConfigs.ts`
- `OperatingRegionPage.tsx`: city select now fetches from `GET /api/v1/provider/location/cities?country={code}` via React Query. Option value = `cityCode` (e.g. `"48"`), label = `name` (e.g. `"Muğla"`)
- Renamed field `cityOrPort` → `cityCode` in:
  - `onboardingTypes.ts` (type definition)
  - `onboardingSchemas.ts` (validation schema)
  - `OperatingRegionPage.tsx` (form field)
  - `ReviewSubmitPage.tsx` (review display)

### 2. Backend: Free-Text Door Closed

`MirrorDraftToProfile` in `ProviderOnboardingDomainService`:
- **Removed** the legacy `cityOrPort` fallback — only reads `OperatingRegion.cityCode`
- **Removed** the `BusinessIdentity.city` path (that field never existed in the form data)
- Normalizes to uppercase: `cityCode.Trim().ToUpperInvariant()`
- Comment explains Bodrum is a district of `48`, port-level matching is GeoDiscovery

`SubmitAsync` validation:
- Added check: if `OperatingRegion` exists but `cityCode` is missing/empty, submission is rejected with "OperatingRegion: city code is required."

### 3. Backfill

| Profile | Before | After | Source |
|---------|--------|-------|--------|
| 100009 | `null` | `34` | Draft had `cityOrPort: "istanbul"` → mapped to plate code 34 |
| 100011 | `izmir` | `35` | Hand-set during backplane test → corrected to plate code 35 |
| All others | `null` | `null` | No onboarding draft with city data; need to re-onboard |

Draft JSON also updated: `cityOrPort` key replaced with `cityCode` carrying the plate code.

**Unmappable profiles:** All profiles with `City = null` and no onboarding draft city value. These are profiles that never completed the OperatingRegion step. They will get a city code when they (re-)submit onboarding with the updated SPA.

### 4. Hub City Group Normalization

`ProviderRealtimeHub.CityGroup()` normalizes to `ToUpperInvariant()`. Both the profile (`UserProfile.City`) and the SR (`LocationCityCode`) store uppercase codes. The group key for Izmir is `city:35` on both sides.

### 5. Warning on Missing City

Hub `OnConnectedAsync` logs at **Warning** when a provider has no city:
```
Realtime connected WITHOUT city group: provider {ProfileId} has no City on their profile.
They will not receive new service request notifications.
```

---

## Vocabulary Agreement

**UserProfiles.City (distinct):**
```
34
35
```

**ServiceRequests.LocationCityCode (distinct):**
Empty — no live service requests in the DB (mock data is in seed JSON, not in the active DB).

**Mock data (seed JSON):**
All entries now use plate codes: `34`, `35`, `48`. The only non-TR entry is `PORTOFINO` (Italy).

**Both sides now use the same vocabulary: ReferenceData province plate codes, uppercase.**

---

## Backplane Test Status

The backplane was proven working in the previous test with `city:izmir`. After the backfill, profile 100011 has `City = 35`. When the browser reconnects:
- The hub will join `city:35`
- Service requests with `LocationCityCode = 35` will be pushed to `city:35`
- The groups match

**Browser reconnection required** to verify the hub log shows `city 35`. The DB is correct; the Identity API will return the updated value.

---

## Build Status

`dotnet build` — **succeeded** with no errors.

---

## Files Changed

### Backend (addesso-project)
| File | Change |
|------|--------|
| `ProviderOnboardingDomainService.cs` | Removed legacy `cityOrPort` fallback; only reads `cityCode`; added submit validation |
| `ProviderRealtimeHub.cs` | Warning log for missing city; uppercase normalization |
| `cities.json` (ReferenceData seed) | 13 Turkish coastal cities (was 3) |
| `service-requests.json` (SR mock data) | `ISTANBUL→34`, `MUGLA→48`, `IZMIR→35` |

### SPA (inktavia-marine-provider-web)
| File | Change |
|------|--------|
| `onboardingFieldConfigs.ts` | Deleted `CITY_OPTIONS_BY_COUNTRY`, `getCityOptions` |
| `onboardingTypes.ts` | Renamed `cityOrPort` → `cityCode` |
| `onboardingSchemas.ts` | Renamed `cityOrPort` → `cityCode` |
| `OperatingRegionPage.tsx` | Fetch cities from API; field renamed to `cityCode` |
| `ReviewSubmitPage.tsx` | Display uses `cityCode` |

### Database
| Table | Change |
|-------|--------|
| `UserProfiles` (100009) | `City: null → 34` |
| `UserProfiles` (100011) | `City: izmir → 35` |
| `provider_onboarding` (100009, 100011) | Draft `cityOrPort` → `cityCode` with plate codes |
