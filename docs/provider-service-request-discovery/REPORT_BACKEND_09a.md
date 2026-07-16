# 09a Backend Phase 1: Discovery Data Foundation — Report

**Date:** 2026-07-14
**Branch:** `feature/messaging-registration`

---

## 1. PublishedAt

**Column:** `DateTime? PublishedAt` on `ServiceRequestEntity`, private setter.

**Domain method:** `Publish()` sets `PublishedAt ??= DateTime.UtcNow` — idempotent, a republish does not overwrite.

**EF config:** Nullable DateTime, indexed as `(Status, PublishedAt)`.

**Backfill:** Existing rows in a published status (Open, WaitingForOffer, OfferReceived, and beyond) should have `PublishedAt = CreateDate`. These are **approximate** — `CreateDate` is draft creation, not publication. The backfill SQL:

```sql
UPDATE servicerequest.service_requests
SET "PublishedAt" = "CreateDate"
WHERE "PublishedAt" IS NULL
  AND "Status" NOT IN (1)  -- 1 = Draft
  AND "IsDeleted" = false;
```

**Not run** — requires the EF migration to add the column first. The migration has not been generated (no `dotnet ef` in this session).

---

## 2. Vessel Snapshot

**Columns added to `ServiceRequestEntity`:**
- `VesselTypeCode` (string?, max 50)
- `VesselManufacturer` (string?, max 200)
- `VesselModel` (string?, max 200)
- `VesselLengthValue` (decimal?, precision 10,2)
- `VesselLengthUnitCode` (string?, max 10)

`VesselName` already existed.

**Set at publication:** `Publish()` accepts optional vessel snapshot params. Immutable after first publication — a republish does not overwrite.

**No Vessel remote call exists.** There is no `IVesselRemoteCall`, no batch endpoint, no Vessel module reference in the ServiceRequest project. The snapshot columns are ready for when that integration is built. Until then, only `VesselName` (already denormalized on the entity) has data.

**Backfill from Vessel:** Cannot be done without a Vessel API or direct DB access to the Vessel schema. Left as a TODO for when the Vessel integration ships. Existing rows will have null vessel snapshot columns (except `VesselName`).

---

## 3. Budget — DECIDED: NO

**No budget columns were added.** No `BudgetMin`, `BudgetMax`, `BudgetCurrencyCode` anywhere — not on the entity, not on any DTO, not as a placeholder.

Reasons (from the product decision, 2026-07-14):
- A published budget anchors every offer to the ceiling
- Owners cannot state a budget for marine work (condition-dependent pricing)
- Vessel type/length, category, urgency, marina and distance are what providers need

The market-range idea (derived from historical offers) is post-MVP.

---

## 4. Discovery Query

### Filter DTO

`ProviderServiceRequestDiscoveryFilter`:
- `PageSize` (int, default 20, max 50)
- `Cursor` (string?, Base64-encoded)
- `SortBy` (string, `PublishedAtDesc` or `PriorityDesc`)
- `LocationCityCode`, `LocationCountryCode`, `ServiceCategoryCode` (string?)
- `MinPriority` (ServiceRequestPriority?)
- `SearchTerm` (string?)
- `OfferState` (OfferStateFilter?, enum: Any/NotOffered/Offered)
- **No `ProviderProfileId` field** — identity comes from the BFF assertion

### Response DTO

`ProviderDiscoveryResponse`:
- `Items` (List of `ProviderDiscoveryItemDto`)
- `NextCursor` (string?)
- `PageSize` (int)
- **No `TotalCount`**

`ProviderDiscoveryItemDto` includes:
- Id, RequestCode, Title, Description, Status, Priority
- ServiceCategoryCode, ServiceTypeCode
- Location fields (CityCode, CountryCode, MarinaName, Lat, Lng)
- Dates (RequestedStart/End, ExpiresAt, PublishedAt)
- Vessel snapshot (Name, TypeCode, Manufacturer, Model, LengthValue, LengthUnitCode)
- Counts: `OfferCount`, `AttachmentCount` (subquery counts)
- Caller's offer: `HasProviderOffer`, `ProviderOfferId`, `ProviderOfferStatus`
- **No OwnerUserId, no RequestedByEmail, no foreign offer amounts/details**

