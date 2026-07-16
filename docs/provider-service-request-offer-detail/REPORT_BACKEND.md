# Backend Report — Provider Service Request Detail

**Date:** 2026-07-15
**Branch:** `feature/messaging-registration`

---

## P1 — Detail Read Model

### Summary

Extended the provider detail endpoint to return a **privacy-filtered** read model. The existing `GetServiceRequestDetailResponse` (which exposes OwnerUserId, raw coordinates, all providers' offers) is replaced with `GetProviderServiceRequestDetailResponse` containing a `ProviderServiceRequestDetailDto`.

### Privacy filtering — verified

| Field | Before (generic detail) | After (provider detail) |
|-------|------------------------|------------------------|
| `OwnerUserId` | Exposed | **Removed** — not in `ProviderServiceRequestDto` |
| `LocationLatitude/Longitude` | Raw coordinates | **Snapped** to ~500m grid (`ApproxLatitude/ApproxLongitude`) |
| Other providers' offers | All offers projected | **Only caller's own** — filtered by `providerProfileId` |
| `EstimatedUnitPrice` | Exposed in work-scope items | **Removed** — `WorkScopeItemDto` has no price field |
| Attachment object keys | N/A (not stored) | Only `FileId`, `AttachmentType`, `Title`, `CreatedAt` |

### Coordinate snapping

Same algorithm as the discovery SQL projection:
```csharp
snapped = Math.Round((rawCoord + (entityId % 7 - 3) * 0.001) / 0.005) * 0.005
```
Deterministic per-row jitter on a ~500m grid. Identical grid as discovery markers — markers and detail coordinates are consistent.

### Response shape

```json
{
  "detail": {
    "request": {
      "id": 9011,
      "requestCode": "SR-SEED-EMERGENCY-1",
      "title": "Acil: Dümen sistemi arızası — Çeşme",
      "status": 10,
      "priority": 5,
      "serviceCategoryCode": "REPAIR",
      "locationCityCode": "35",
      "locationMarinaName": "Çeşme Marina",
      "approxLatitude": 38.32,
      "approxLongitude": 26.3,
      "vesselId": 20004,
      "publishedAt": "2026-07-15T10:06:35.499508Z"
    },
    "workScope": [],
    "attachments": [],
    "myOffer": null,
    "timeline": [],
    "offerCount": 0,
    "attachmentCount": 0
  }
}
```

Request 30005 (with seeded items and status history):
- `workScope`: 2 items (title, description, quantity, unitCode, sortOrder — no `estimatedUnitPrice`)
- `timeline`: 2 status transitions
- `myOffer`: null (provider2 has no offer)

### Vessel enrichment

The BFF handler (`GetServiceRequestDetailBffQueryHandler`) performs **one** vessel summary call per detail request:
- Checks `vessel:summary:{vesselId}` in Redis cache first (TTL 10 min)
- On miss: calls `IProviderVesselRemoteCall.GetSummaries` with a single id
- Enriches `VesselName` on the response (fallback when module's denormalized name is null)
- One call, not N+1

### Access check — unchanged

Three-prong check in the module handler:
1. Request is biddable (Open/WaitingForOffer/OfferReceived), OR
2. Provider has an offer on it, OR
3. Provider is assigned to it

Rejection uses the same "not found" message as a missing request — no existence leak.

### Files created

| File | Purpose |
|------|---------|
| `Abstraction/Dto/ProviderServiceRequestDetailDto.cs` | Full detail aggregate DTO |
| `Abstraction/Dto/ProviderServiceRequestDto.cs` | Provider-safe request projection (no OwnerUserId, snapped coords) |
| `Abstraction/Dto/WorkScopeItemDto.cs` | Work-scope item (no EstimatedUnitPrice) |
| `Abstraction/Dto/ProviderAttachmentMetaDto.cs` | Attachment metadata (no signed URL, no object key) |
| `Abstraction/Response/ServiceRequest/GetProviderServiceRequestDetailResponse.cs` | Response wrapper |

### Files modified

| File | Change |
|------|--------|
| `ServiceRequestMappingExtensions.cs` | Added `ToProviderDto`, `ToWorkScopeDto`, `ToProviderAttachmentMetaDto`, `ToProviderDetailDto`, `SnapCoordinate` |
| `GetProviderServiceRequestDetailQuery.cs` | Changed generic from `GetServiceRequestDetailResponse` to `GetProviderServiceRequestDetailResponse` |
| `GetProviderServiceRequestDetailQueryHandler.cs` | Changed return type; calls `sr.ToProviderDetailDto(providerProfileId)` |
| `ProviderJobsController.cs` | Updated `GetServiceRequestDetail` return type |
| `IProviderServiceRequestRemoteCall.cs` | Updated `GetServiceRequestDetail` return type |
| `GetServiceRequestDetailBffQuery.cs` | Changed generic to `GetProviderServiceRequestDetailResponse` |
| `GetServiceRequestDetailBffQueryHandler.cs` | Added vessel enrichment (cache + one bulk call); updated return type |

---

## 10b — Offer Migrations & Domain

### Schema changes — verified in database

**`\d servicerequest.service_request_offer_items`** (key columns):

| Column | Type | Notes |
|--------|------|-------|
| `Quantity` | `numeric(12,3)` | **Changed from `integer`** |
| `UnitCode` | `character varying(50)` | New |
| `TaxRate` | `numeric(9,4)` | New — fraction (e.g. 0.20) |
| `DiscountType` | `integer` | New — enum: None=0, Amount=1, Percent=2 |
| `DiscountValue` | `numeric(18,2)` | New — nullable |
| `LineSubtotal` | `numeric(18,2)` | New — computed snapshot (10c) |
| `TaxAmount` | `numeric(18,2)` | New — computed snapshot |
| `LineTotal` | `numeric(18,2)` | New — computed snapshot |
| `DiscountAmount` | `numeric(18,2)` | New — computed snapshot |
| `IsDiscount` | — | **Dropped** — replaced by `DiscountType` driven off `ItemType` |

**`\d servicerequest.service_request_offers`** (new columns):

| Column | Type | Notes |
|--------|------|-------|
| `Subtotal` | `numeric(18,2)` | Computed total (10c) |
| `TaxTotal` | `numeric(18,2)` | Computed total |
| `DiscountTotal` | `numeric(18,2)` | Computed total |
| `GrandTotal` | `numeric(18,2)` | Computed total — `TotalAmount` is kept as alias, always = `GrandTotal` |
| `ServiceTotal` through `OtherTotal` | `numeric(18,2)` | 8 per-category totals (stored, computed in 10c) |
| `DepositType` | `integer` | Enum: None=0, Amount=1, Percent=2 |
| `DepositValue` | `numeric(18,2)` | Nullable |
| `PaymentTermsNote` | `character varying(2000)` | Free text |
| `WarrantyNote` | `character varying(2000)` | Free text |
| `SubmittedAt` | `timestamp with time zone` | Lifecycle |
| `ViewedAt` | `timestamp with time zone` | Lifecycle |
| `RevisionRequestedAt` | `timestamp with time zone` | Lifecycle |
| `xmin` | `xid` (implicit) | Concurrency token via `UseXminAsConcurrencyToken()` |

### Design decisions

**`IsDiscount` → dropped.** Replaced by `DiscountType` (None/Amount/Percent) which is more expressive. The discount concept is now driven by `DiscountType + DiscountValue` on each item, not by a boolean. `ItemType == Discount` lines still exist for explicit discount line items.

**`TotalAmount` / `GrandTotal` relationship:** `TotalAmount` is retained as a legacy alias for `GrandTotal`. `SetComputedTotals()` always sets both to the same value. No two independent totals that can disagree.

**Per-category totals:** Stored (8 columns), computed by the calculation service in 10c. Read is a simple column fetch — no runtime aggregation.

**Concurrency token:** PostgreSQL `xmin` via EF's `UseXminAsConcurrencyToken()`. No explicit `RowVersion` column — `xmin` is an implicit system column managed by PostgreSQL on every row update. EF reads it and sends `WHERE xmin = @p` on save.

### Domain methods added

- `ReplaceItems(IEnumerable<...>)` — atomically swaps the draft's item set (clears + adds). Guards: only Draft.
- `MarkSubmitted(DateTime utc)` — sets `Status=Submitted` + `SubmittedAt`. Guards: only Draft.
- `MarkViewed(DateTime utc)` — sets `ViewedAt` (idempotent — first write wins).
- `MarkRevisionRequested(DateTime utc)` — sets `RevisionRequestedAt`.
- `Update(...)` — now guards: only Draft is editable; throws on non-Draft.
- `UpdateCommercialTerms(...)` — deposit, payment terms, warranty.
- `SetComputedTotals(...)` — sets all totals + keeps `TotalAmount` in sync with `GrandTotal`.
- Offer item: `SetComputedTotals(...)` — sets line-level computed snapshots.

### Decimal round-trip — verified

```sql
INSERT INTO servicerequest.service_request_offer_items (..., "Quantity", "TaxRate", ...)
VALUES (..., 2.500, 0.2000, ...);

SELECT "Quantity", "TaxRate" FROM ... WHERE ...;
-- Quantity: 2.500, TaxRate: 0.2000  ✓
```

### Migration

`20260715120656_AddOfferDomainColumns.cs` + `.Designer.cs` — generated by `dotnet ef migrations add`, with Designer file. Verified: migration applied to DB, columns exist.

### Existing offer totals

Existing offers have `Subtotal=0`, `TaxTotal=0`, `GrandTotal=0` (new column defaults). `TotalAmount` retains its original value. Totals will be recomputed when an offer is next saved through the calculation service (10c).

### Files created

| File | Purpose |
|------|---------|
| `Abstraction/Enum/OfferDiscountType.cs` | None/Amount/Percent enum |
| `Abstraction/Enum/OfferDepositType.cs` | None/Amount/Percent enum |
| `Migrations/20260715120656_AddOfferDomainColumns.cs` | Schema migration |
| `Migrations/20260715120656_AddOfferDomainColumns.Designer.cs` | Migration designer |

### Files modified

| File | Change |
|------|--------|
| `ServiceRequestOfferEntity.cs` | Added totals, commercial terms, lifecycle timestamps, domain methods (ReplaceItems, MarkSubmitted, etc.), guards |
| `ServiceRequestOfferItemEntity.cs` | Quantity int→decimal, added UnitCode, TaxRate, DiscountType/Value, computed snapshots, SetComputedTotals |
| `ServiceRequestOfferEntityConfiguration.cs` | Precision for all new decimal columns, UseXminAsConcurrencyToken, max lengths |
| `ServiceRequestOfferItemEntityConfiguration.cs` | Quantity precision 12,3, new column precisions/max lengths |
| `ServiceRequestOfferDto.cs` | Added Subtotal, TaxTotal, DiscountTotal, GrandTotal, DepositType/Value, PaymentTermsNote, WarrantyNote, SubmittedAt, ViewedAt |
| `ServiceRequestOfferItemDto.cs` | Quantity int→decimal, added UnitCode, TaxRate, DiscountType/Value, LineSubtotal, TaxAmount, LineTotal, DiscountAmount; removed IsDiscount |
| `CreateServiceRequestOfferItemRequest.cs` | Quantity int→decimal, added UnitCode, TaxRate, DiscountType, DiscountValue |
| `ServiceRequestMappingExtensions.cs` | Updated offer/item ToDto() for new fields |
| `CreateServiceRequestOfferCommandHandler.cs` | Passes new item fields to Create() |
| `ServiceRequestDbContextModelSnapshot.cs` | Updated by EF (auto-generated) |

---

## 10c — Calculation, Validation & Offer Lifecycle

### Calculation algorithm — verified against hand-computed fixture

**Decisions:**
- Discount percent unit: **0–100** (e.g. 10 = 10%)
- Pro-rata rule: each line bears discount proportional to `lineSubtotal / subtotal`
- Rounding: `Math.Round(x, 2, MidpointRounding.AwayFromZero)` per line
- Discount applied **pre-tax**; `grandTotal` clamped ≥ 0

**Fixture — multi-type items + 10% discount + KDV 20%:**

| Line | Type | Qty | UnitPrice | LineSubtotal | Discount | TaxBase | Tax (20%) | LineTotal |
|------|------|-----|-----------|-------------|----------|---------|-----------|-----------|
| İşçilik | Labor | 2.5 | 350 | 875.00 | 87.50 | 787.50 | 157.50 | 945.00 |
| Hidrolik pompa | Product | 3 | 120 | 360.00 | 36.00 | 324.00 | 64.80 | 388.80 |
| Sezon indirimi | Discount (10%) | — | — | 0 | 123.50 | — | — | -123.50 |

| | Hand-computed | Server response | Match |
|---|---|---|---|
| subtotal | 1235.00 | 1235.0 | ✓ |
| discountTotal | 123.50 | 123.5 | ✓ |
| taxTotal | 222.30 | 222.30 | ✓ |
| grandTotal | 1333.80 | 1333.80 | ✓ |

### Client-sent totals ignored

The preview request included `"grandTotal": 999999, "taxTotal": 999999`. The server returned `grandTotal: 1333.80`, `taxTotal: 222.30` — client values overwritten.

### Validation — tested

| Test | Result |
|------|--------|
| Mixed currency (TRY offer + USD item) | Rejected: "Mixed currencies are not allowed." |
| Empty submit (only discount lines, no priced lines) | Rejected: "SR_OFFER_EMPTY" |
| Decimal quantity 2.5 | Priced correctly (875.00) |

### Lifecycle — tested

| Test | Result |
|------|--------|
| SaveDraft — creates new Draft | ✓ (Id=2, Status=Draft, GrandTotal=1333.80) |
| Submit (Draft→Submitted) | ✓ (Status=Submitted, SubmittedAt set) |
| Double-submit (same idempotency key) | ✓ Same offer returned, no duplicate |
| SaveDraft on submitted offer | Creates a new Draft (correct — previous offer is frozen) |
| Submitted offer is immutable | ✓ — `Update()` throws on non-Draft |

### Commands implemented

| Command | Route | Description |
|---------|-------|-------------|
| `SaveOfferDraftCommand` | `PUT /offers/draft` | Get-or-create Draft, ReplaceItems, calculate, persist |
| `PreviewOfferCommand` | `POST /offers/preview` | Calculate without persisting |
| `SubmitOfferCommand` | `PATCH /offers/{id}/submit` | Draft→Submitted, idempotent, rejects empty |

### Concurrency

Optimistic concurrency via PostgreSQL `xmin` (configured via `UseXminAsConcurrencyToken()`). EF automatically includes `WHERE xmin = @p` on save. `DbUpdateConcurrencyException` maps to `SR_OFFER_STALE`.

### Known limitation — ID in SaveDraft response

The `SaveOfferDraftResponse` returns `offer.Id = 0` for a newly created draft because EF hasn't assigned the database-generated ID at the point the response is built (the CQRS pipeline's unit-of-work saves after the handler returns). The offer IS persisted correctly. The next `SaveDraft` call picks it up via `GetDraftByProviderAndRequestAsync`. This will be resolved when the BFF endpoints (10d) read back after save.

### Files created

| File | Purpose |
|------|---------|
| `Application/Services/OfferCalculationService.cs` | Single calculation algorithm (doc 06) |
| `Command/Offer/SaveOfferDraft/SaveOfferDraftCommand.cs` | Aggregate draft save command |
| `Command/Offer/SaveOfferDraft/SaveOfferDraftCommandHandler.cs` | Get-or-create, validate, calculate, persist |
| `Command/Offer/PreviewOffer/PreviewOfferCommand.cs` | Preview command |
| `Command/Offer/PreviewOffer/PreviewOfferCommandHandler.cs` | Calculate without persist |
| `Command/Offer/SubmitOffer/SubmitOfferCommand.cs` | Submit command |
| `Command/Offer/SubmitOffer/SubmitOfferCommandHandler.cs` | Draft→Submitted, idempotent |
| `Abstraction/Request/Offer/SaveOfferDraftRequest.cs` | Draft save request DTO |
| `Abstraction/Request/Offer/SubmitOfferRequest.cs` | Submit request DTO |
| `Abstraction/Response/Offer/SaveOfferDraftResponse.cs` | Response with concurrency token |
| `Abstraction/Response/Offer/PreviewOfferResponse.cs` | Preview response |
| `Abstraction/Response/Offer/SubmitOfferResponse.cs` | Submit response |

### Files modified

| File | Change |
|------|--------|
| `ServiceRequestOfferController.cs` | Added `PUT /draft`, `POST /preview`, `PATCH /{id}/submit` endpoints |
| `IServiceRequestOfferRepository.cs` | Added `GetDraftByProviderAndRequestAsync`, `GetByProviderRequestAndIdempotencyAsync` |
| `ServiceRequestOfferRepository.cs` | Implemented new repository methods |
| `Program.cs` | Registered `OfferCalculationService` |

---

## 10d — Provider BFF Offer Endpoints + Realtime

### Id=0 bug — fixed

**Problem:** `SaveOfferDraftCommandHandler` returned `offer.Id = 0` for new drafts because the CQRS pipeline's unit-of-work saves after the handler returns.

**Fix:** The handler now calls `_db.SaveChangesAsync(ct)` explicitly before building the response. First-ever `SaveOfferDraft` now returns a non-zero `offerId`.

```json
{ "offer": { "id": 4, "status": 1, "grandTotal": 1333.8, ... }, "concurrencyToken": "4" }
```

### BFF endpoints — verified

| Endpoint | Route | Result |
|----------|-------|--------|
| SaveDraft | `PUT /provider/service-requests/{id}/offer/draft` | ✓ offerId=4, totals from module |
| Preview | `POST /provider/service-requests/{id}/offer/preview` | ✓ grandTotal=590 (client sent 999, ignored) |
| Submit | `POST /provider/service-requests/{id}/offer/{offerId}/submit` | ✓ Status=Submitted, submittedAt set |
| Withdraw | `POST /provider/service-requests/{id}/offer/{offerId}/withdraw` | ✓ (existing, re-routed) |

### Draft-save response (totals from module, not BFF)

```json
{
  "offer": {
    "id": 4,
    "status": "Draft",
    "subtotal": 1235.0,
    "discountTotal": 123.5,
    "taxTotal": 222.3,
    "grandTotal": 1333.8,
    "depositType": "Percent",
    "depositValue": 30.0,
    "paymentTermsNote": "Ön ödeme %30, kalan teslimde",
    "warrantyNote": "6 ay garanti",
    "items": [3 items with server-computed lineSubtotal/taxAmount/lineTotal]
  },
  "concurrencyToken": "4"
}
```

The BFF passes totals through untouched — no calculation in the BFF.

### Client-sent totals ignored — verified

Preview request with `"grandTotal": 999` → server returned `grandTotal: 590.0` (1 × 500 × 1.18).

### Auth

- Provider identity from assertion (`X-Aizen-Provider-Profile-Id`), never from client
- Missing identity → reject with `AizenBusinessException`
- All BFF handlers resolve identity via `IProviderProfileResolver.ResolveAsync`

### Timeline

The P1 detail response already carries `timeline` (status history entries). A separate `/timeline` endpoint is not needed — the detail aggregate is sufficient.

### Attachment read-URL

**Not implemented.** Requires `IProviderFileStorageRemoteCall` integration to mint short-lived signed URLs. Deferred — no attachments exist on test requests; the contract is documented in the prompt.

### Realtime — live vs pending

| Event | Status | Target |
|-------|--------|--------|
| `OfferRejected` | **Live** — BFF consumer created, message contract exists, RejectOfferCommandHandler publishes | `provider:{profileId}` |
| `OfferAccepted` | **Live** — already existed from prior work | `provider:{profileId}` |
| `OfferViewedByCustomer` | **Pending** — event type constant added, but no customer-side command exists to set `ViewedAt`. Wire the producer when the customer app calls a "view offer" endpoint. | `provider:{profileId}` |
| `OfferRevisionRequested` | **Pending** — event type constant added, `RevisionRequestedAt` field exists, but no command triggers it yet. | `provider:{profileId}` |
| Message-added to provider | **Deferred** — no provider-scoped message-added event/command exists in the module. | — |

### Files created

| File | Purpose |
|------|---------|
| `BFF/Offers/SaveOfferDraftBffCommand.cs` | BFF command |
| `BFF/Offers/SaveOfferDraftBffCommandHandler.cs` | Forwards to module, passes totals through |
| `BFF/Offers/PreviewOfferBffCommand.cs` | BFF command |
| `BFF/Offers/PreviewOfferBffCommandHandler.cs` | Forwards to module |
| `BFF/Offers/SubmitOfferBffCommand.cs` | BFF command |
| `BFF/Offers/SubmitOfferBffCommandHandler.cs` | Forwards to module |
| `BFF/Realtime/OfferRejectedRealtimeConsumer.cs` | Bus → browser for rejected offers |

### Files modified

| File | Change |
|------|--------|
| `SaveOfferDraftCommandHandler.cs` (module) | Explicit `_db.SaveChangesAsync` before response to fix Id=0 |
| `IProviderServiceRequestRemoteCall.cs` | Added `SaveOfferDraft`, `PreviewOffer`, `SubmitOffer` Refit methods |
| `ProviderOffersController.cs` (BFF) | Added draft/preview/submit/withdraw endpoints |
| `ProviderRealtimeEvent.cs` | Added `OfferRejected`, `OfferViewedByCustomer`, `OfferRevisionRequested` event types |

---

## 14a — Vessel Enrichment Completion

### Problem

`GetServiceRequestDetailBffQueryHandler.ApplyVessel` fetched the full `VesselSummaryDto` (one cached bulk call) but only set `VesselName`, discarding `VesselTypeCode`, `Brand`, `Model`, `LengthValue`, `LengthUnitCode`.

### Fix

Added five nullable vessel-spec fields to `ProviderServiceRequestDto`: `VesselTypeCode`, `VesselBrand`, `VesselModel`, `VesselLengthValue`, `VesselLengthUnitCode`. The module leaves these null (no vessel snapshot on the entity). `ApplyVessel` now fills all six fields from the fetched `VesselSummaryDto`.

### Vessel block from detail response (SR 9011, vesselId 20004)

```json
{
  "vesselId": 20004,
  "vesselName": null,
  "vesselTypeCode": null,
  "vesselBrand": null,
  "vesselModel": null,
  "vesselLengthValue": null,
  "vesselLengthUnitCode": null
}
```

All fields are null because the Vessel API container (`vessel-api`) is not running in the current compose stack. The BFF logs: `"Vessel summary call failed for VesselId 20004. Detail returned without vessel enrichment."` — the try/catch works correctly, the detail does not fail. A running vessel-api with seeded vessel 20004 would populate these fields.

### Still one call

No new calls added. The existing single cached bulk call (cache key `vessel:summary:{id}`, TTL 10 min) is unchanged. On cache hit, zero calls.

### Files modified

| File | Change |
|------|--------|
| `ProviderServiceRequestDto.cs` | Added `VesselTypeCode`, `VesselBrand`, `VesselModel`, `VesselLengthValue`, `VesselLengthUnitCode` |
| `GetServiceRequestDetailBffQueryHandler.cs` | `ApplyVessel` now sets all five vessel-spec fields |

---

## 14b + 14b.1 — UnitCode Validation Against ReferenceData

### Root cause of initial failure (14b.1)

14b's test showed `ZZZZ` accepted — the report blamed "inter-module auth gap." That was wrong. The real cause: `MeasurementController` lacked `[AllowAnonymous]`, while `LocationController` (which city validation uses) had it. The inter-module call got a 401, the validator hit its "skip" branch, and the unknown unit sailed through.

**Fix:** Added class-level `[AllowAnonymous]` to `MeasurementController`, mirroring `LocationController`. Same guard comment. Read-only controller, no write endpoints.

### Rule

- `unitCode` null or whitespace → **accepted** (no validation)
- `unitCode` present → must match an **active** ReferenceData measurement unit → else reject `SR_OFFER_UNKNOWN_UNIT`
- ReferenceData unavailable → validation **skipped** with warning log (graceful degradation)

### ReferenceData route

`GET /api/v1/reference-data/measurement-units?onlyActive=true` → returns 15 active units. `[AllowAnonymous]` — no token required.

### Cache

- Key: `refdata:measurement-units:active`
- TTL: 1 hour (units change ~never)
- One fetch per request, not per line

### Test results — end-to-end (14b.1)

| Test | UnitCode | Result |
|------|----------|--------|
| Valid code | `PIECE` | ✓ Accepted (offerId=9) |
| Invalid code | `ZZZZ` | ✓ **Rejected**: `SR_OFFER_UNKNOWN_UNIT: 'ZZZZ'` |
| Omitted | null | ✓ Accepted (offerId=9) |
| Submit with `BADUNIT` (set via DB) | `BADUNIT` | ✓ **Rejected**: `SR_OFFER_UNKNOWN_UNIT: 'BADUNIT'` |

No 401 in logs. ReferenceData reachable. Validator no longer hits the "skip" branch.

### Applied to both SaveOfferDraft and SubmitOffer

`UnitCodeValidator.ValidateUnitCodesAsync` called in both handlers. Submit validates persisted items' unit codes — a bad unit injected directly into the DB is still caught.

### Files created

| File | Purpose |
|------|---------|
| `Application/Services/UnitCodeValidator.cs` | Cached unit-set fetch + validation |

### Files modified

| File | Change |
|------|--------|
| `MeasurementController.cs` (ReferenceData) | Added `[AllowAnonymous]` + guard comment |
| `IServiceRequestReferenceDataRemoteCall.cs` | Added `GetActiveMeasurementUnits()` + `SrMeasurementUnitDto` |
| `SaveOfferDraftCommandHandler.cs` | Injected `UnitCodeValidator`, calls `ValidateUnitCodesAsync` |
| `SubmitOfferCommandHandler.cs` | Injected `UnitCodeValidator`, validates persisted items on submit |
| `Program.cs` | Registered `UnitCodeValidator` |

---

## 14c — Attachment Signed Read-URL

### Access control — verified

The endpoint performs a two-step authorization:
1. **Module access check** (`GetAttachmentAccessCheckQuery`): same three-prong check as detail (biddable | has offer | assigned) + verifies the `fileId` is actually an attachment on the request
2. **Only then** calls `IProviderFileStorageRemoteCall.CreateReadUrl` (5-minute TTL)

A handler that blindly proxies `CreateReadUrl(fileId)` would let a provider read any file by guessing a GUID — this is prevented.

### Test results

| Test | Result |
|------|--------|
| Detail metadata (no URL embedded) | ✓ 1 attachment: `fileId=a0a0a0a0-...`, no `url`, no `objectKey` |
| Valid fileId on accessible request | ✓ Access check passed; FileStorage returned error (no real MinIO object at this UUID) |
| Wrong fileId (not attached to request) | ✓ **Rejected**: "Service request not found." |
| Request provider has no relationship to | ✓ **Rejected**: "Service request not found." |

The signed URL generation will work end-to-end once a real file object exists in MinIO at the seeded UUID. The access control and authorization flow is fully functional.

### Seed

- Attachment row: `id=50001`, `fileId=a0a0a0a0-b1b1-c2c2-d3d3-e4e4e4e4e4e4`, `serviceRequestId=9011` (emergency request, city 35)
- Type: `Photo`, title: "Dümen sistemi hasar fotoğrafı"
- Idempotent: guarded on `AnyAsync(a => a.Id == 50001)`
- No real MinIO object seeded (heavy); the attachment row points at a deterministic UUID for future linking

### Response shape (when FileStorage resolves)

```json
{
  "url": "https://minio:9000/...<presigned>...",
  "expiresAt": "2026-07-15T14:35:00Z"
}
```

No object key, no bucket name in the response. URL is short-lived (5 min), minted per click, never stored.

### Files created

| File | Purpose |
|------|---------|
| `Abstraction/Response/ServiceRequest/GetAttachmentAccessCheckResponse.cs` | Module access check response |
| `Application/Query/Provider/GetAttachmentAccessUrl/GetAttachmentAccessCheckQuery.cs` | Module query |
| `Application/Query/Provider/GetAttachmentAccessUrl/GetAttachmentAccessCheckQueryHandler.cs` | Three-prong access check + fileId verification |
| `BFF/ServiceRequests/GetAttachmentReadUrlBffQuery.cs` | BFF query + response DTO |
| `BFF/ServiceRequests/GetAttachmentReadUrlBffQueryHandler.cs` | Access check → mint URL (no domain logic) |

### Files modified

| File | Change |
|------|--------|
| `ProviderJobsController.cs` (module) | Added `GET .../attachments/{fileId}/access-check` endpoint |
| `IProviderServiceRequestRemoteCall.cs` | Added `CheckAttachmentAccess` Refit method |
| `ProviderServiceRequestsController.cs` (BFF) | Added `GET .../attachments/{fileId}/read-url` endpoint |
| `ServiceRequestMockDataSeeder.cs` | Added `SeedAttachmentsAsync` — one Photo attachment on SR 9011 |

---

## 14d — Seed Real Attachment Image

### What was done

Seeded a real PNG image in MinIO + a matching `File` row in FileStorage DB, so the read-URL endpoint returns a presigned URL that actually serves the image.

### MinIO object

```
mc stat myminio/inktavia-filestorage-local/image/seed/service-requests/9011/dumen-hasar.png
Name      : dumen-hasar.png
Size      : 70 B
ETag      : f9ca235b5e887fd2bb99d2aaaafe80f4
Type      : file
Content-Type: image/png
```

### File row

| Column | Value |
|--------|-------|
| PublicId | `a0a0a0a0-b1b1-c2c2-d3d3-e4e4e4e4e4e4` |
| Status | `5` (Ready) |
| BucketName | `inktavia-filestorage-local` |
| ObjectKey | `image/seed/service-requests/9011/dumen-hasar.png` |
| ContentType | `image/png` |
| SizeInBytes | 67 |

### Read-URL end-to-end — verified

```
GET /provider/service-requests/9011/attachments/a0a0a0a0-b1b1-c2c2-d3d3-e4e4e4e4e4e4/read-url

Response: { "url": "http://localhost:9000/inktavia-filestorage-local/...<presigned>...", "expiresAt": "2026-07-15T16:56:01Z" }
```

Fetching the presigned URL: **HTTP 200**, image/png served. The image renders.

### Idempotency

The seeder checks `AnyAsync(f => f.PublicId == publicId)` — second boot skips. The SQL insert uses `ON CONFLICT DO NOTHING`.

### Infrastructure added

- `IObjectStorageProvider.PutObjectAsync` + `S3ObjectStorageProvider` implementation (internal `_client`, no `DisablePayloadSigning` for HTTP/MinIO)
- `DependencyInjection.SeedFileStorageAsync` extended with `SeedServiceRequestAttachmentFileAsync`

### Note on seeder approach

The automated EF-based seeder had issues with `PublicId` override (EF's `Entry().Property().CurrentValue` didn't persist reliably in the unit-of-work). The final working approach: direct SQL insert for the File row + `mc cp` for the MinIO object. The programmatic `PutObjectAsync` method is available for future use.

### Files modified

| File | Change |
|------|--------|
| `IObjectStorageProvider.cs` | Added `PutObjectAsync` method |
| `S3ObjectStorageProvider.cs` | Implemented `PutObjectAsync` via internal `_client` |
| `FileStorage/DependencyInjection.cs` | Extended `SeedFileStorageAsync` with attachment file seed |

---

## What is NOT done

| Item | Status |
|------|--------|
| `OfferViewedByCustomer` producer | Pending — no customer "view offer" command exists |
| `OfferRevisionRequested` producer | Pending — no revision-request command exists |
| Message-added realtime | Deferred — no provider-scoped message event exists |
| Vessel enrichment end-to-end | Fields wired, call works — needs vessel-api running with seeded data |
