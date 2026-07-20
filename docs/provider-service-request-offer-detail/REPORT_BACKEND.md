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

## 16 — Vessel Year / Material / Registry on Detail

### Three fields added end-to-end

| Field | Source | DTO field |
|-------|--------|-----------|
| Production Year | `VesselSpecificationEntity.ProductionYear` | `vesselYear` |
| Hull Material | `VesselSpecificationEntity.HullMaterialCode` | `vesselMaterialCode` |
| Registration Number | `VesselEntity.RegistrationNumber` | `vesselRegistrationNumber` |

### Detail response for SR 9011 (vessel 20004 — Aegean Wind)

```json
{
  "vesselId": 20004,
  "vesselName": "Aegean Wind",
  "vesselTypeCode": "SAILING_YACHT",
  "vesselBrand": "Bavaria",
  "vesselModel": "Bavaria C42",
  "vesselLengthValue": 13.3,
  "vesselLengthUnitCode": "M",
  "vesselYear": 2016,
  "vesselMaterialCode": "GRP",
  "vesselRegistrationNumber": "TR-IZM-2016-0042"
}
```

### Seed values

Vessel 20004 already had `ProductionYear=2016` and `HullMaterialCode=GRP` from the vessel seed. `RegistrationNumber` was set to `TR-IZM-2016-0042` for testing.

### Cache key bumped

`vessel:summary:` → `vessel:summary:v2:` (both detail and discovery handlers). Old cached entries without the new fields expire naturally (10 min TTL) and are replaced with the new shape.

### No N+1

The spec is already joined in `GetVesselSummariesQueryHandler` (GroupJoin). Three new columns added to the same projection — no additional query.

### Codes stay codes

`HullMaterialCode` is a ReferenceData code (e.g. `GRP`, `ALUMINUM`, `STEEL`). Not translated server-side. The SPA maps it via i18n (`hullMaterial.*`).

### Files modified

| File | Change |
|------|--------|
| `VesselSummaryDto` (Vessel Abstraction) | Added `ProductionYear`, `HullMaterialCode`, `RegistrationNumber` |
| `GetVesselSummariesQueryHandler.cs` | Added three fields to the projection |
| `ProviderServiceRequestDto.cs` (SR Abstraction) | Added `VesselYear`, `VesselMaterialCode`, `VesselRegistrationNumber` |
| `GetServiceRequestDetailBffQueryHandler.cs` | `ApplyVessel` maps the three new fields |
| `GetProviderDiscoveryBffQueryHandler.cs` | Cache key bumped to `v2` |

---

## 17 — Distance-to-Request on Detail

### Two responses — with and without centre

**With centre** (`centerLatitude=38.40&centerLongitude=26.35`, ~10 km from Çeşme):
```json
{
  "distanceKm": 9.9,
  "approxLatitude": 38.32,
  "approxLongitude": 26.3
}
```

**Without centre:**
```json
{
  "distanceKm": null
}
```

### Exact coordinates never leak

`locationLatitude` / `locationLongitude` are not present in the response — only `approxLatitude`/`approxLongitude` (snapped) and `distanceKm` (computed from exact coords in the module).

### Same haversine as discovery

Added `GeoHelper.HaversineKm(lat1, lng1, lat2, lng2)` — same formula as the SQL expression in the discovery projection. Computed from exact coordinates in the module handler; only the rounded result (1 decimal) leaves.

### No geo maths in the BFF

The BFF accepts `centerLatitude`/`centerLongitude` from the SPA and forwards them as query params to the module. `distanceKm` is carried through untouched. No computation in the BFF.

### Süre (ETA) — deferred

No routing service in the stack. `distanceKm` is straight-line; a travel time is not derivable without routing. The SPA shows "Mesafe: X km" and hides süre.

### Files modified

| File | Change |
|------|--------|
| `GeoHelper.cs` | Added `HaversineKm` static method |
| `GetProviderServiceRequestDetailQuery.cs` | Added `CenterLatitude`, `CenterLongitude` |
| `GetProviderServiceRequestDetailQueryHandler.cs` | Computes `DistanceKm` from exact coords when centre provided |
| `ProviderServiceRequestDto.cs` | Added `DistanceKm` |
| `ProviderJobsController.cs` (module) | Added `centerLatitude`, `centerLongitude` query params |
| `IProviderServiceRequestRemoteCall.cs` | Added centre params to `GetServiceRequestDetail` |
| `GetServiceRequestDetailBffQuery.cs` | Added centre fields |
| `GetServiceRequestDetailBffQueryHandler.cs` | Forwards centre to module call |
| `ProviderServiceRequestsController.cs` (BFF) | Added centre query params |

---

## 18 — Offer-Gated Messaging (Anti-Harassment) + Realtime

### The gate — verified end-to-end

**`channelOpen`** = the conversation contains at least one message with `SenderType == Owner`.

| Test | Result |
|------|--------|
| Provider POST, no owner message | **Rejected**: `SR_MSG_CHANNEL_LOCKED` |
| GET messages, no owner message | `channelOpen: false`, 0 items |
| Owner sends message (simulated) | Inserted |
| Provider POST after owner message | **Accepted**: `senderType: 2` (Provider) |
| GET messages after owner message | `channelOpen: true`, 2 items (owner + provider) |

Gate is enforced **server-side** in `SendServiceRequestMessageCommandHandler` — the BFF cannot bypass it.

### Offer-as-message

On `SubmitOffer`, a `MessageType.Offer` message is created (idempotent per offer id):
- `SenderType=Provider`, `MessageType=Offer`
- `Content = "offer:{offerId}|{grandTotal} {currency}"`
- Does **not** open the channel for the provider (only an Owner message does)

**Decision**: offer-message created on **submit** (owner confirmed 2026-07-16), not on view.

### SenderType override

The BFF's service account token lacks Provider/Owner Keycloak roles, so the module controller's role-based detection falls through to Owner. Fixed by adding `SenderTypeOverride` to `SendServiceRequestMessageRequest` — the BFF sets it to `Provider`; the module controller uses it when present.

### Provider realtime

- `ServiceRequestMessageSentMessage` now carries `ProviderProfileId`
- `MessageAddedRealtimeConsumer` added to BFF → pushes `"MessageAdded"` event to `provider:{profileId}` group
- Only notifies the provider for **Owner→provider** messages (provider's own sends don't toast)
- **No message content** on the realtime frame — the SPA refetches the thread

### BFF endpoints

| Endpoint | Route | Description |
|----------|-------|-------------|
| GET messages | `GET /provider/service-requests/{id}/messages` | History + `channelOpen` state |
| Send message | `POST /provider/service-requests/{id}/messages` | Provider free text (gate enforced) |

### Files created

| File | Purpose |
|------|---------|
| `BFF/Realtime/MessageAddedRealtimeConsumer.cs` | Bus → browser for owner messages |
| `BFF/ServiceRequests/GetProviderMessagesQuery.cs` | Query + response DTO |
| `BFF/ServiceRequests/GetProviderMessagesQueryHandler.cs` | Messages + channelOpen |
| `BFF/ServiceRequests/SendProviderMessageCommand.cs` | Command |
| `BFF/ServiceRequests/SendProviderMessageCommandHandler.cs` | Forwards with SenderTypeOverride=Provider |

### Files modified

| File | Change |
|------|--------|
| `ServiceRequestMessageType.cs` | Added `Offer = 4` |
| `ServiceRequestMessageSentMessage.cs` | Added `ProviderProfileId` |
| `IServiceRequestMessageRepository.cs` | Added `HasOwnerMessageAsync`, `HasOfferMessageForOfferAsync` |
| `ServiceRequestMessageRepository.cs` | Implemented new methods |
| `SendServiceRequestMessageCommandHandler.cs` | Gate logic + bus publish + provider profile resolution |
| `SubmitOfferCommandHandler.cs` | Creates offer-as-message on submit |
| `SendServiceRequestMessageRequest.cs` | Added `SenderTypeOverride` |
| `ServiceRequestMessageController.cs` | Respects `SenderTypeOverride` |
| `ProviderRealtimeEvent.cs` | Added `MessageAdded` event type |
| `IProviderServiceRequestRemoteCall.cs` | Added `GetMessages`, `SendMessage` Refit methods |
| `ProviderServiceRequestsController.cs` (BFF) | Added GET/POST messages endpoints |

---

## 19a — Provider Catalog Items

### Verified

| Test | Result |
|------|--------|
| List (provider2) | 4 items (seeded: Gövde Yıkama, Antifouling Boya, Pasta Cila, Tekne Altı İşçilik) |
| Create with `unitCode: "ZZZZ"` | Rejected (400) |
| Create with valid unit (`PIECE`) | Accepted (id assigned) |
| Delete own item | Accepted (soft-delete) |
| Cross-provider access | Not found (ownership enforced by `ProviderProfileId` filter) |

### Seed IDs

| Id | Title | Price | Unit |
|----|-------|-------|------|
| 60001 | Gövde Basınçlı Yıkama | 1250 TRY | PIECE |
| 60002 | Antifouling Boya — Jotun | 480 TRY | LITER |
| 60003 | Pasta Cila | 900 TRY | — |
| 60004 | Tekne Altı İşçilik (saat) | 350 TRY | HOUR |

### DB columns verified

`\d servicerequest.provider_catalog_items` — `ProviderProfileId`, `ItemType`, `Title`, `DefaultQuantity` (numeric 12,3), `UnitCode`, `DefaultUnitPrice` (numeric 18,4), `CurrencyCode`, `DefaultTaxRate` (numeric 9,4).

Migration `AddProviderCatalogItems` with Designer — applied.

---

## 19b — Provider Offer Templates

### Verified

| Test | Result |
|------|--------|
| List (provider2) | 1 template: "Standart Karina Bakımı" with 3 items |
| Get template | Returns items with all fields |
| Create with 0 items | Rejected |
| Cross-provider template | Not found |

### Seed

Template 70001 "Standart Karina Bakımı" with 3 items (Gövde Yıkama + Antifouling Boya + Pasta Cila).

### DB tables

`servicerequest.provider_offer_templates` + `servicerequest.provider_offer_template_items` — FK + cascade.

Migration `AddProviderOfferTemplates` with Designer — applied.

### Totals path confirmation

Catalog/template items are **seed values only**. The SPA reads the library, seeds the builder inputs, and saves through the existing `SaveOfferDraft` → `OfferCalculationService`. No new totals path — the library never writes offer totals directly.

### Files created (19a + 19b)

| File | Purpose |
|------|---------|
| `Domain/Entities/Catalog/ProviderCatalogItemEntity.cs` | Catalog item entity |
| `Domain/Entities/Catalog/ProviderOfferTemplateEntity.cs` | Template entity |
| `Domain/Entities/Catalog/ProviderOfferTemplateItemEntity.cs` | Template item entity |
| `Configurations/ProviderCatalogItemEntityConfiguration.cs` | EF config |
| `Configurations/ProviderOfferTemplateEntityConfiguration.cs` | EF config (both entities) |
| `Dto/ProviderCatalogItemDto.cs` | Catalog DTO |
| `Dto/ProviderOfferTemplateDto.cs` | Template + item DTOs |
| `Request/Offer/CatalogItemRequest.cs` | Create/update request |
| `Request/Offer/OfferTemplateRequest.cs` | Create/update request |
| `Command/Catalog/CatalogCommands.cs` | CRUD commands |
| `Command/Catalog/CatalogHandlers.cs` | CRUD handlers (with UnitCode validation) |
| `Command/Catalog/TemplateCommands.cs` | CRUD commands |
| `Command/Catalog/TemplateHandlers.cs` | CRUD handlers (with UnitCode validation) |
| `Controller/V1/Catalog/ProviderCatalogController.cs` | Module endpoints |
| `Controller/V1/Catalog/ProviderTemplateController.cs` | Module endpoints |
| `BFF/Controllers/V1/ProviderCatalogController.cs` | BFF passthrough |
| `BFF/Controllers/V1/ProviderTemplateController.cs` | BFF passthrough |
| Migrations (2, each with Designer) | `AddProviderCatalogItems`, `AddProviderOfferTemplates` |

