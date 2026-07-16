# 02 — Current Backend Audit (ServiceRequest, Vessel, ReferenceData, Identity)

Read from source, not from docs. Paths are real.

## ServiceRequest module

`Modules/ServiceRequest/src/` → `Abstraction | Application | Core | Domain | Repository | (host)`.

### Aggregate

`Domain/Entities/ServiceRequest/ServiceRequestEntity.cs` — `AizenEntityWithAudit`, private setters, static
`Create(...)`, child collections: `Items`, `StatusHistory`, `Attachments`, `Messages`, `Offers`,
`Assignment`, `Completion`, `Dispute`, `WorkPhases`, `Conversations`.

Fields relevant to discovery:

| Field | Present | Note |
|---|---|---|
| `RequestCode`, `Title`, `Description` | ✅ | |
| `Status` (`ServiceRequestStatus`) | ✅ | Draft=1, Open=10, WaitingForOffer=11, OfferReceived=12 … Cancelled=90, Expired=91, Closed=99 |
| `Priority` (`ServiceRequestPriority`) | ✅ | Low=1, Normal=2, High=3, **Urgent=4, Emergency=5** |
| `ServiceCategoryCode`, `ServiceTypeCode` | ✅ | flat; **no subcategory collection** |
| `LocationCountryCode`, `LocationCityCode`, `LocationMarinaName` | ✅ | city now canonical plate code (`35`) |
| `LocationLatitude`, `LocationLongitude` (`decimal?`) | ✅ | **exist and are the basis of the map** |
| `RequestedStartDate/EndDate` | ✅ | |
| `ExpiresAt` | ✅ | request expiry — **not** an explicit offer deadline |
| `VesselId`, `VesselName` | ✅ / snapshot | no type, no length |
| **Budget min/max/currency** | ❌ | **does not exist** |
| **PublishedAt** | ❌ | only `CreateDate` (audit) — draft creation, not publication |
| `PaymentTransactionId` | ✅ | Payment is wired on offer acceptance |

### The provider query — reusable shape, wrong semantics

`Application/Query/Provider/GetOpenServiceRequests/GetOpenServiceRequestsQueryHandler.cs`
→ `Repository/Repositories/ServiceRequestRepository.cs` `BuildProviderOpenQuery`:

```csharp
var query = _db.ServiceRequests.AsNoTracking()
    .Where(x => !x.IsDeleted
        && biddableStatuses.Contains(x.Status)           // Open | WaitingForOffer | OfferReceived
        && !x.Offers.Any(o => o.ProviderProfileId == providerProfileId && !o.IsDeleted)   // ← EXCLUDES
        && x.Assignment == null);
```

Two structural problems for this screen:

1. **It excludes requests the provider has already bid on.** The new screen needs them *present* with an
   offer-state badge — the "Teklif Verdiğim: 8 aktif" KPI and the "only ones I haven't bid on" *filter*
   both require the offered ones to be in the result set and filtered **optionally**. Exclusion must become
   **projection**.
2. `GetOpenForProviderAsync` does `.Include(x => x.Offers).Include(x => x.Attachments)` and then counts them
   in memory (`sr.Offers.Count`). It loads every offer row (including other providers' amounts — see doc 05,
   privacy) to produce two integers. Must become a `Select` projection with `.Count()` subqueries.

Filters supported today: `ServiceCategoryCode`, `LocationCityCode`, `LocationCountryCode`, `MinPriority`,
`SearchTerm`. Sorting: `OrderByDescending(CreateDate)`. Paging: offset (`PageIndex`/`PageSize`).

**No geospatial capability of any kind** — no radius, no bounding box, no distance, no ordering by distance.

### Indexes

From the EF designer snapshots: `ServiceRequests` has indexes on `CreateDate`, `OwnerUserId`, `RequestCode`,
`Status`; children indexed on `ServiceRequestId`, `ProviderProfileId`, `Status`. **No composite index for the
discovery predicate** (`Status` + city / coordinates), **no index on the coordinate columns**.

### Realtime

`ServiceRequestPublishedMessage` → RabbitMQ → `Aizen.Bff.MarineProvider/Realtime/ServiceRequestPublishedRealtimeConsumer`
→ `city:{code}` group. Proven end to end today (two BFF replicas, Redis backplane, 0/6 consumed by the
socket-less instance, all six delivered).

Gaps: only **published** is bridged. No updated / cancelled / urgency-changed / attachment-added event
reaches the provider list. And the city group is the **only** filter — a provider in `35` is notified about
requests in categories they do not serve.

`ServiceRequestRealtimeEventType` defines 23 event types; `ServiceRequestRealtimePublisher` pushes them to the
module's own hub, which **no browser connects to**. The provider browser only talks to the BFF hub.

## Vessel module

`Modules/Vessel/src/…` — controllers: `VesselController`, `VesselSpecificationController`, `VesselEngineController`,
`VesselMediaController`, `VesselLocationController`, `VesselDocumentController`, `VesselOwnershipController`,
`VesselStatusController`, admin.

`VesselEntity`: `Name`, `VesselTypeCode`. `VesselSpecificationEntity`: `Model`, `LengthValue` + `LengthUnitCode`,
`BeamValue` + `BeamUnitCode`, `HullMaterialCode`, build year (spec).

**There is no `IVesselModuleRemoteCall` anywhere in the solution, and no batch/`ByIds` endpoint on Vessel.**
So a discovery list that shows vessel type/length would need one HTTP call per card → N+1 across the network.

## ReferenceData

Owns country/city/district, currency, units, lookup groups/items, marine lookups. City is now the canonical
**province plate code** (`34`, `35`, `48`), shared by Identity (`UserProfile.City`) and ServiceRequest
(`LocationCityCode`) — established and verified today. Categories/service types are code-based; translation is
the frontend's job.

## Identity / provider context

`UserProfileEntity`: `City` (plate code), `Country`, company name, status. **No coordinates, no service radius,
no service-area geometry, no per-provider category list.** `ProviderProfileId` arrives at modules through the
BFF assertion.

This is the single most important backend fact for this screen: **the supply side has no geography beyond a
city code.**
