# 02 — Current Backend Audit (ServiceRequest)

Read from source. Paths real.

## Offer aggregate — `ServiceRequestOfferEntity`

`Domain/Entities/Offer/ServiceRequestOfferEntity.cs` (`AizenEntityWithAudit`):

`ServiceRequestId`, `ProviderProfileId`, `ProviderUserId`, `Status` (`ServiceRequestOfferStatus`), **`TotalAmount`
(decimal)**, `CurrencyCode` (default `"USD"`), `Description`, `ProviderNotes`, `EstimatedStartDate`,
`EstimatedEndDate`, `EstimatedDurationMinutes`, `ExpiresAt`, `AcceptedAt`, `RejectedAt`, `RejectionReason`,
`WithdrawnAt`, `WithdrawalReason`, `Items` (collection).

Methods: `Create(...)`, `Submit()` (→ Submitted), `Accept()`, `Reject(reason)`, `Withdraw(reason)`,
`Update(...)`, `AddItem(...)`, **`RecalculateTotal() => Items.Sum(i => i.Quantity * i.UnitPrice)`**.

**Missing for the mock:** no `TaxRate`/`TaxAmount` (any level), no `Version`, no `ViewedAt`, no
`RevisionRequestedAt`, no deposit / payment-terms / warranty, no `SubtotalAmount`/`TaxTotal`/per-category totals.
`RecalculateTotal` is quantity×price only — no tax, no discount arithmetic.

## Offer line — `ServiceRequestOfferItemEntity`

`ItemType` (`ServiceRequestOfferItemType`), `Title`, `Description`, **`Quantity` (int)**, `UnitPrice` (decimal),
`CurrencyCode`, `SortOrder`, `IsDiscount` (bool).

**Missing:** `UnitCode` (offer items carry no unit — the mock shows "Adet", "10 Litre kova"), no `TaxRate`,
`TaxAmount`, `LineSubtotal`, `LineTotal`, `DiscountType`/`DiscountValue`/`DiscountAmount`, no catalog source id,
no snapshot of name/description separate from live values (they ARE the snapshot, which is fine). **`Quantity` is
`int`** — labour hours (2.5 h) and lengths (13.85 m) cannot be expressed.

## Item-type enum — complete, but only an enum

`ServiceRequestOfferItemType`: Service=1, Product=2, Installation=3, Delivery=4, Labor=5, Inspection=6,
EmergencyFee=7, Discount=8, Other=99. **The enum existing does not mean the pricing/tax/validation for each type
exists** — it does not (no tax, no per-type totals, discount is a bare `IsDiscount` flag).

## Offer lifecycle — `ServiceRequestOfferStatus`

Draft=1, Submitted=2, UnderReview=3, Accepted=4, Rejected=5, Withdrawn=6, Expired=7.

**Missing vs the task:** no `RevisionRequested`/`Revised`, no `Viewed`. `Submit()` just flips status; `Update()`
mutates in place — **a submitted offer is mutable, and there is no version history.** The task wants immutable
submitted versions or explicit versioning; today neither exists.

## Offer commands — coarse-grained only

`Application/Command/Offer/`: **CreateServiceRequestOffer, UpdateServiceRequestOffer, WithdrawServiceRequestOffer,
RejectServiceRequestOffer, AcceptServiceRequestOffer**.

**No fine-grained item commands** — no AddOfferItem / UpdateOfferItem / RemoveOfferItem / ReorderOfferItems. The
itemized builder's per-row add/edit/delete/reorder has **no backend command**; today an offer is created (or
updated) with its whole item list in one aggregate call. (This is actually a reasonable MVP shape — see doc 08.)

The create handler was fixed in the discovery work (EF "temporary value" bug) and builds the aggregate then
`AddAsync` once.

## Request read side — mostly present

- `Query/Provider/GetProviderServiceRequestDetail/` exists, with the access check (biddable | has offer |
  assigned → else "not found"). Need to confirm it projects the **work scope items** and the **caller's offer
  with its items** (the detail page needs both).
- `ServiceRequestItemEntity` (work scope) is real: `ItemType` (`ServiceRequestItemType`), `Title`, `Description`,
  `Quantity` (int), `UnitCode`, `EstimatedUnitPrice` (decimal?), `SortOrder`. So **İş Kapsamı has real data.**
- `Attachments` (with FileStorage `FileId`), `Messages`/`Conversations`, `StatusHistory` all exist on the
  aggregate — the timeline and gallery have backing data, though the provider-scoped read/projection for each
  needs confirmation.

## Vessel

Snapshot columns were **removed** in 09b.1 — the request row carries only `VesselName` + `VesselId`. The detail
page's "Tekne Bilgileri" (type, model, year, length, beam, hull, engine) must be **BFF-enriched** from Vessel via
the bulk summary endpoint added in 09b.1 (`GET /vessels/summary?ids=`), **one call**, not per-field.

## Realtime

Bridge proven (module → RabbitMQ → BFF `ProviderRealtimeHub` → `city:{code}` / `provider:{id}`, Redis backplane).
Events today: `ServiceRequestPublished`, `Updated`, `Cancelled`, `UrgencyChanged`, `OfferAccepted`. **Missing for
this screen:** offer submitted (to owner), **customer viewed offer**, **revision requested**, message/question
added to the provider. No `ViewedAt` means "customer viewed" cannot be produced yet.

## Money / concurrency / idempotency

`TotalAmount`/`UnitPrice`/`EstimatedUnitPrice` are `decimal` (good — no float). **No optimistic concurrency
token** on the offer (audit `ModifyDate` only). **No idempotency key** on submit. Both matter for an auto-saving,
double-click-prone offer builder.