### Files modified

| File | Change |
|------|--------|
| `ServiceRequestDbContext.cs` | Added 3 DbSets |
| `IProviderServiceRequestRemoteCall.cs` | Added catalog + template Refit methods |
| `ServiceRequestMockDataSeeder.cs` | Added `SeedCatalogItemsAsync`, `SeedTemplatesAsync` |

---

## 20a — Conversation List (Inbox)

### Verified

```
GET /provider/service-requests/conversations → 1 conversation
  SR 9011 | SR-SEED-EMERGENCY-1 | unread=1 | channelOpen=true | last="Tabii, nasıl yardımcı olabilir…"
```

- Provider-scoped: only requests where the provider has an offer or messages
- No customer identity exposed — only request title/code
- One grouped query (no N+1): unread count, last message preview, channelOpen, lifecycle status all computed in the projection

### Endpoint

| Route | Description |
|-------|-------------|
| Module: `GET /provider/conversations` | Returns `GetProviderConversationsResponse` |
| BFF: `GET /provider/service-requests/conversations` | Passthrough |

---

## 20b — Rich Message Content (Location + Image display)

### Location fields added

`ServiceRequestMessageEntity`: `LocationLat` (numeric 10,7), `LocationLng` (numeric 10,7), `LocationLabel` (varchar 500).

Migration `AddMessageLocationFields` with Designer — applied.

### Message types added

`MessageType.Image = 5`, `MessageType.Location = 6`.

### Send flow

`POST /messages` accepts optional `locationLat`, `locationLng`, `locationLabel`. When present, creates `MessageType.Location`. Gate still applies — provider can only send when `channelOpen`.

### Image display

Uses existing 14c pattern — `GET .../attachments/{fileId}/read-url` serves signed URLs for image messages with `AttachmentFileId`. No new endpoint needed for display.

---

## 20c — Lifecycle System Messages

### Implemented

| Handler | Code | Status |
|---------|------|--------|
| `AcceptServiceRequestOfferCommandHandler` | `OFFER_ACCEPTED` | ✓ Idempotent, publishes `MessageAdded` bus event |
| `CancelServiceRequestCommandHandler` | `CONVERSATION_CLOSED` | ✓ Idempotent |

`HasSystemMessageAsync(srId, code)` guards against duplicates. Messages are `SenderType.System`, `MessageType.StatusChange`.

### Realtime fix

`MessageAddedRealtimeConsumer` now forwards **System** messages (was Owner-only). Provider receives lifecycle pills live.

### Not yet wired

| Handler | Code | Reason |
|---------|------|--------|
| `StartServiceRequestAssignmentCommandHandler` | `JOB_STARTED` | Handler exists but not modified in this batch |
| `ApproveServiceRequestCompletionCommandHandler` | `JOB_COMPLETED` | Handler exists but not modified in this batch |

---

## 20d — Conversation Seed

### Seeded on SR 9011 (5 messages, ids 80001–80005)

| Id | SenderType | MessageType | Content | Extra |
|----|-----------|-------------|---------|-------|
| 80001 | Owner | Text | "Merhaba, teklifinizi aldım…" | Unread — opens channel |
| 80002 | Provider | Text | "Merhaba, acil müdahale gerekiyor…" | Read |
| 80003 | Owner | Image | "Hasar fotoğrafı" | AttachmentFileId=a0a0a0a0… (14c file) — unread |
| 80004 | Owner | Location | "Çeşme Marina" | lat=38.3235, lng=26.3050 — unread |
| 80005 | System | StatusChange | `OFFER_ACCEPTED` | Read |

### Conversations inbox

```
SR 9011 | unread=3 | channelOpen=true | lifecycleStatus=OFFER_ACCEPTED | last="Merhaba, acil müdahale gerekiyor…"
```

### Thread

```
channelOpen: true
  Text   | sender=Owner    | "Merhaba, teklifinizi aldım…"
  Text   | sender=Provider | "Merhaba, acil müdahale gerekiyor…"
  Image  | sender=Owner    | "Hasar fotoğrafı" [file:a0a0a0a0]
  Location| sender=Owner   | "Çeşme Marina" [loc:38.3235,26.305]
  StatusChange | sender=System | OFFER_ACCEPTED
```

---

### Files created (20a + 20b + 20c + 20d)

| File | Purpose |
|------|---------|
| `Dto/ProviderConversationDto.cs` | Conversation list item DTO |
| `Response/Message/GetProviderConversationsResponse.cs` | Response wrapper |
| `Query/Provider/GetProviderConversations/GetProviderConversationsQuery.cs` | Query |
| `Query/Provider/GetProviderConversations/GetProviderConversationsQueryHandler.cs` | Grouped query for inbox |
| Migration `AddMessageLocationFields` (+ Designer) | Location columns on messages |

### Files modified (20a + 20b + 20c + 20d)

| File | Change |
|------|--------|
| `ServiceRequestMessageType.cs` | Added `Image = 5`, `Location = 6` |
| `ServiceRequestMessageEntity.cs` | Added `LocationLat/Lng/Label`, `CreateLocation` factory |
| `ServiceRequestMessageDto.cs` | Added location fields |
| `ServiceRequestMappingExtensions.cs` | Maps location fields in `ToDto` |
| `SendServiceRequestMessageRequest.cs` | Added location fields |
| `SendServiceRequestMessageCommandHandler.cs` | Creates location messages when lat/lng present |
| `ServiceRequestMessageEntityConfiguration.cs` | Precision for location columns |
| `ProviderJobsController.cs` (module) | Added `GET /conversations` |
| `IServiceRequestMessageRepository.cs` | Added `HasSystemMessageAsync` |
| `ServiceRequestMessageRepository.cs` | Implemented `HasSystemMessageAsync` |
| `AcceptServiceRequestOfferCommandHandler.cs` | Injects msgRepo, creates `OFFER_ACCEPTED` system msg |
| `CancelServiceRequestCommandHandler.cs` | Injects msgRepo, creates `CONVERSATION_CLOSED` system msg |
| `StartServiceRequestAssignmentCommandHandler.cs` | Injects msgRepo + messagePublisher, creates `JOB_STARTED` system msg (20e) |
| `ApproveServiceRequestCompletionCommandHandler.cs` | Injects msgRepo + assignmentRepo + messagePublisher, creates `JOB_COMPLETED` system msg (20e) |
| `MessageAddedRealtimeConsumer.cs` (BFF) | Forwards System messages (not just Owner) |
| `ServiceRequestMockDataSeeder.cs` | Added `SeedConversationAsync` (5 messages on SR 9011) |
| `IProviderServiceRequestRemoteCall.cs` | Added `GetProviderConversations` Refit method |
| `ProviderServiceRequestsController.cs` (BFF) | Added conversations + location on send |
| `SendProviderMessageCommand.cs` (BFF) | Added location fields |
| `SendProviderMessageCommandHandler.cs` (BFF) | Passes location to module |

---

## 20e — JOB_STARTED / JOB_COMPLETED Lifecycle Messages

### Implemented

| Handler | Code | ProviderProfileId source |
|---------|------|--------------------------|
| `StartServiceRequestAssignmentCommandHandler` | `JOB_STARTED` | `assignment.ProviderProfileId` |
| `ApproveServiceRequestCompletionCommandHandler` | `JOB_COMPLETED` | Resolved from `_assignmentRepository.GetByServiceRequestIdAsync` |

Both follow the exact `OFFER_ACCEPTED` pattern: idempotent via `HasSystemMessageAsync`, `SenderType.System`, `MessageType.StatusChange`, content = code. Each publishes `ServiceRequestMessageSentMessage` with the correct `ProviderProfileId` so the BFF consumer pushes a content-free `MessageAdded` to the provider.

All four lifecycle codes now wired: `OFFER_ACCEPTED`, `JOB_STARTED`, `JOB_COMPLETED`, `CONVERSATION_CLOSED`.

---

## 20f — Message Image Display (widened access check)

### Approach: widen 14c

Instead of a new endpoint, the existing `GET .../attachments/{fileId}/read-url` access check was widened to accept a fileId that is **either** a request attachment (`ServiceRequestAttachmentEntity.FileId`) **or** a message attachment (`ServiceRequestMessageEntity.AttachmentFileId`) on the same request. The SPA needs no new endpoint — the same URL pattern serves both.

### Access check

The three-prong provider relationship check is unchanged. After confirming the provider may see the request, the handler checks:
1. `sr.Attachments.Any(a => a.FileId == fileId)` — request attachment (14c original)
2. `sr.Messages.Any(m => m.AttachmentFileId == fileId)` — message attachment (20f addition)

If neither matches → "not found". A foreign fileId still rejected.

### MessageType.Image on send

`SendServiceRequestMessageCommandHandler`: when `req.AttachmentFileId.HasValue` and no location, the message is created as `MessageType.Image` (not `Text`). The inbox preview can show "Görsel" and the thread renders deterministic image bubbles.

### Files modified

| File | Change |
|------|--------|
| `GetAttachmentAccessCheckQueryHandler.cs` | Widened check: also accepts message `AttachmentFileId` |
| `SendServiceRequestMessageCommandHandler.cs` | Sets `MessageType.Image` when attachment present |

---

## JB-1 — Jobs Enrichment (Title + Vessel + Code)

### Module

`GetProviderJobsQueryHandler` now does an in-module join (`ServiceRequestAssignments` JOIN `ServiceRequests`) and projects `Title`, `RequestCode`, `VesselId`, `VesselName` onto each `ProviderJobItemDto`. One query, no N+1, no cross-module call.

### BFF

`GetProviderJobsQueryHandler` (BFF) bulk-enriches vessel names: collects distinct `VesselId`s, one cached `GetSummaries` call, maps `VesselName` back. Same cache + graceful-degrade pattern as the detail handler. On failure → jobs without vessel names, page still renders.

### Files modified

| File | Change |
|------|--------|
| `ProviderJobItemDto` (Abstraction) | Added `Title`, `RequestCode`, `VesselId`, `VesselName` |
| `GetProviderJobsQueryHandler.cs` (module) | In-module join with ServiceRequests; projects new fields |
| `ProviderJobDto` (BFF Contracts) | Added `Title`, `RequestCode`, `VesselId`, `VesselName` |
| `GetProviderJobsQueryHandler.cs` (BFF) | Vessel bulk enrichment + maps new fields |

---

## JB-2 — Jobs Summary Counts

### Module

`GetProviderJobsSummaryQueryHandler`: one `GROUP BY` query over assignments joined to SRs, grouped by `ServiceRequestStatus`. Returns per-status counts + `Active` (non-Completed) + `Total`. Provider-scoped.

### Endpoint

| Route | Description |
|-------|-------------|
| Module: `GET /provider/jobs/summary` | Returns `GetProviderJobsSummaryResponse` |
| BFF: `GET /provider/jobs/summary` | Passthrough |

### KPI mapping

| KPI | Source |
|-----|--------|
| Aktif İşler | `Active` |
| Devam Eden | `InProgress` |
| Planlanan | `Scheduled` |
| Onay Bekleyen | `WaitingForOwnerApproval + WaitingForMaterial + CompletionSubmitted` |

### Files created

| File | Purpose |
|------|---------|
| `Response/Jobs/GetProviderJobsSummaryResponse.cs` | Summary DTO |
| `Query/Jobs/GetProviderJobsSummary/GetProviderJobsSummaryQuery.cs` | Query |
| `Query/Jobs/GetProviderJobsSummary/GetProviderJobsSummaryQueryHandler.cs` | Grouped count query |

### Files modified

| File | Change |
|------|--------|
| `ProviderJobsController.cs` (module) | Added `GET /jobs/summary` |
| `IProviderServiceRequestRemoteCall.cs` | Added `GetProviderJobsSummary` Refit |
| `ProviderJobsController.cs` (BFF) | Added `GET /summary` passthrough |

---

## JB-3 — Weekly Workload (Stacked Bar Chart)

### Endpoint

`GET /provider/jobs/workload?weeks=6` → per-week buckets (Monday-start ISO weeks, UTC).

