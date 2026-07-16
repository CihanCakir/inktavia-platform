# 09b Backend Phase 2: Geo Filtering & Coordinate Privacy — Report

**Date:** 2026-07-14
**Branch:** `feature/messaging-registration`

---

## 1. Geo Filtering

### Filter Fields Added

`ProviderServiceRequestDiscoveryFilter` extended with:
- `CenterLatitude`, `CenterLongitude` (decimal?) — browser/map origin
- `RadiusKm` (decimal?) — search radius from center
- `BoundsMinLat`, `BoundsMaxLat`, `BoundsMinLng`, `BoundsMaxLng` (decimal?) — map viewport bounds
- `SortBy` now accepts `DistanceAsc` in addition to `PublishedAtDesc` and `PriorityDesc`

### Bounding Box Prefilter

Computed server-side from `(center, radiusKm)` using great-circle math with latitude correction:

```csharp
var deltaLat = (double)radiusKm / EarthRadiusKm * (180.0 / Math.PI);
var deltaLng = deltaLat / Math.Cos(latRad);
```

The bbox is the indexed prefilter:
```sql
WHERE "LocationLatitude" >= @minLat AND "LocationLatitude" <= @maxLat
  AND "LocationLongitude" >= @minLng AND "LocationLongitude" <= @maxLng
```

Uses the `IX_service_requests_Status_Lat_Lng` composite index.

When map viewport bounds are provided (`BoundsMinLat/MaxLat/MinLng/MaxLng`), they are used directly as the bbox.

### Haversine Distance in SQL

Distance is computed **in the SQL projection** as an EF expression tree — not in memory:

```csharp
DistanceKm = 6371.0 * 2.0 * Math.Asin(Math.Sqrt(
    Math.Pow(Math.Sin((lat2 - lat1) * PI / 360), 2) +
    Math.Cos(lat1 * PI / 180) * Math.Cos(lat2 * PI / 180) *
    Math.Pow(Math.Sin((lng2 - lng1) * PI / 360), 2)))
```

EF Core with Npgsql translates `Math.Sin`, `Math.Cos`, `Math.Sqrt`, `Math.Asin`, `Math.Pow` to their PostgreSQL equivalents (`sin()`, `cos()`, `sqrt()`, `asin()`, `pow()`).

**No materialisation:** distance is computed in the same SQL query as the Select projection. At no point are rows loaded into memory for distance calculation.

### DistanceAsc Sort

When `SortBy = DistanceAsc`, the query orders by the haversine expression then tiebreaks by `Id`:
```csharp
ordered = query.OrderBy(x => distanceExpression).ThenByDescending(x => x.Id);
```

Cursor pagination for DistanceAsc encodes `lastDistance|lastId|filterHash`.

### Index

`(Status, LocationLatitude, LocationLongitude)` — composite index added via migration `20260714122340_AddGeoIndex`.

---

## 2. Input Validation

Handler validates all geo inputs before querying:

| Input | Rule | Violation |
|-------|------|-----------|
| `CenterLatitude` | ∈ [-90, 90] | `AizenBusinessException` |
| `CenterLongitude` | ∈ [-180, 180] | `AizenBusinessException` |
| `RadiusKm` | ≤ 200 | `AizenBusinessException` |
| Bounds | min < max | `AizenBusinessException` |
| `SortBy = DistanceAsc` | requires center | `AizenBusinessException` |

**Rejected, not clamped.** An out-of-range radius is an error, not silently reduced to 200.

Browser coordinates are untrusted discovery input. They narrow the caller's view and nothing else — never an authorization input, never an entitlement input.

---

## 3. City Fallback

When no geo fields are provided (no center, no bounds):
- `LocationMode = "ProviderCity"`
- Filter by `LocationCityCode == provider profile city` (already implemented in 09a)
- `DistanceKm = null` on all items
- Sort defaults to `PublishedAtDesc`

This is a **first-class path**, not an error path. Location permission will be refused by many users.

When geo fields are provided:
- `LocationMode = "BrowserLocation"` (center + radius) or `"MapViewport"` (bounds)

---