### Repository: Select Projection

The query uses `.Select()` — **no `.Include()`**. The generated SQL will produce:
- `COUNT(*)` subqueries for OfferCount and AttachmentCount
- `EXISTS` subquery for HasProviderOffer
- Scalar subqueries for ProviderOfferId and ProviderOfferStatus
- **No child collection is materialized**

Other providers' offer rows are never loaded — the projection only touches the caller's own offer via a filtered subquery.

### Cursor Pagination

- Encoded as Base64 of `sortValue|lastId|filterHash`
- `filterHash` is SHA256 of the serialized filter fields (excluding Cursor and PageSize)
- A cursor from different filters is silently ignored (starts from page 1)
- Sort order: `PublishedAtDesc` → `(PublishedAt DESC, Id DESC)`, `PriorityDesc` → `(Priority DESC, Id DESC)`
- Always tiebreaks on `Id`
- `Take(pageSize + 1)` to detect next page without a count query

### OfferState Filter

- `Any` (default): shows all biddable SRs, with `HasProviderOffer` badge
- `NotOffered`: only SRs the provider has NOT offered on
- `Offered`: only SRs the provider HAS offered on
- The hard exclusion from the old query is **deleted** — replaced by this optional predicate

### Auth

- Provider identity from `_info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId`
- Missing or `<= 0` → **`AizenBusinessException("Provider identity could not be resolved.")`** — not an empty list
- `ProviderServiceRequestDiscoveryFilter` has no `ProviderProfileId` field

### Endpoint

`GET /api/v1/service-requests/provider/discovery?PageSize=20&SortBy=PublishedAtDesc&LocationCityCode=35`

### Indexes

- `(Status, LocationCityCode)` — city-filtered discovery
- `(Status, PublishedAt)` — newest-first default sort

---

## 5. 09a.1 Follow-Up: Migration and Vessel Producer (CLOSED)

### Migration — Applied

Migration `20260714115342_AddDiscoveryColumns` generated and applied. The database now has:

```
 PublishedAt          | timestamp with time zone
 VesselLengthUnitCode | character varying(10)
 VesselLengthValue    | numeric(10,2)
 VesselManufacturer   | character varying(200)
 VesselModel          | character varying(200)
 VesselTypeCode       | character varying(50)
```

Indexes:
```
 IX_service_requests_Status_LocationCityCode  btree (Status, LocationCityCode)
 IX_service_requests_Status_PublishedAt       btree (Status, PublishedAt)
```

**Verified against the running database** — not just the code.

### PublishedAt Backfill — Done

```sql
UPDATE servicerequest.service_requests
SET "PublishedAt" = "CreateDate"
WHERE "PublishedAt" IS NULL AND "Status" != 1 AND "IsDeleted" = false;
-- 45 rows updated
```

All 45 published rows now have `PublishedAt`. These are **approximate** — `CreateDate` is draft creation, not publication. Republishing does not overwrite (idempotent: `PublishedAt ??= DateTime.UtcNow`).

### Vessel Snapshot Producer — Built (option a)

**Choice: (a) Build the producer.** Five null columns with no writer is the budget pattern we refused.

`IServiceRequestVesselRemoteCall` added to `ServiceRequest.Abstraction/RemoteCall/` — calls `GET /api/v1/vessels/{vesselId}`.

`PublishServiceRequestCommandHandler` now fetches the vessel at publication:
- Reads `VesselTypeCode` from `vessel.Vessel.VesselTypeCode`
- Reads `Brand`, `Model`, `LengthValue`, `LengthUnitCode` from `vessel.Vessel.Specification`
- Passes all five into `entity.Publish(...)`
- **Immutable**: `Publish()` only sets the snapshot on first publication