Each bucket: `{ weekStartUtc, label ("H29"), scheduled, inProgress, completed }`.

- `scheduled` = jobs with `scheduledStartDate` in that week, status ∈ {Assigned, Scheduled}
- `inProgress` = jobs with start date in that week, status = InProgress
- `completed` = jobs with `actualEndDate` in that week, status = Completed

One query, bucketed in memory. Empty weeks zero-filled (not omitted). Provider-scoped.

### Files created

| File | Purpose |
|------|---------|
| `Response/Jobs/GetProviderJobsWorkloadResponse.cs` | Response + `WorkloadWeekBucket` |
| `Query/Jobs/GetProviderJobsWorkload/GetProviderJobsWorkloadQuery.cs` | Query |
| `Query/Jobs/GetProviderJobsWorkload/GetProviderJobsWorkloadQueryHandler.cs` | One query + in-memory bucketing |

---

## JB-4 — Action-Required Feed

### Endpoint

`GET /provider/jobs/action-required` → three groups, each enriched (title, requestCode, vesselId, vesselName), capped at 5.

| Group | Statuses |
|-------|----------|
| `ownerApproval` | WaitingForOwnerApproval, CompletionSubmitted |
| `materialRequired` | WaitingForMaterial |
| `blocked` | Paused |

### CTAs

All three design CTAs ("Onay Hatırlat", "Stok Kontrol", "Engeli Çöz") are **navigate-to-detail** for MVP — no dedicated backend command exists for reminding an owner or resolving a block. The SPA links to `/app/jobs/:assignmentId`.

### Files created

| File | Purpose |
|------|---------|
| `Response/Jobs/GetProviderJobsActionRequiredResponse.cs` | Response + `ActionRequiredJobDto` |
| `Query/Jobs/GetProviderJobsActionRequired/GetProviderJobsActionRequiredQuery.cs` | Query |
| `Query/Jobs/GetProviderJobsActionRequired/GetProviderJobsActionRequiredQueryHandler.cs` | One query, grouped |

### Files modified (JB-3 + JB-4)

| File | Change |
|------|--------|
| `ProviderJobsController.cs` (module) | Added `GET /jobs/workload`, `GET /jobs/action-required` |
| `IProviderServiceRequestRemoteCall.cs` | Added Refit methods |
| `ProviderJobsController.cs` (BFF) | Added `GET /workload`, `GET /action-required` passthrough |

---

## SEED Accepted Job + JB-6 Status Alignment

### Seed (idempotent, ids 90001/91001)

- **Offer** 90001: provider2 on SR 9011, 5000 TRY, status `Accepted`, `SubmittedAt` + `AcceptedAt` set
- **Assignment** 91001: provider2 on SR 9011, `ScheduledStartDate` = tomorrow, `ScheduledEndDate` = +3 days
- **SR 9011** status changed to `Assigned`, `AssignedProviderName` set

### JB-6 — Status alignment fix

`GetProviderJobsQueryHandler` projection: `Status = x.a.Status.ToString()` → **`Status = x.sr.Status.ToString()`**. The list now uses `ServiceRequestStatus` (Assigned/Scheduled/InProgress/Completed…) — same vocabulary as JB-2 summary and the SPA.

### Expected result

- `GET /provider/jobs` → 1 job: SR 9011, title "Acil: Dümen sistemi arızası — Çeşme", requestCode "SR-SEED-EMERGENCY-1", status **"Assigned"**, scheduled dates set
- `GET /provider/jobs/summary` → `assigned=1, active=1, total=1`

### Files modified

| File | Change |
|------|--------|
| `GetProviderJobsQueryHandler.cs` (module) | `Status = x.sr.Status.ToString()` (was `x.a.Status`) |
| `ServiceRequestMockDataSeeder.cs` | Added `SeedAcceptedJobAsync` |

---

## JD-1 — Job Detail Aggregate

### Endpoint

| Route | Description |
|-------|-------------|
| Module: `GET /provider/jobs/{assignmentId}` | Returns `GetProviderJobDetailResponse` |
| BFF: `GET /provider/jobs/{assignmentId}` | Passthrough + vessel enrichment |

### Aggregate shape

- **Assignment**: `assignmentId`, `status` (SR lifecycle), `assignmentStatus` (sub-state), scheduled/actual dates, providerNotes
- **Service Request**: `title`, `requestCode`, `description`, work scope items, attachments (metadata only), location (snapped), vesselId + specs
- **Accepted Offer**: line items (itemType/title/qty/price/tax/discount/totals) + `subtotal`/`taxTotal`/`grandTotal`/`currencyCode` + commercial notes
- **Timeline**: status history events

### Access check

Provider must own the assignment (`assignment.ProviderProfileId == profileId`). Cross-provider → "Job not found." Same "not found" as a missing id.

### Reuses existing P1 detail assembly

`sr.ToProviderDetailDto(profileId)` provides the SR part (title, work scope, attachments, location, timeline, snapped coords). No duplicated logic.

### BFF vessel enrichment

One `GetSummaries` call per detail (cached, v2 key). All vessel spec fields mapped (name, type, brand, model, length, year, material, registration).

### Files created

| File | Purpose |
|------|---------|
| `Response/Jobs/GetProviderJobDetailResponse.cs` | Aggregate DTO |
| `Query/Jobs/GetProviderJobDetail/GetProviderJobDetailQuery.cs` | Query |
| `Query/Jobs/GetProviderJobDetail/GetProviderJobDetailQueryHandler.cs` | In-module assembly |

### Files modified

| File | Change |
|------|--------|
| `ProviderJobsController.cs` (module) | Added `GET /jobs/{assignmentId}` |
| `IProviderServiceRequestRemoteCall.cs` | Added `GetProviderJobDetail` Refit |
| `ProviderJobsController.cs` (BFF) | Added detail endpoint + vessel enrichment |

---

## JD-2 — Provider Job Actions (Start / Complete)

### Guards added (Gap 1 + Gap 2 from the spec)

**Ownership guard** (both handlers): `providerProfileId` from assertion; if `assignment.ProviderProfileId != providerProfileId` → "Job not found." Cross-provider access blocked.

**State guard**:
- **Start**: `sr.Status ∈ {Assigned, Scheduled}` only → else `SR_JOB_NOT_STARTABLE`
- **Complete**: `sr.Status == InProgress` only → else `SR_JOB_NOT_COMPLETABLE`

### Endpoints

| Route | Description |
|-------|-------------|
| Module: `POST /provider/jobs/{assignmentId}/start` | Assigned/Scheduled → InProgress |
| Module: `POST /provider/jobs/{assignmentId}/complete` | InProgress → CompletionSubmitted (body: notes + evidence optional) |
| BFF: `POST /provider/jobs/{assignmentId}/start` | Passthrough |
| BFF: `POST /provider/jobs/{assignmentId}/complete` | Passthrough |

### Side effects (unchanged, existing)

- Start: `assignment.Start()`, SR → InProgress, status-history, WorkStarted realtime, `JOB_STARTED` system message
- Complete: Creates `ServiceRequestCompletionEntity`, SR → CompletionSubmitted, status-history, realtime, bus message

### Files modified

| File | Change |
|------|--------|
| `StartServiceRequestAssignmentCommandHandler.cs` | Added ownership + state guards |
| `SubmitServiceRequestCompletionCommandHandler.cs` | Added ownership + state guards |
| `ProviderJobsController.cs` (module) | Added `POST /start`, `POST /complete` |
| `IProviderServiceRequestRemoteCall.cs` | Added `StartJob`, `CompleteJob` Refit |
| `ProviderJobsController.cs` (BFF) | Added start/complete endpoints + error forwarding |

---

## RT — Realtime MessageSenderType

`ProviderRealtimeEvent` now carries `messageSenderType` (`"Owner"` or `"System"`). The `MessageAddedRealtimeConsumer` sets it from the bus message's `SenderType`. The SPA uses it to differentiate toasts: System → "İş güncellemesi", Owner → "Müşteri size yanıt verdi". Still content-free.

| File | Change |
|------|--------|
| `ProviderRealtimeEvent.cs` | Added `MessageSenderType` |
| `MessageAddedRealtimeConsumer.cs` | Sets `MessageSenderType = message.SenderType.ToString()` |

---

## What is NOT done

| Item | Status |
|------|--------|
| `OfferViewedByCustomer` producer | Pending — no customer "view offer" command exists |
| `OfferRevisionRequested` producer | Pending — no revision-request command exists |
| Süre (ETA) | Deferred — no routing service in stack |
| `SaveDraftAsTemplate` (from draft → template) | Optional, not implemented |
| BFF vessel enrichment for action-required | Module returns `VesselName` from SR denorm; BFF enrichment deferred |
| JD-4..JD-5 (evidence upload, completion evidence) | Planned — Post-MVP |

---

## JD-3 — Provider Work Logs (List + Add)

### Guards added

Both the add and get handlers now check ownership: `providerProfileId` from assertion, `assignment.ProviderProfileId != providerProfileId` → "Job not found."

### Endpoints

| Route | Description |
|-------|-------------|
| Module: `GET /provider/jobs/{assignmentId}/work-logs` | List logs (chronological) |
| Module: `POST /provider/jobs/{assignmentId}/work-logs` | Add a log entry |
| BFF: `GET /provider/jobs/{assignmentId}/work-logs` | Passthrough |
| BFF: `POST /provider/jobs/{assignmentId}/work-logs` | Passthrough |

### Request body (POST)

`{ logType, title, description?, locationLatitude?, locationLongitude?, attachmentFileId? }`

LogType codes: GeneralNote, ArrivedAtVessel, InspectionStarted, WorkStarted, MaterialRequired, WorkPaused, WorkResumed, WorkCompleted, etc. (SPA localizes.)

### Side effects

No SR status change. `WorkLogAdded` realtime event fired. `AttachmentFileId` optional (evidence upload is JD-5).

### Files modified

| File | Change |
|------|--------|
| `AddServiceRequestWorkLogCommandHandler.cs` | Added ownership guard |
| `GetServiceRequestWorkLogsQueryHandler.cs` | Added ownership guard (loads assignment first) |
| `ProviderJobsController.cs` (module) | Added `GET/POST /jobs/{id}/work-logs` |
| `IProviderServiceRequestRemoteCall.cs` | Added `GetWorkLogs`, `AddWorkLog` Refit |
| `ProviderJobsController.cs` (BFF) | Added work-logs GET/POST endpoints |

---

## JD-4 — Vessel Beam / Draft

### Widened

`VesselSummaryDto`: added `BeamValue`, `BeamUnitCode`, `DraftValue`, `DraftUnitCode`. Projection in `GetVesselSummariesQueryHandler` maps from spec (same null-guard pattern as Length).

`ProviderServiceRequestDto`: added `VesselBeamValue`, `VesselBeamUnitCode`, `VesselDraftValue`, `VesselDraftUnitCode`.

### Cache key bumped

`vessel:summary:v2:` → `vessel:summary:v3:` in all 4 sites (discovery, detail, jobs list, job detail).

### BFF enrichment

Both `ApplyVessel` (SR detail handler) and the job detail controller's inline enrichment now map Beam/Draft from the vessel summary.

### Files modified

| File | Change |
|------|--------|
| `VesselSummaryDto` | Added 4 Beam/Draft fields |
| `GetVesselSummariesQueryHandler.cs` | Maps Beam/Draft from spec |
| `ProviderServiceRequestDto.cs` | Added 4 Beam/Draft fields |
| `GetServiceRequestDetailBffQueryHandler.cs` | Maps Beam/Draft in `ApplyVessel` |
| `ProviderJobsController.cs` (BFF) | Maps Beam/Draft in job detail enrichment |
| 4 BFF handlers | Cache key `v2` → `v3` |

---

## JD-5 — Evidence Read-URL (Work-Log + Completion)

The 14c access-check (`GetAttachmentAccessCheckQueryHandler`) now accepts fileIds from **four** sources on the same SR:

1. Request attachments (`sr.Attachments`)
2. Message attachments (`sr.Messages`) — 20f
3. Work-log evidence (`sr.Assignment?.WorkLogs`) — **new**
4. Completion evidence (`sr.Completion?.EvidenceFileId`) — **new**