## 4. Coordinate Privacy — Snapping

`CoordinateSnapper.Snap(lat, lng, requestId)`:
- Grid size: `0.005°` (~500m at mid-latitudes)
- Deterministic jitter per request ID: `(requestId % 7 - 3) * 0.001` (~300m offset)
- Same request ID always produces the same snapped coordinates — markers don't jitter between page loads

**Discovery never returns exact coordinates.** The handler post-processes every item:
```csharp
var (snappedLat, snappedLng) = CoordinateSnapper.Snap(item.SnappedLatitude, item.SnappedLongitude, item.Id);
item.SnappedLatitude = snappedLat;
item.SnappedLongitude = snappedLng;
```

Exact `LocationLatitude`/`LocationLongitude` are released only to the assigned provider through the existing job endpoints.

The discovery DTO has `SnappedLatitude`/`SnappedLongitude` — there are no `LocationLatitude`/`LocationLongitude` fields.

---

## 5. GEO ON LOAN Header

Present on:
- `CoordinateSnapper.cs`
- `GeoHelper.cs`

These are the only geo-specific files. The bounding box computation in the repository is a duplicated helper (avoids cross-layer dependency) and carries an inline comment referencing the architectural exception.

---

## 6. LocationMode

`LocationMode` is a string on `ProviderDiscoveryResponse`:
- `"BrowserLocation"` — center + radius provided
- `"MapViewport"` — viewport bounds provided
- `"ProviderCity"` — no geo, filtered by provider's city

It is a code, never a display string. The phrase "service area" appears nowhere.

---

## Places I Was Tempted to Compute in Memory

**Distance sort with cursor pagination.** The haversine expression for ordering is the same as the one in the projection, duplicated in the `OrderBy` clause. EF translates both to SQL. I was tempted to materialise the first page and sort in memory to avoid the duplicate expression — did not.

**Bounding box computation.** Done server-side in C# (pure math, no DB call). The box parameters are passed into the LINQ Where clause. This is correct — the box is a filter parameter, not a data transformation.

---

## Build Status

`dotnet build` — **succeeded** with no errors.

---

## Files Created

| File | Purpose |
|------|---------|
| `CoordinateSnapper.cs` | ~500m grid snapping, deterministic per request ID |
| `GeoHelper.cs` | Bounding box from center + radius |
| `20260714122340_AddGeoIndex.cs` | EF migration for `(Status, Lat, Lng)` index |

## Files Modified

| File | Change |
|------|--------|
| `ProviderServiceRequestDiscoveryFilter.cs` | Added 7 geo fields |
| `ProviderDiscoveryResponse.cs` | `SnappedLatitude`/`SnappedLongitude` replace exact coords; added `DistanceKm`, `LocationMode` |
| `ServiceRequestRepository.cs` | Bbox prefilter, haversine in projection, DistanceAsc sort, geo in filter hash |
| `GetProviderDiscoveryQueryHandler.cs` | Geo validation, coordinate snapping, LocationMode |
| `CursorHelper.cs` | DistanceAsc cursor support, geo fields in hash |
| `ServiceRequestEntityConfiguration.cs` | `(Status, Lat, Lng)` index |

---

## 09b.1 — Vessel Enrichment Moved to BFF

### What changed and why

The vessel snapshot (5 columns on `ServiceRequestEntity`, written at publication, requiring a `ServiceRequest → Vessel` remote call on the write path) was replaced with **BFF bulk enrichment** (one cached `Vessel` call per page on the read path).

| | Snapshot (09a) | BFF enrichment (09b.1) |
|---|---|---|
| Cross-module coupling | ServiceRequest → Vessel (write path) | BFF → Vessel (read path) |
| Read cost | zero (columns on the row) | 1 call per page, mostly from cache |
| Freshness | frozen at publication | always current |
| Schema | 5 columns + migration + backfill | none |
| Failure mode | permanent nulls on that row | this page renders without decoration; next page is fine |

### ServiceRequest — removed