**Failure policy:** If Vessel is unreachable, publication **proceeds with a null snapshot** and logs a warning. A missing boat length must not stop an owner from publishing a job.

**Docker-compose:** `RemoteCalls__IServiceRequestVesselRemoteCall__BaseUrl: http://vessel-api:8080`

**Vessel snapshot backfill — Done.** Both `servicerequest` and `vessel` schemas live in `inktavia_store`. Direct SQL join:

```sql
UPDATE servicerequest.service_requests sr
SET "VesselTypeCode" = v."VesselTypeCode",
    "VesselManufacturer" = vs."Brand",
    "VesselModel" = vs."Model",
    "VesselLengthValue" = vs."LengthValue",
    "VesselLengthUnitCode" = vs."LengthUnitCode"
FROM vessel.vessels v
LEFT JOIN vessel.vessel_specifications vs ON vs."VesselId" = v."Id"
WHERE sr."VesselId" = v."Id"
  AND sr."VesselTypeCode" IS NULL AND sr."IsDeleted" = false AND sr."Status" != 1;
-- 21 rows updated
```

Result: 21/45 published rows now have vessel snapshot data. The remaining 24 have `VesselId` values (test data, id=1) that do not exist in the vessel table — left as null, not guessed.

### Identity rejection verified

Calling the discovery endpoint without the BFF assertion returns:
```json
{"header":{"isSuccess":false,"errorCode":9999,"errorMessage":"Provider identity could not be resolved."}}
```

Not an empty list — a rejection. Working as designed.

---

## Build Status

`dotnet build` — **succeeded** with no errors.

---

## Files Created

| File | Purpose |
|------|---------|
| `Abstraction/Request/Filter/ProviderServiceRequestDiscoveryFilter.cs` | Filter DTO + `OfferStateFilter` enum |
| `Abstraction/Response/Provider/ProviderDiscoveryResponse.cs` | Response + item DTO |
| `Application/Query/Provider/GetProviderDiscovery/GetProviderDiscoveryQuery.cs` | Query |
| `Application/Query/Provider/GetProviderDiscovery/GetProviderDiscoveryQueryHandler.cs` | Handler (reject missing identity) |
| `Application/Query/Provider/GetProviderDiscovery/CursorHelper.cs` | Cursor encode/decode |
| `Abstraction/RemoteCall/IServiceRequestVesselRemoteCall.cs` | Vessel remote call for snapshot |
| `Repository/Migrations/20260714115342_AddDiscoveryColumns.cs` | EF migration |

## Files Modified

| File | Change |
|------|--------|
| `ServiceRequestEntity.cs` | Added `PublishedAt`, vessel snapshot columns, updated `Publish()` |
| `ServiceRequestEntityConfiguration.cs` | EF config for new columns + discovery indexes |
| `IServiceRequestRepository.cs` | Added `GetDiscoveryAsync` |
| `ServiceRequestRepository.cs` | Implemented Select-projection discovery query |
| `ProviderJobsController.cs` | Added `GET /discovery` endpoint |
| `PublishServiceRequestCommandHandler.cs` | Vessel snapshot fetch + failure policy |
| `docker-compose.yaml` | Vessel remote call base URL |

## What is NOT done

| Item | Status |
|------|--------|
| Discovery endpoint returns rows via BFF | Requires BFF handler + remote call (09c scope) |

### Previously listed as blocked — now closed

- **Vessel snapshot backfill**: Done. 21/45 rows backfilled from `vessel.vessels` + `vessel.vessel_specifications` (same database). 24 rows have null because their VesselId does not exist in the vessel table (test data).
- **City code validation in discovery handler**: Done. `GetProviderDiscoveryQueryHandler` now validates `LocationCityCode` against ReferenceData via the same `IServiceRequestReferenceDataRemoteCall` that `PublishServiceRequestCommandHandler` already uses. Unknown code → `AizenBusinessException`. PageSize capped at 50.