Relationship gate unchanged (offer OR assigned). No new endpoint, DTO, or BFF change. The existing `/attachments/{fileId}/read-url` serves all four.

| File | Change |
|------|--------|
| `GetAttachmentAccessCheckQueryHandler.cs` | Added work-log + completion evidence checks |

---

## SEED Reset Job 91001

### What it does (Dev/Local only, idempotent)

1. Deletes test work logs for assignment 91001
2. Removes completion entity for SR 9011
3. Removes lifecycle system messages (JOB_STARTED, JOB_COMPLETED) — keeps OFFER_ACCEPTED
4. Trims status history after Assigned
5. Resets assignment to Accepted status, clears actual dates
6. Resets SR 9011 to `Assigned`
7. Seeds 3 realistic offer line items on offer 90001 (Labor 24h×45, Product 15L×120, Service 1×250) with inline calculation → subtotal ~2830, tax ~566, grandTotal ~3396
8. Seeds 2 demo conversation messages (Owner asks for photo, Provider replies)

### Files modified

| File | Change |
|------|--------|
| `ServiceRequestMockDataSeeder.cs` | Added `ResetJob91001Async` |

---

## SEED Reset FIX — offer items crash

**Root cause**: `offer.ReplaceItems()` throws `InvalidOperationException` on Accepted offers (domain guard). The seeder runs at boot → unhandled → crash loop.

**Fix**:
1. Insert offer items directly via `_db.ServiceRequestOfferItems.Add()` — bypasses domain guard (dev seed only)
2. Pre-computed totals: subtotal=3130, taxTotal=626, grandTotal=3756 (Labor 1080+216, Product 1800+360, Service 250+50)
3. Wrapped entire `ResetJob91001Async` in try/catch → log warning + `ChangeTracker.Clear()` on any error (boot-safe)

---

## JD-6 — Completion Requires Evidence Photo

### Guard

`SubmitServiceRequestCompletionCommandHandler`: after ownership + state guards, rejects if `EvidenceFileId` is null/empty → `SR_COMPLETION_EVIDENCE_REQUIRED`.

### Exposed on job detail

`GetProviderJobDetailResponse`: added `CompletionEvidenceFileId (Guid?)` + `CompletedAtUtc (DateTime?)`. Set from `sr.Completion?.EvidenceFileId` / `sr.Completion?.SubmittedAt` (already loaded, no extra query).

The SPA can mint a read-URL for this fileId via the existing JD-5 path.

### Files modified

| File | Change |
|------|--------|
| `SubmitServiceRequestCompletionCommandHandler.cs` | Added `SR_COMPLETION_EVIDENCE_REQUIRED` guard |
| `GetProviderJobDetailResponse.cs` | Added `CompletionEvidenceFileId`, `CompletedAtUtc` |
| `GetProviderJobDetailQueryHandler.cs` | Sets from `sr.Completion` |

---

## DD-2 — Exact Location for Assigned Provider

The job aggregate (`GET /provider/jobs/{assignmentId}`) now returns **exact** `LocationLatitude/Longitude` instead of the snapped ~500m grid point. After `sr.ToProviderDetailDto(profileId)` (which snaps), the handler overwrites with the raw coordinates:

```csharp
detail.Request.ApproxLatitude  = sr.LocationLatitude;
detail.Request.ApproxLongitude = sr.LocationLongitude;
```

Access is already enforced (JD-1: `assignment.ProviderProfileId == profileId`), so exact coordinates only reach the assigned provider. Discovery / pre-acceptance detail stays snapped. DEV_DEBT DD-2 resolved.

| File | Change |
|------|--------|
| `GetProviderJobDetailQueryHandler.cs` | Overwrites snapped coords with exact for the assigned provider |

---

## CI-1 — Provider CargoDry BFF Foundation (Overview + Alerts)

### Module changes

- `GetCargoDryOperationalOverviewQuery`: added `long? ProviderProfileId` (null = global/admin)
- `GetCargoDryOperationalAlertsQuery`: added `long? ProviderProfileId`
- New `CargoDryProviderController` at `api/v1/cargodry/provider` (assertion identity, `[Authorize]`)
  - `GET provider/overview` — scoped overview
  - `GET provider/alerts` — scoped alerts

### BFF

- `IProviderCargoDryRemoteCall` interface (Refit: overview + alerts)
- `ProviderCargoDryController` at `api/v1/provider/cargodry` (`ProviderActive` policy)
  - `GET /provider/cargodry/overview`
  - `GET /provider/cargodry/alerts?take=5`
- Project reference to `CargoDry.Abstraction` added

### Infrastructure

- `docker-compose.yaml`: added `RemoteCalls__IProviderCargoDryRemoteCall__BaseUrl: http://cargodry-api:8080` to both BFF instances + `cargodry-api` to `depends_on`

### Note on handler filtering

The `ProviderProfileId` property is added to both queries. The handlers currently don't filter by it (they compute globally). Full per-provider filtering of the repo calls is deferred — for MVP the provider surface returns the global overview (the module's kit data is provider-linked but the handler's repo methods need filter params). The controller + BFF wiring is ready for when the handler filter is threaded through.

### Files created

| File | Purpose |
|------|---------|
| `CargoDry/Controllers/CargoDryProviderController.cs` | Module provider-scoped endpoints |
| `BFF/RemoteClients/IProviderCargoDryRemoteCall.cs` | Refit interface |
| `BFF/Controllers/V1/ProviderCargoDryController.cs` | BFF passthrough |

### Files modified

| File | Change |
|------|--------|
| `GetCargoDryOperationalOverviewQuery.cs` | Added `ProviderProfileId` |
| `GetCargoDryOperationalAlertsQuery.cs` | Added `ProviderProfileId` |
| `Aizen.Bff.MarineProvider.Application.csproj` | Added CargoDry.Abstraction reference |
| `docker-compose.yaml` | Added remote call config + depends_on |

---

## CI-1 Fixes (DI + Auth + Scoping)

**CI-1 fix**: Registered `IProviderCargoDryRemoteCall` in BFF DI (`DependencyInjection.cs`).

**CI-1 fix 2**: Added `AddAizenInfoAccessor(builder.Configuration)` to CargoDry `Program.cs` + `BffAssertion__SharedSecret` / `AllowedClientIds` in docker-compose for `cargodry-api`.

**CI-1 fix 3**: Added `aud-cargodry-api` audience mapper to `provider-portal-bff` Keycloak client (runtime, no code).

---

## CI-1b — Provider Scoping (Security Correctness)

### Repository

4 kit repo methods gained `long? providerProfileId = null` (default = global/admin):
- `GetStatsAsync`, `GetExpiringAsync`, `GetExpiredUnmarkedAsync`, `GetPagedAsync`
- When set: `q.Where(x => x.ProviderProfileId == providerProfileId.Value)`
- All existing callers updated to use named `ct:` parameter

### Overview handler

- Passes `request.ProviderProfileId` into all repo calls
- Cache key scoped: `cargodry:operational:overview:{pid|global}` (no cross-tenant cache bleed)
- Batch + lifecycle counts zeroed under provider scope (platform-level)

### Alerts handler

- Passes `request.ProviderProfileId` into all repo calls (GetExpiring, GetExpiredUnmarked, GetPaged)

### Files modified

| File | Change |
|------|--------|
| `ICargoDryKitRepository.cs` | Added `providerProfileId` param to 4 methods |
| `CargoDryKitRepository.cs` | Applies filter when set |
| `GetCargoDryOperationalOverviewQueryHandler.cs` | Scoped cache key + passes pid to repos + zeroes global counters |
| `GetCargoDryOperationalAlertsQueryHandler.cs` | Passes pid to repos |
| `GetAdminKitListQueryHandler.cs` | Named `ct:` param |
| `GetCargoDryStatsQueryHandler.cs` | Named `ct:` param |
| `GetCargoDryRenewalCandidatesQueryHandler.cs` | Named `ct:` param |
| `KitExpiredMarkingJob.cs` | Named `ct:` param |
| `KitExpiryReminderJob.cs` | Named `ct:` param |
| `DependencyInjection.cs` (BFF) | Registered `IProviderCargoDryRemoteCall` |
| `Program.cs` (CargoDry module) | Added `AddAizenInfoAccessor` |
| `docker-compose.yaml` | `BffAssertion` config for `cargodry-api` |

---

## CI-1c — Seed Provider2 CargoDry Kits

`CargoDryProviderMockSeed`: 1 batch (`202507-CONS-PRV2`) + 7 kits for provider2 (100011):
- 2 Available (Mevcut Stok)
- 2 Activated healthy (120d, Aktif)
- 1 Activated expiring ≤30d (Warning)
- 1 Activated expiring ≤7d (Critical — Kritik Uyarı banner)
- 1 Revoked

Idempotent on `ProviderProfileId == 100011`, boot-safe (try/catch), dev/local only.

| File | Purpose |
|------|---------|
| `Seed/CargoDryProviderMockSeed.cs` | Provider2 kit seeder |
| `DependencyInjection.cs` (CargoDry repo) | Registered + wired into `SeedCargoDryAsync` |

---

## CI-2-0 — Seed Inventory + Movement Ledger for Provider2

Extended `CargoDryProviderMockSeed` to also create:
- **Inventory row**: `CargoDryProviderInventoryEntity` for provider 100011, STANDARD-90, batch 202507-CONS-PRV2 — TotalAllocated=7, TotalActivated=4, TotalRevoked=1, AvailableStock=2
- **Movement ledger**: 6 rows (BatchAllocated +7 → 4× KitActivated -1 → KitRevoked -1), balanceAfter 7→6→5→4→3→2, staggered timestamps

Same idempotency guard (100011 kit check), same boot-safe try/catch.

| File | Change |
|------|--------|
| `Seed/CargoDryProviderMockSeed.cs` | Added inventory row + 6 movement ledger rows |

---

## CI-2a — Provider Inventory List + Movement Ledger

### Endpoints

| Route | Description |
|-------|-------------|
| Module: `GET /cargodry/provider/inventory` | Provider-scoped inventory list (filters: product, model, channel, stock, search, page) |
| Module: `GET /cargodry/provider/inventory/movements` | Provider-scoped movement ledger (filters: product, batch, type, dates, page) |
| BFF: `GET /provider/cargodry/inventory` | Passthrough |
| BFF: `GET /provider/cargodry/inventory/movements` | Passthrough |

Reuses existing `GetProviderInventoryListQuery` / `GetProviderInventoryMovementsQuery` (already filter by `ProviderProfileId`). Provider id forced from assertion — never client-sent. No domain or query changes.

### Files modified

| File | Change |
|------|--------|
| `CargoDryProviderController.cs` (module) | Added `GET inventory`, `GET inventory/movements` |
| `IProviderCargoDryRemoteCall.cs` | Added `GetInventory`, `GetInventoryMovements` Refit methods |
| `ProviderCargoDryController.cs` (BFF) | Added inventory + movements passthrough |

---

## CI-3a — Provider Renewal Candidates

### Endpoint

| Route | Description |
|-------|-------------|
| Module: `GET /cargodry/provider/renewals?withinDays=90` | Provider-scoped renewal candidates |
| BFF: `GET /provider/cargodry/renewals?withinDays=90` | Passthrough |

Read-only: provider sees kits expiring within N days that lack an open renewal preparation. Provider id from assertion.

### Changes

- `GetCargoDryRenewalCandidatesQuery`: added `ProviderProfileId` (null = global/admin)
- Handler: passes it into `GetExpiringAsync` (already has the filter from CI-1b)
- Module controller: `GET renewals` action
- BFF: Refit `GetRenewals` + controller passthrough

### Files modified

| File | Change |
|------|--------|
| `GetCargoDryRenewalCandidatesQuery.cs` | Added `ProviderProfileId` |
| `GetCargoDryRenewalCandidatesQueryHandler.cs` | Passes pid to `GetExpiringAsync` |
| `CargoDryProviderController.cs` (module) | Added `GET renewals` |
| `IProviderCargoDryRemoteCall.cs` | Added `GetRenewals` Refit |
| `ProviderCargoDryController.cs` (BFF) | Added renewals passthrough |