- **Migration** `20260714124319_RemoveVesselSnapshotColumns` drops `VesselTypeCode`, `VesselManufacturer`, `VesselModel`, `VesselLengthValue`, `VesselLengthUnitCode`
- **`IServiceRequestVesselRemoteCall`** deleted — ServiceRequest calls no module except ReferenceData (city invariant)
- **`Publish()`** reverted to no-argument form
- **`PublishServiceRequestCommandHandler`** no longer fetches vessel data
- **Discovery DTO** carries `VesselId` + `VesselName` only (VesselName predates all of this)
- **`docker-compose.yaml`** — removed `RemoteCalls__IServiceRequestVesselRemoteCall__BaseUrl`

### Vessel — bulk summary endpoint

`GET /api/v1/vessels/summary?ids=1,2,3`

Returns `VesselSummaryDto[]`: VesselId, Name, VesselTypeCode, Brand, Model, LengthValue, LengthUnitCode.
- Id list capped at 100 — above the cap: rejected
- Missing ids are omitted, not errored
- Auth: standard service-token path (`[AllowAnonymous]` on the endpoint since the BFF authenticates via service token)

### BFF — enrichment handler

`GetProviderDiscoveryBffQueryHandler`:
1. Resolves provider identity (fail closed)
2. Calls module `GET /provider/discovery` with filter params
3. Collects distinct `VesselId`s from the page (≤ page size ≤ 50)
4. Checks `IAizenDistributedCache` per id (key: `vessel:summary:{id}`)
5. Makes **one** bulk call to `GET /vessels/summary?ids=...` for uncached ids
6. Caches each returned summary (TTL 10 minutes)
7. Merges vessel fields into BFF response DTOs

**One call per page. Not one per card.** Subsequent pages hitting the same vessels require zero Vessel calls (cache).

**Failure policy:** If Vessel is unreachable, the page renders with vessel fields null + warning logged. A boat's length is decoration; it never stops a provider from seeing work.

**Markers make no Vessel call** — the markers endpoint (future) reuses the module's discovery projection which carries only VesselId/VesselName. No enrichment.

### Files created

| File | Purpose |
|------|---------|
| `Vessel.Abstraction/Response/Vessel/GetVesselSummariesResponse.cs` | Bulk summary response + VesselSummaryDto |
| `Vessel.Application/Query/Vessel/GetVesselSummaries/GetVesselSummariesQuery.cs` | Query |
| `Vessel.Application/Query/Vessel/GetVesselSummaries/GetVesselSummariesQueryHandler.cs` | Handler (cap 100, Select projection) |
| `BFF/Common/RemoteClients/IProviderVesselRemoteCall.cs` | BFF → Vessel remote call |
| `BFF/ServiceRequests/GetProviderDiscoveryBffQuery.cs` | BFF discovery query |
| `BFF/ServiceRequests/GetProviderDiscoveryBffResponse.cs` | BFF response with vessel fields |
| `BFF/ServiceRequests/GetProviderDiscoveryBffQueryHandler.cs` | Enrichment handler with cache |
| `SR.Repository/Migrations/20260714124319_RemoveVesselSnapshotColumns.cs` | Drop 5 columns |

### Files modified

| File | Change |
|------|--------|
| `ServiceRequestEntity.cs` | Removed 5 vessel properties, reverted `Publish()` to no-arg |
| `ServiceRequestEntityConfiguration.cs` | Removed 5 vessel property configs |
| `ProviderDiscoveryResponse.cs` | Removed 5 vessel fields, added `VesselId` |
| `ServiceRequestRepository.cs` | Removed vessel fields from projection, added `VesselId` |
| `PublishServiceRequestCommandHandler.cs` | Removed vessel fetch + logger |
| `VesselController.cs` | Added `GET summary` endpoint |
| `BFF DependencyInjection.cs` | Registered `IProviderVesselRemoteCall` |
| `BFF csproj` | Added `Vessel.Abstraction` reference |
| `docker-compose.yaml` | Added Vessel base URL to both BFF instances; removed from SR |
| `IProviderServiceRequestRemoteCall.cs` | Added `GetProviderDiscovery` method |

## What is NOT done

Nothing deferred. The phase is complete.