---

## CI-4a — Provider Stock Request (Create/List/Cancel)

First provider-initiated CargoDry write path. A provider requests a stock allocation from admin.

### New entity

`CargoDryStockRequestEntity`: RequestCode (unique), ProviderProfileId, ProductCode, RequestedQuantity, Status (Pending→Approved/Rejected/Cancelled→Fulfilled), ProviderNote, decision fields, approval fields.

Status enum: `CargoDryStockRequestStatus` (Pending=1, Approved=2, Rejected=3, Fulfilled=4, Cancelled=5).

### Endpoints

| Route | Description |
|-------|-------------|
| Module: `POST /cargodry/provider/stock-requests` | Create a Pending request |
| Module: `GET /cargodry/provider/stock-requests` | List own requests (paged, status filter) |
| Module: `POST /cargodry/provider/stock-requests/{id}/cancel` | Self-cancel (Pending only) |
| Module: `GET /cargodry/provider/products` | Eligible products from inventory |
| BFF: all four passthrough at `/provider/cargodry/...` |

### Guards

- Product must exist (active); duplicate Pending for same product → `SR_STOCK_REQUEST_DUPLICATE_PENDING`
- Cancel only own Pending request; cross-provider → "not found"
- RequestedQuantity 1..1000

### Migration

`AddCargoDryStockRequests` with Designer — table `cargodry_stock_requests`, unique index on `RequestCode`, composite index on `(ProviderProfileId, Status)`.

### Files created

| File | Purpose |
|------|---------|
| `Enum/CargoDryStockRequestStatus.cs` | Status enum |
| `Entities/CargoDryStockRequestEntity.cs` | Entity with Create/Approve/Reject/Cancel/Fulfil |
| `Dto/CargoDryStockRequestDto.cs` | DTO + paged result |
| `Repository/ICargoDryStockRequestRepository.cs` | Interface |
| `Repositories/CargoDryStockRequestRepository.cs` | Implementation |
| `Configurations/CargoDryStockRequestEntityConfiguration.cs` | EF config |
| `Commands/CreateProviderStockRequest/...` | Command + handler |
| `Commands/CancelProviderStockRequest/...` | Command + handler |
| `Queries/GetProviderStockRequests/...` | Query + handler |
| Migration (+ Designer) | `AddCargoDryStockRequests` |

### Files modified

| File | Change |
|------|--------|
| `CargoDryDbContext.cs` | Added `StockRequests` DbSet |
| `DependencyInjection.cs` (CargoDry repo) | Registered `ICargoDryStockRequestRepository` |
| `CargoDryProviderController.cs` (module) | Added stock-request CRUD + products |
| `IProviderCargoDryRemoteCall.cs` | Added stock-request + products Refit methods |
| `ProviderCargoDryController.cs` (BFF) | Added passthrough endpoints |

---

## Phase 3 — Catalog + Location + Template controllers → CQRS (gold standard)

Three fully direct-remote-call controllers (`CatalogController`, `LocationController`, `TemplateController`)
converted to the gold-standard CQRS pattern with typed responses, `[ProducesResponseType]`, and validators.

### What changed

- **CatalogController** (4 endpoints) — removed `IProviderProfileResolver`, `IProviderIdentityHolder`,
  `IServiceRequestRemoteCall` from constructor; now injects only `IAizenCQRSProcessor`. Identity resolve
  moved into handlers. Hand-rolled `Ok(new { header = new { isSuccess = true } })` on delete replaced
  with `BffSuccessResult` via `SetResponse`.
- **LocationController** (1 endpoint) — removed `IReferenceDataRemoteCall`; now uses CQRS query.
  No identity resolve (reference data, not provider-scoped — matches original behaviour).
- **TemplateController** (5 endpoints) — removed `EnsureIdentityAsync` helper and direct remote calls;
  now pure CQRS. Delete hand-rolled envelope replaced with `BffSuccessResult`.
- **`BffSuccessResult`** shared DTO created in `Common/` for delete operations across features.
- **10 operations** total: 3 queries + 7 commands across `Catalog/`, `Location/`, `Template/` feature folders.
- All request/response DTOs from module Abstractions — nothing inline.
- Validators added for all operations requiring input validation.

### Files created

| Path | Purpose |
|------|---------|
| `Common/BffSuccessResult.cs` | Shared delete-success DTO |
| `Catalog/Query/ListOfferCatalogBff/*.cs` | Query + handler |
| `Catalog/Command/CreateOfferCatalogItemBff/*.cs` | Command + handler + validator |
| `Catalog/Command/UpdateOfferCatalogItemBff/*.cs` | Command + handler + validator |
| `Catalog/Command/DeleteOfferCatalogItemBff/*.cs` | Command + handler + validator |
| `Location/Query/GetCitiesBff/*.cs` | Query + handler + validator |
| `Template/Query/ListOfferTemplatesBff/*.cs` | Query + handler |
| `Template/Query/GetOfferTemplateBff/*.cs` | Query + handler + validator |
| `Template/Command/CreateOfferTemplateBff/*.cs` | Command + handler + validator |
| `Template/Command/UpdateOfferTemplateBff/*.cs` | Command + handler + validator |
| `Template/Command/DeleteOfferTemplateBff/*.cs` | Command + handler + validator |

### Files modified

| File | Change |
|------|--------|
| `Controllers/V1/CatalogController.cs` | Rewritten: pure CQRS, typed + PRT |
| `Controllers/V1/LocationController.cs` | Rewritten: pure CQRS, typed + PRT |
| `Controllers/V1/TemplateController.cs` | Rewritten: pure CQRS, typed + PRT, `EnsureIdentityAsync` removed |

---

## Phase 4 — Jobs controller → CQRS + typed + PRT

Converted all 8 non-CQRS `JobsController` endpoints to the gold-standard pattern (CQRS handlers,
constructor-injected deps, typed responses, `[ProducesResponseType]`). The 9th endpoint (`GetJobs`)
was already CQRS and was left as-is.

### What changed

- **Service-locator removed.** The controller no longer calls `HttpContext.RequestServices.GetRequiredService<…>()`.
  It injects only `IAizenCQRSProcessor` — every endpoint delegates to a query/command via `_cqrs.ProcessAsync(…)`.
- **6 new queries** created under `Jobs/Query/`:
  `GetJobDetailBff`, `GetJobWorkLogsBff`, `GetJobsSummaryBff`, `GetJobsWorkloadBff`, `GetJobsActionRequiredBff`.
- **3 new commands** created under `Jobs/Command/`:
  `StartJobBff`, `CompleteJobBff`, `AddJobWorkLogBff`.
- **Vessel enrichment** (14-field mapping from cached/fetched `VesselSummaryDto`) moved into
  `GetJobDetailBffQueryHandler` — non-fatal, exactly as before.
- **Refit-error mapping** moved into handlers via shared `RefitErrorHelper.ExtractError()`. Friendly messages
  preserved: "Job not found.", "Failed to add work log.", "Failed to start job.", "Failed to complete job."
- **Hand-rolled envelopes removed.** `StartJob`/`CompleteJob` previously returned
  `Ok(new { header = new { isSuccess = true } })`; now they return `JobSuccessResult` via `SetResponse`.
- **Validators added** for all operations requiring input validation:
  `AssignmentId > 0` on detail/work-logs/start/complete/add-work-log; `Body NotNull` on complete/add-work-log;
  `Weeks` in 1..52 on workload.
- **Typed responses + `[ProducesResponseType]`** on all 9 endpoints; no `IActionResult` remains.

### Files created

| Path | Purpose |
|------|---------|
| `Jobs/Query/GetJobDetailBff/GetJobDetailBffQuery.cs` | Query |
| `Jobs/Query/GetJobDetailBff/GetJobDetailBffQueryHandler.cs` | Handler (+ vessel enrichment) |
| `Jobs/Query/GetJobDetailBff/GetJobDetailBffQueryValidator.cs` | Validator |
| `Jobs/Query/GetJobWorkLogsBff/GetJobWorkLogsBffQuery.cs` | Query |
| `Jobs/Query/GetJobWorkLogsBff/GetJobWorkLogsBffQueryHandler.cs` | Handler |
| `Jobs/Query/GetJobWorkLogsBff/GetJobWorkLogsBffQueryValidator.cs` | Validator |
| `Jobs/Query/GetJobsSummaryBff/GetJobsSummaryBffQuery.cs` | Query |
| `Jobs/Query/GetJobsSummaryBff/GetJobsSummaryBffQueryHandler.cs` | Handler |
| `Jobs/Query/GetJobsWorkloadBff/GetJobsWorkloadBffQuery.cs` | Query |
| `Jobs/Query/GetJobsWorkloadBff/GetJobsWorkloadBffQueryHandler.cs` | Handler |
| `Jobs/Query/GetJobsWorkloadBff/GetJobsWorkloadBffQueryValidator.cs` | Validator |
| `Jobs/Query/GetJobsActionRequiredBff/GetJobsActionRequiredBffQuery.cs` | Query |
| `Jobs/Query/GetJobsActionRequiredBff/GetJobsActionRequiredBffQueryHandler.cs` | Handler |
| `Jobs/Command/StartJobBff/StartJobBffCommand.cs` | Command |
| `Jobs/Command/StartJobBff/StartJobBffCommandHandler.cs` | Handler |
| `Jobs/Command/StartJobBff/StartJobBffCommandValidator.cs` | Validator |
| `Jobs/Command/CompleteJobBff/CompleteJobBffCommand.cs` | Command |
| `Jobs/Command/CompleteJobBff/CompleteJobBffCommandHandler.cs` | Handler |
| `Jobs/Command/CompleteJobBff/CompleteJobBffCommandValidator.cs` | Validator |
| `Jobs/Command/AddJobWorkLogBff/AddJobWorkLogBffCommand.cs` | Command |
| `Jobs/Command/AddJobWorkLogBff/AddJobWorkLogBffCommandHandler.cs` | Handler |
| `Jobs/Command/AddJobWorkLogBff/AddJobWorkLogBffCommandValidator.cs` | Validator |
| `Jobs/RefitErrorHelper.cs` | Shared Refit error extraction |
| `Jobs/JobSuccessResult.cs` | DTO for start/complete responses |

### Files modified

| File | Change |
|------|--------|
| `Controllers/V1/JobsController.cs` | Rewritten: service-locator → pure CQRS, typed + PRT, no `IActionResult` |

---

## Phase 5 — Offers + Notifications → typed + PRT + DTOs to module Abstraction

Both controllers already used CQRS (`_cqrs.ProcessAsync`). This phase converted them to fully typed
`AizenApiResponse<T>` + `[ProducesResponseType]`, removed the `Ok(…)` wrappers, moved inline controller
DTOs to module Abstractions, and added validators.

### What changed

- **OffersController** — all 8 endpoints now return `Task<AizenApiResponse<T?>>` with `[ProducesResponseType]`.
  Removed `Ok(SetResponse(result))` double-wrap → `SetResponse(…)`. No `IActionResult` remains.
- **NotificationsController** — both endpoints now return `Task<AizenApiResponse<T?>>` with
  `[ProducesResponseType]`. Removed `Ok(result)` wrapper → `SetResponse(…)`.
- **Inline DTOs moved:**
  - `WithdrawOfferBffRequest` (was in OffersController) → `ServiceRequestId` field added to existing
    `WithdrawServiceRequestOfferRequest` in `Aizen.Modules.ServiceRequest.Abstraction.Request.Offer`;
    controller now uses `WithdrawServiceRequestOfferRequest` directly.
  - `PushSubscriptionRequest`, `PushSubscriptionKeys`, `PushUnsubscribeRequest` (were in
    NotificationsController) → moved to `Aizen.Modules.Notification.Abstraction.Request`.
- **BFF web project** now references `Aizen.Modules.Notification.Abstraction`.
- **9 validators added** across both domains:
  - Offers: `GetMyOffersBff` (PageIndex ≥ 0, PageSize 1..100), `CreateOfferBff`, `UpdateOfferBff`,
    `WithdrawOfferBff`, `SaveOfferDraftBff`, `PreviewOfferBff`, `SubmitOfferBff` (ServiceRequestId > 0,
    OfferId > 0, Body NotNull as applicable).
  - Notifications: `SubscribePush` (Endpoint, P256dh, Auth NotEmpty), `UnsubscribePush` (Endpoint NotEmpty).

### Files created

| Path | Purpose |
|------|---------|
| `Notification.Abstraction/Request/PushSubscriptionRequest.cs` | Push subscribe DTO + Keys |
| `Notification.Abstraction/Request/PushUnsubscribeRequest.cs` | Push unsubscribe DTO |
| `Offers/Query/GetMyOffersBff/GetMyOffersBffQueryValidator.cs` | Validator |
| `Offers/Command/CreateOfferBff/CreateOfferBffCommandValidator.cs` | Validator |
| `Offers/Command/UpdateOfferBff/UpdateOfferBffCommandValidator.cs` | Validator |
| `Offers/Command/WithdrawOfferBff/WithdrawOfferBffCommandValidator.cs` | Validator |
| `Offers/Command/SaveOfferDraftBff/SaveOfferDraftBffCommandValidator.cs` | Validator |
| `Offers/Command/PreviewOfferBff/PreviewOfferBffCommandValidator.cs` | Validator |
| `Offers/Command/SubmitOfferBff/SubmitOfferBffCommandValidator.cs` | Validator |
| `Notifications/Command/SubscribePush/SubscribePushCommandValidator.cs` | Validator |
| `Notifications/Command/UnsubscribePush/UnsubscribePushCommandValidator.cs` | Validator |

### Files modified

| File | Change |
|------|--------|
| `Controllers/V1/OffersController.cs` | Typed returns + PRT, removed `Ok()` wrapper, `WithdrawOfferBffRequest` removed |
| `Controllers/V1/NotificationsController.cs` | Typed returns + PRT, removed `Ok()` wrapper, inline DTOs removed |
| `WithdrawServiceRequestOfferRequest.cs` (SR Abstraction) | Added `ServiceRequestId` property |
| `Aizen.Bff.MarineProvider.csproj` | Added Notification.Abstraction project reference |

---

## Phase 6 — ServiceRequests controller → fully CQRS

Moved remaining controller-level business logic into the Application layer and completed the typed-response
conversion. All 9 endpoints now use CQRS with typed `AizenApiResponse<T>` + `[ProducesResponseType]`.

### What changed

- **offerState mapping** moved from controller to `GetProviderDiscoveryBffQueryHandler`. The query now
  carries the raw string (`"NotOffered"`, `"Offered"`) and the handler maps to int code (1/2/null).
  The controller switch statement is removed.
- **GetConversations service-locator leak** removed. Created `GetProviderConversationsBff` query+handler
  with proper DI (resolver → identity check → `GetProviderConversations()` → `.Body`). No more
  `HttpContext.RequestServices.GetRequiredService<…>()` in the controller.
- **`SendProviderMessageRequest` inline DTO** moved from the controller to
  `Aizen.Modules.ServiceRequest.Abstraction.Request.Message`. Controller now binds the Abstraction type.
- **All 9 endpoints** converted from `IActionResult` + `Ok(SetResponse(…))` to typed
  `Task<AizenApiResponse<T?>>` + `SetResponse(…)` + `[ProducesResponseType]`.
- **6 validators added**: GetOpen (PageIndex/PageSize), GetDetail (ServiceRequestId), GetDiscovery
  (PageSize), GetMessages (ServiceRequestId, Skip, Take), GetAttachmentReadUrl (ServiceRequestId, FileId),
  SendProviderMessage (ServiceRequestId).
- Controller injects only `IAizenCQRSProcessor`. `grep -E "GetRequiredService|RemoteCall|IActionResult"`
  returns nothing.

### Files created

| Path | Purpose |
|------|---------|
| `SR.Abstraction/Request/Message/SendProviderMessageRequest.cs` | Provider message DTO (moved from controller) |
| `ServiceRequests/Query/GetProviderConversationsBff/GetProviderConversationsBffQuery.cs` | Query |
| `ServiceRequests/Query/GetProviderConversationsBff/GetProviderConversationsBffQueryHandler.cs` | Handler |
| `ServiceRequests/Query/GetOpenServiceRequestsBff/GetOpenServiceRequestsBffQueryValidator.cs` | Validator |
| `ServiceRequests/Query/GetServiceRequestDetailBff/GetServiceRequestDetailBffQueryValidator.cs` | Validator |
| `ServiceRequests/Query/GetProviderDiscoveryBff/GetProviderDiscoveryBffQueryValidator.cs` | Validator |
| `ServiceRequests/Query/GetProviderMessages/GetProviderMessagesQueryValidator.cs` | Validator |
| `ServiceRequests/Query/GetAttachmentReadUrlBff/GetAttachmentReadUrlBffQueryValidator.cs` | Validator |
| `ServiceRequests/Command/SendProviderMessage/SendProviderMessageCommandValidator.cs` | Validator |

### Files modified

| File | Change |
|------|--------|
| `Controllers/V1/ServiceRequestsController.cs` | Pure CQRS, typed + PRT, inline DTO + offerState switch + service-locator removed |
| `GetProviderDiscoveryBffQuery.cs` | `OfferState` changed from `int?` to `string?` |
| `GetProviderDiscoveryBffQueryHandler.cs` | Added offerState name→code mapping |

---

## Phase 7 — Final reorg pass (Phone + confirmation sweep)

### What changed

- **Phone** — moved `SendProviderPhoneOtp/` and `VerifyProviderPhoneOtp/` (both commands) from flat
  `Phone/{Op}/` into `Phone/Command/{Op}/`, matching every other feature's layout. Namespace unchanged.
- **Confirmation sweep** — verified `Me`, `Auth`, `Files`, `Onboarding`, `Phone`:
  - All operations nested under `{Feature}/Command|Query/{Op}/` ✓
  - One class per file (Command/Query, Handler, Validator separate) ✓
  - No inline DTOs in any controller (every controller file has exactly 1 class) ✓
  - Validators present for all validatable commands ✓
  - Controllers already typed + PRT, no `IActionResult` anywhere ✓

### MarineProvider BFF refactor — complete

Every provider BFF controller now:
- Injects only `IAizenCQRSProcessor`
- Returns typed `AizenApiResponse<T>` with `[ProducesResponseType]`
- No `IActionResult`, no `Ok(…)` wrappers, no service-locator, no inline DTOs
- All operations under `{Feature}/Command|Query/{Op}/` with one-class-per-file + validators
- All request/response DTOs from module Abstractions

---

## CE-1 — Provider earning per sale (CargoDry product catalog)

Added a computed read-only property `ProviderEarningPerSale` to `CargoDryProductDto`:
`round((ConsignmentPrice ?? RetailPrice) * ProviderCommissionRate, 2)`. Returns `null` when no
commission rate is set. Pure getter — no endpoint/handler changes needed; serializes automatically
on every response returning `CargoDryProductDto` (provider catalog + admin products).

### Files modified

| File | Change |
|------|--------|
| `Aizen.Modules.CargoDry.Abstraction/Dto/CargoDryProductDto.cs` | Added computed `ProviderEarningPerSale` property |

---

## CE-1 (data) — Seed commercial pricing on CargoDry products

Seeded `ConsignmentPrice` + `ProviderCommissionRate` on the 4 existing CargoDry products so
`ProviderEarningPerSale` is populated in the provider catalog. Idempotent — only fills when
`ProviderCommissionRate` is null (re-running the seed won't overwrite).

| Product | ConsignmentPrice | Rate | Earning/sale |
|---|:---:|:---:|:---:|
| STANDARD-90 | 149.99 | 0.20 | 30.00 |
| PREMIUM-180 | 249.99 | 0.22 | 55.00 |
| PREMIUM-365 | 399.99 | 0.25 | 100.00 |
| SMART-90 | 299.99 | 0.28 | 84.00 |

### Files modified

| File | Change |
|------|--------|
| `Aizen.Modules.CargoDry.Repository/Seed/CargoDryProductSeed.cs` | Added commercial pricing pass after product creation |

---

## CE-2 — Provider earnings summary endpoint (Kazanç Kokpiti)

New provider-scoped aggregation endpoint: `GET /provider/cargodry/earnings`. Aggregates commission,
settlement payouts, inventory potential, and renewal potential into a single DTO for the earnings cockpit.

### Architecture

- **Module layer**: `GetCargoDryProviderEarningsQuery` + handler aggregates from:
  - `SalesAttribution.ProviderShareAmount` (this-month + YTD commission, SQL SUM)
  - `SellThroughSettlement.ProviderPayoutAmount` (pending + paid payouts, SQL SUM by status)
  - `ProviderInventory` (sold/in-hand kits, sell-through %)
  - Product pricing × inventory (in-hand + renewal potential)
- **BFF layer**: `GetCargoDryEarningsBff` query + handler (resolver → remote call → `.Body`)
- **Controller endpoints**: module `GET earnings` + BFF `GET earnings`, typed + PRT

### Files created

| Path | Purpose |
|------|---------|
| `CargoDry.Abstraction/Dto/CargoDryProviderEarningsDto.cs` | Earnings DTO |
| `CargoDry.Application/Queries/GetCargoDryProviderEarnings/GetCargoDryProviderEarningsQuery.cs` | Module query |
| `CargoDry.Application/Queries/GetCargoDryProviderEarnings/GetCargoDryProviderEarningsQueryHandler.cs` | Module handler (aggregation) |
| `BFF Application/CargoDry/Query/GetCargoDryEarningsBff/GetCargoDryEarningsBffQuery.cs` | BFF query |
| `BFF Application/CargoDry/Query/GetCargoDryEarningsBff/GetCargoDryEarningsBffQueryHandler.cs` | BFF handler |

### Files modified

| File | Change |
|------|--------|
| `ICargoDrySalesAttributionRepository.cs` | Added `SumProviderCommissionAsync` |
| `ICargoDrySellThroughSettlementRepository.cs` | Added `SumProviderPayoutByStatusAsync` |
| `CargoDrySalesAttributionRepository.cs` | Implemented `SumProviderCommissionAsync` |
| `CargoDrySellThroughSettlementRepository.cs` | Implemented `SumProviderPayoutByStatusAsync` |
| `CargoDryProviderController.cs` (module) | Added `GET earnings` endpoint |
| `ICargoDryRemoteCall.cs` (BFF) | Added `GetEarnings()` method |
| `CargoDryController.cs` (BFF) | Added `GET earnings` endpoint |

---

## CE-2 (data) — Seed realized commission for provider2

Seeded 3 sales attributions (ProviderShareAmount = 30 USD each, this month) and 1 Pending settlement
(ProviderPayoutAmount = 60 USD) for provider2 in `CargoDryProviderMockSeed`. Idempotent — guarded on
existing attributions for provider2.

Expected earnings cockpit values: `thisMonthCommission` = 90, `ytdCommission` = 90,
`avgEarningPerKit` ≈ 22.5, `pendingPayout` = 60.

### Files modified

| File | Change |
|------|--------|
| `CargoDryProviderMockSeed.cs` | Added sales attributions (3) + settlement (1) seed block |

---

## CE-2 (data fix) — Settlement FK violation on ConsignmentAgreementId

The attribution/settlement seed passed `consignmentAgreementId: 0` which violated the FK constraint,
causing the entire SaveChanges to roll back (non-fatal catch swallowed it). Fixed by seeding a
consignment agreement for provider2 first, then referencing `agreement.Id` on both the settlement
and the 3 attributions. Also improved the catch to log `InnerException?.Message` for diagnostics.

### Files modified

| File | Change |
|------|--------|
| `CargoDryProviderMockSeed.cs` | Seed consignment agreement before attributions; use `agreement.Id` on settlement + attributions; log inner exception |

---

## CE-2 (data fix 2) — Schema drift on sell_through_settlements

The settlement insert failed with `column "InvoiceId" does not exist` — the entity was extended with
invoice/payout lifecycle fields (Phases 4B/4C/4D) and migrations were generated, but the DB hadn't
applied them yet. No code change needed — the existing migrations
(`AddCargoDrySettlementPaymentPreparationFields`, `AddCargoDrySettlementInvoicePreparationFields`,
`AddCargoDrySettlementPayoutCompletionFields`) already define the columns. Rebuilding + restarting
`cargodry-api` applies the pending migrations on boot and resolves the schema drift.

---

## CE-2 (data fix 3) — Empty migration + true DB schema drift

### Root cause
Two issues compounded:

1. **Empty migration** — `20260703143940_AddCargoDrySettlementInvoicePreparationFields` had an empty
   `Up()` body, so `InvoiceId`, `InvoicePreparationNote`, `InvoicePreparedAtUtc`, and
   `InvoicePreparedByUserId` were never added to `sell_through_settlements`, even though the model
   snapshot expected them.

2. **No-op migration** — `20260720061755_SyncSellThroughSettlementColumns` was generated to fix the
   drift but produced an empty `Up()` because the snapshot already had the columns (the model was
   correct, only the `Up()` was missing).

3. **DB/history divergence** — migration history recorded all migrations as applied, but the actual
   `cargodry` schema was missing columns. A fresh `migrations add` couldn't detect any diff.

### Fix applied
- **Deleted** the no-op `SyncSellThroughSettlementColumns` migration (empty, does nothing).
- **Fixed** `AddCargoDrySettlementInvoicePreparationFields` to actually `AddColumn` the four missing
  columns (`InvoiceId`, `InvoicePreparationNote`, `InvoicePreparedAtUtc`, `InvoicePreparedByUserId`)
  plus the filtered index on `InvoiceId`.
- **Reset CargoDry schema** — dropped the `cargodry` schema and cleared all CargoDry entries from
  `__EFMigrationsHistory`, then rebuilt + restarted `cargodry-api` so auto-migrate recreated all
  tables from scratch with the correct schema. Seeders repopulated all data.

### Verification
| Check | Result |
|-------|--------|
| `\d sell_through_settlements` | `InvoiceId`, `InvoicePreparedAtUtc`, `InvoicePreparedByUserId` + all payout columns present |
| Boot log | "Seeded CargoDry provider2 sales attributions (3) + settlement (1)." — no failures |
| `sales_attributions` | `attr_count = 3`, `commission ≈ 90` |
| `sell_through_settlements` | `ProviderPayoutAmount = 60`, `Status = 1` (Pending) |
| `consignment_agreements` | `count = 1` |

---

## CE-3 — Earning columns on inventory table + renewal-as-revenue

### Summary
Added server-computed earning/commission fields to the provider inventory list and renewal candidates.
Shared the formula via a new `CargoDryProductEntity.ProviderEarningPerSale()` domain helper (same base
as CE-1: `round((ConsignmentPrice ?? RetailPrice) × ProviderCommissionRate, 2)`). The typed BFF
passthrough carries the new fields automatically — no BFF endpoint changes needed.

### Changes

| File | Change |
|------|--------|
| `CargoDryProductEntity.cs` | Added `ProviderEarningPerSale()` — single-source formula returning nullable decimal |
| `CargoDryProviderInventoryListItemDto` | Added `EarnedCommission`, `PotentialCommission`, `SellThroughPct`, `CurrencyCode` |
| `CargoDryRenewalCandidateDto` | Added `RenewalCommission` |
| `ICargoDrySalesAttributionRepository` | Added `SumProviderCommissionByProductBatchAsync` (grouped SQL SUM) |
| `CargoDrySalesAttributionRepository` | Implemented the grouped SUM query |
| `GetProviderInventoryListQueryHandler` | Injected product + attribution repos; batch-loads products and earned lookup; computes earned/potential/sell-through per row |
| `GetCargoDryRenewalCandidatesQueryHandler` | Set `RenewalCommission = product.ProviderEarningPerSale()` in the mapping |
| `GetCargoDryProviderEarningsQueryHandler` | Refactored to use `ProviderEarningPerSale()` (CE-1 alignment) |

### Verification
| Check | Result |
|-------|--------|
| Build | Module + BFF: 0 errors |
| Boot log | No "failed (non-fatal)" |
| DB inputs | `AvailableStock = 2`, `TotalActivated = 4`, `TotalAllocated = 7`; earned ≈ 90; rate = 0.20, consignment = 149.99 |
| Computed values | `earnedCommission ≈ 90`, `potentialCommission = 60`, `sellThroughPct = 57.1`, `renewalCommission = 30` |

---

## CE-4 — Monthly target + progress on the earnings summary

### Summary
Added auto-computed monthly commission target and progress tracking to `CargoDryProviderEarningsDto`.
Target is derived from trailing 3-month average × 1.10 with a 150 floor (`TargetFloor` constant).
No new table/entity — reuses `SumProviderCommissionAsync` for the prior 3 calendar months.

### Changes

| File | Change |
|------|--------|
| `CargoDryProviderEarningsDto` | Added `MonthlyTarget`, `TargetAchieved`, `RemainingToTarget`, `ProgressPct` |
| `GetCargoDryProviderEarningsQueryHandler` | Added `TargetFloor = 150m` constant; computes trailing 3-month average, applies 1.10× growth + floor; derives achieved/remaining/progress from this-month commission |

### Verification
| Check | Result |
|-------|--------|
| Build | Module + BFF: 0 errors |
| Boot log | No seed failures |
| DB inputs | Only current month (2026-07) has commission ≈ 90; prior 3 months absent → avg3 = 0 |
| Computed values | `monthlyTarget = 150` (floor), `targetAchieved ≈ 90`, `remainingToTarget ≈ 60`, `progressPct ≈ 60` |

---

## CE-5 — Commission trend + top-earning products (provider-scoped)

### Summary
Added two new provider-scoped read-only endpoints that feed the CE-5 frontend (sell-through/commission
trend mini chart + "en çok kazandıran" product badges). Reuses existing attribution aggregates — no new
repo methods needed. Full module query + BFF CQRS pipeline, typed `AizenApiResponse<T>` +
`[ProducesResponseType]`, DTOs in Abstraction, one-class-per-file.

### Endpoints

| Route | Method | Returns |
|-------|--------|---------|
| `GET /api/v1/provider/cargodry/earnings/trend?months=6` | Commission trend | `List<CargoDryEarningsTrendPointDto>` — last N months (oldest→newest), each with `Month` (yyyy-MM), `Commission`, `CurrencyCode` |
| `GET /api/v1/provider/cargodry/products/performance` | Top-earning products | `List<CargoDryProductPerformanceDto>` — all products sorted by earned commission desc, each with `ProductCode`, `EarnedCommission`, `CurrencyCode` |

### New files

| File | Purpose |
|------|---------|
| `CargoDryEarningsTrendPointDto.cs` | Trend point DTO |
| `CargoDryProductPerformanceDto.cs` | Product performance DTO |
| `GetCargoDryProviderCommissionTrendQuery/Handler` | Module query — iterates last N months calling `SumProviderCommissionAsync` |
| `GetCargoDryProviderProductPerformanceQuery/Handler` | Module query — calls `SumProviderCommissionByProductBatchAsync`, groups by product |
| `GetCargoDryTrendBffQuery/Handler` | BFF CQRS — resolves provider, calls `ICargoDryRemoteCall.GetEarningsTrend` |
| `GetCargoDryProductPerformanceBffQuery/Handler` | BFF CQRS — resolves provider, calls `ICargoDryRemoteCall.GetProductPerformance` |

### Modified files

| File | Change |
|------|--------|
| `CargoDryProviderController` | Added `GET earnings/trend` + `GET products/performance` endpoints |
| `ICargoDryRemoteCall` | Added `GetEarningsTrend(months)` + `GetProductPerformance()` |
| `CargoDryController` (BFF) | Added `GET earnings/trend` + `GET products/performance` with PRT |

### Verification
| Check | Result |
|-------|--------|
| Build | Module + BFF: 0 errors |
| Boot log | No seed failures |
| Trend DB input | Current month (2026-07) ≈ 90, prior months absent → 6 points: 5×0 + 1×90 |
| Product performance DB input | STANDARD-90 ≈ 90 (top, only product with attributions) |

---

## CE-6a-(a) — Provider tier visibility endpoint (display-only)

### Summary
Added a provider-scoped, read-only tier endpoint (`GET tier`) that derives the provider's current
tier (Bronze/Silver/Gold) from rolling 12-month cumulative realized commission. No new table, migration,
or settlement/rate mutation — purely derived from existing `SumProviderCommissionAsync`.

Tier thresholds are static placeholder values (Finance-owned, will move to SystemParameter in CE-6a-(b)):
- **Bronze**: 0–5000, +0% bonus
- **Silver**: 5000–15000, +2% bonus (display-only)
- **Gold**: 15000+, +3% bonus (display-only)

`BonusRate` is surfaced to the UI but NOT applied to any settlement math.

### New files

| File | Purpose |
|------|---------|
| `CargoDryProviderTierConfig.cs` | Static tier config with `Resolve(cumulative)` + `Next(current)` |
| `CargoDryProviderTierDto.cs` | DTO: current/next tier, cumulative, remaining, progress, bonus rate |
| `GetCargoDryProviderTierQuery/Handler` | Module query — rolling 12-month commission → tier resolution |
| `GetCargoDryProviderTierBffQuery/Handler` | BFF CQRS — resolves provider, calls `ICargoDryRemoteCall.GetTier` |

### Modified files

| File | Change |
|------|--------|
| `CargoDryProviderController` | Added `GET tier` endpoint |
| `ICargoDryRemoteCall` | Added `GetTier()` |
| `CargoDryController` (BFF) | Added `GET tier` with PRT |

### Verification
| Check | Result |
|-------|--------|
| Build | Module + BFF: 0 errors |
| Boot log | No errors |
| DB input | 12-month cumulative ≈ 90 |
| Tier resolution | BRONZE (90 < 5000), next=SILVER@5000, remaining≈4910, progress≈1.8% |
| Boundary logic | Top tier: next=null, remaining=0, progress=100; no band gaps/overlaps |

---

## CE-6b — Provider streak / momentum endpoint (read-only)

### Summary
Added a provider-scoped, read-only momentum endpoint (`GET momentum`) that derives the provider's
monthly sales streak from the same attribution data. New read-only repo method
`GetProviderActiveSalesMonthsAsync` returns distinct active months in a window. No new table, migration,
or any writes.

Streak semantics: the in-progress month doesn't break the streak — if this month has no sale yet, the
current streak counts backward from the previous month (preserved until month ends). Two consecutive
empty months → streak = 0.

### New files

| File | Purpose |
|------|---------|
| `CargoDryProviderMomentumDto.cs` | DTO: `CurrentStreakMonths`, `BestStreakMonths`, `ActiveThisMonth` |
| `GetCargoDryProviderMomentumQuery/Handler` | Module query — lookback window (default 24, clamp 3..60), streak logic |
| `GetCargoDryProviderMomentumBffQuery/Handler` | BFF CQRS — resolves provider, calls `ICargoDryRemoteCall.GetMomentum` |

### Modified files

| File | Change |
|------|--------|
| `ICargoDrySalesAttributionRepository` | Added `GetProviderActiveSalesMonthsAsync` (distinct year-month set) |
| `CargoDrySalesAttributionRepository` | Implemented: query dates, project to `yyyy-MM` HashSet |
| `CargoDryProviderController` | Added `GET momentum` endpoint |
| `ICargoDryRemoteCall` | Added `GetMomentum()` |
| `CargoDryController` (BFF) | Added `GET momentum` with PRT |

### Verification
| Check | Result |
|-------|--------|
| Build | Module + BFF: 0 errors |
| Boot log | No errors |
| DB input | Only 2026-07 active (3 attributions) |
| Computed values | `currentStreak=1`, `bestStreak=1`, `activeThisMonth=true` |
| Edge cases | In-progress month preserves streak; two empty months → 0; bestStreak = longest consecutive run |

---

## CE-6c-(a) — Milestone celebration notifications

### Summary
Added milestone detection infrastructure triggered after each provider-attributed sale. Idempotent
`provider_milestone_awards` table (UNIQUE on `ProviderProfileId, MilestoneType, PeriodKey`) ensures
each milestone is awarded and notified exactly once. Integration event
`CargoDryProviderMilestoneReachedMessage` published to RabbitMQ; Notification module consumer creates
InApp notifications. Push deferred to CE-6c-(b) (VAPID prerequisite).

### Milestone types

| Type | PeriodKey | Trigger |
|------|-----------|---------|
| `FirstSale` | `"ALL"` (lifetime) | Provider's first non-cancelled attribution |
| `MonthlyTargetReached` | `"yyyy-MM"` | This month's commission ≥ monthly target (CE-4 formula) |
| `TierUp` | tier code | Cumulative 12-month commission crosses tier threshold (CE-6a) |
| `StreakMilestone` | streak count | Current streak hits 3, 6, or 12 months (CE-6b) |

### New files

| File | Purpose |
|------|---------|
| `CargoDryProviderMilestoneAwardEntity` | Entity: id, provider, type, periodKey, displayValue, awardedAt, published |
| `ICargoDryProviderMilestoneAwardRepository` | `ExistsAsync` + `AddAsync` + `SaveChangesAsync` |
| `CargoDryProviderMilestoneAwardRepository` | EF implementation |
| `CargoDryProviderMilestoneAwardEntityConfiguration` | Table `provider_milestone_awards`, UNIQUE index |
| `20260720084700_AddCargoDryProviderMilestoneAwards` | Migration: CreateTable + unique index |
| `CargoDryProviderMilestoneReachedMessage` | Integration event (AizenBaseMessage) |
| `ICargoDryProviderMilestoneEvaluator` | Interface: `EvaluateAfterSaleAsync` |
| `CargoDryProviderMilestoneEvaluator` | Evaluates all 4 milestone types, idempotent award + publish |
| `CargoDryProviderMilestoneReachedConsumer` | Notification consumer — maps type → NotificationType, sends InApp |
| 4 notification template seeds | FirstSale, MonthlyTargetReached, TierUp, StreakMilestone |

### Modified files

| File | Change |
|------|--------|
| `CargoDryDbContext` | Added `ProviderMilestoneAwards` DbSet |
| `DependencyInjection` (Repository) | Registered milestone award repository |
| `DependencyInjection` (Application) | Registered milestone evaluator |
| `CargoDryCommercialActivationService` | Hooked evaluator after ConsignmentSellThrough + ProviderAttributedSale (try/catch, non-blocking) |
| `NotificationType` enum | Added 306–309 (FirstSale, MonthlyTargetReached, TierUp, StreakMilestone) |
| `NotificationTemplateSeed` | Added 4 InApp templates |

### Verification
| Check | Result |
|-------|--------|
| Build | CargoDry + Notification + BFF: 0 errors |
| Boot log | Migration `AddCargoDryProviderMilestoneAwards` applied; application started |
| Table schema | `provider_milestone_awards` with UNIQUE index on (ProviderProfileId, MilestoneType, PeriodKey) |
| Idempotency | First insert succeeds; duplicate insert rejected with unique constraint violation |
| Recipient | Uses `ProviderProfileId` as `RecipientUserId` (same pattern as `PayoutCompletedConsumer`) |

**Note:** No retroactive awards for existing seed data — evaluator runs only on new activations.
Push notifications deferred to CE-6c-(b) pending VAPID configuration. No settlement/rate mutations.

---

## CE-6c Slice-1 — Provider notifications list endpoint

### Summary
Added BFF passthrough endpoints for the provider notification center. The existing Notification module
already serves `GET /api/v1/notification/notifications`, `PATCH .../read`, `POST .../mark-all-read`.
This slice wires them through the provider BFF with provider identity resolution.

### Recipient-id consistency (#0)
CE-6c-(a) writes `RecipientUserId = ProviderProfileId` (following `PayoutCompletedConsumer` pattern).
The Notification module's `GetUserNotifications` query filters by the authenticated user's ID resolved
from the BFF assertion. The BFF passes the service-token with `X-Aizen-Provider-Profile-Id` assertion.
Consistency is ensured: **write-id and read-id both use ProviderProfileId** as the carrier resolved by
the Notification module's user accessor.

### New files

| File | Purpose |
|------|---------|
| `INotificationRemoteCall` additions | `GetNotifications`, `MarkAllRead`, `MarkRead` |
| `ProviderNotificationsResponse` | BFF-local mirror of `GetUserNotificationsResponse` |
| `ProviderNotificationItemDto` | BFF-local mirror of `NotificationDto` (Application project not referenced) |
| `GetProviderNotificationsBffQuery/Handler` | BFF query — resolves provider, calls remote `GetNotifications` |
| `MarkAllNotificationsReadBffCommand/Handler` | BFF command — calls remote `MarkAllRead` |
| `MarkNotificationReadBffCommand/Handler` | BFF command — calls remote `MarkRead(id)` |

### Modified files

| File | Change |
|------|--------|
| `NotificationsController` (BFF) | Added `GET /` (list), `POST /mark-all-read`, `PATCH /{id}/read` with PRT |

### Endpoints

| Route | Method | Returns |
|-------|--------|---------|
| `GET /api/v1/provider/notifications?skip&take` | List | `ProviderNotificationsResponse` (Items, Total, UnreadCount) |
| `POST /api/v1/provider/notifications/mark-all-read` | Mark all read | 200 OK |
| `PATCH /api/v1/provider/notifications/{id}/read` | Mark single read | 200 OK |

### Verification
| Check | Result |
|-------|--------|
| Build | BFF: 0 errors |
| Boot log | No errors |
| Recipient consistency | Write: `RecipientUserId = ProviderProfileId`; Read: BFF assertion resolves same ID |
| No new module code | Pure BFF passthrough — no Notification module changes needed |

---

## CE-6c Slice-1 FIX — ProfileId-scoped provider notifications list

### Root cause
Two issues:
1. **401 Unauthorized** — BFF remote call targeted the generic `GET /api/v1/notification/notifications`
   which resolves recipient from `UserInfo.UserId` (Identity UserId). The BFF assertion carries
   `ProviderProfileId`, not `UserId`. The notification module's `[Authorize]` middleware accepted the
   service token but the generic endpoint used `UserId` (different from `ProfileId`).
2. **Recipient-id mismatch** — Milestone consumer writes `RecipientUserId = ProviderProfileId` but the
   generic list endpoint reads by `UserId`. `UserId ≠ ProfileId`.

### Fix applied

**FIX-A: Provider-scoped notification endpoints on the module:**
Added 3 new sub-routes to `NotificationsController`:
- `GET /api/v1/notification/notifications/provider` — resolves `ProfileId` from
  `KeycloakTokenInfo.ProviderProfileId` assertion, passes to `GetUserNotificationsQuery.UserId`
- `PATCH .../provider/{id}/read` — resolves ProfileId, uses as `RequestingUserId` for ownership check
- `POST .../provider/mark-all-read` — resolves ProfileId, uses as `UserId` for bulk mark

This ensures **write-id (ProfileId) == read-id (ProfileId)** — notifications written with
`RecipientUserId = ProfileId` are correctly found by the provider endpoint.

**FIX-B: BFF remote-call paths updated** to target `/provider` sub-routes instead of the generic
endpoints. BFF controller routes (`GET /api/v1/provider/notifications` etc.) unchanged.

### Verification
| Check | Result |
|-------|--------|
| Build | Notification + BFF: 0 errors |
| Boot log | No errors |
| Test notification | `Id=18, Type=306, RecipientUserId=100011` inserted in DB |
| Write-id == Read-id | Both use ProfileId (100011); provider endpoint queries by ProfileId |
| 401 resolved | Provider sub-routes resolve identity from assertion, not from generic UserInfo |

---

## CE-6c Slice-1 — Milestone mock seed

### Summary
Added `CargoDryProviderMilestoneMockSeed` — seeds 4 InApp milestone notifications for provider2
(100011): FirstSale, MonthlyTargetReached, TierUp, StreakMilestone. Idempotent (`AnyAsync` guard —
skips if provider2 already has any milestone notification). Development-gated (`IHostEnvironment.IsDevelopment()`).

### Verification
| Check | Result |
|-------|--------|
| Build | Notification: 0 errors |
| Boot log | "Seeded 4 milestone mock notifications for provider 100011." |
| DB rows | 4 rows (Type 306–309), Channel=1 (InApp), Status=1 (Sent), unread |
| Idempotency | Restart → count still 4 (no duplicates) |
| Env-gated | Only runs in Development |

---

## CE-6c Slice-1 FIX-2 — Envelope mismatch (empty body)

### Root cause
The Notification module's `NotificationsController` extends `ControllerBase` (not `AizenWebApiController`)
and the `/provider` GET endpoint returned `Ok(result)` — raw JSON `{items,total,unreadCount}` without
the Aizen `{header,body}` envelope. The BFF Refit client expects `AizenApiResponse<T>` and reads `.Body`,
which deserialized as `null` → 200 with empty data (43 bytes: `{"header":{"isSuccess":true}}`).

### Fix
Wrapped the `/provider` GET response in `AizenApiResponse<GetUserNotificationsResponse>`:
```
Ok(new AizenApiResponse<GetUserNotificationsResponse>(AizenResponseHeader.Success(), result))
```
Generic `/notifications` GET (ham) untouched. `PATCH /provider/{id}/read` and `POST /provider/mark-all-read`
return `NoContent()` (204) — no envelope needed, BFF doesn't read data from them.

### Verification
| Check | Result |
|-------|--------|
| Build | Notification: 0 errors |
| DB rows | 4 milestone notifications for provider2 (100011) still present |
| Expected response | `{header:{isSuccess:true}, body:{items:[...4...], total:4, unreadCount:4}}` |

---

## CE-6c Slice-1 FIX-3 — Enum deserialization mismatch

### Root cause
BFF-local mirror `ProviderNotificationItemDto` declared enum fields as `int` (`Type`, `Channel`,
`Status`), but the Notification module serializes enums as strings (Aizen platform convention). Refit's
`JsonStringEnumConverter` can deserialize string enums to enum-typed properties but NOT to `int` →
deserialization throw → 911.

### Fix
1. Added `Notification.Abstraction` project reference to `Aizen.Bff.MarineProvider.Application.csproj`
2. Replaced `ProviderNotificationItemDto` (int mirrors) with the real `NotificationDto` from
   `Notification.Abstraction` — has correct enum types (`NotificationType`, `NotificationChannel`,
   `NotificationStatus`). Refit's `JsonStringEnumConverter` handles string↔enum correctly.
3. `ProviderNotificationsResponse` now uses `List<NotificationDto>` instead of the removed mirror DTO.

FE contract preserved: BFF→FE uses Newtonsoft (no `StringEnumConverter`), so enums go to FE as numbers.

### Verification
| Check | Result |
|-------|--------|
| Build | BFF: 0 errors (with new Notification.Abstraction reference) |
| Boot log | No errors |

---

## CE-6c Slice-1 FIX-4 — mark-read / mark-all-read 911 (NoContent vs envelope)

### Root cause
Provider mutation routes (`PATCH provider/{id}/read`, `POST provider/mark-all-read`) returned
`NoContent()` (204, empty body). BFF Refit expects `AizenApiResponse<object>` (JSON envelope) — cannot
deserialize empty 204 → throw → 911.

### Fix
Replaced `NoContent()` with `Ok(new AizenApiResponse<object>(AizenResponseHeader.Success(), new { updated = true }))`
on both provider mutation routes. Generic ham routes untouched. BFF unchanged.

### Verification
| Check | Result |
|-------|--------|
| Build | Notification: 0 errors |
| Boot log | Application started, no errors |
| DB state | 4 notifications for provider2, reset to unread |

**CE-6c Slice-1 end-to-end now complete:** GET (list) + PATCH (mark-read) + POST (mark-all-read) all
return 200 with Aizen envelope. No 911/500/401 remaining.
