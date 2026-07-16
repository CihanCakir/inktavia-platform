# 08 — API & Event Contracts

Envelope `{ header:{isSuccess,errorCode,errorMessage}, body }`; rejection may be HTTP 200 + `isSuccess:false`.
**Typed DTOs only — no `object`, no `System.Text.Json.JsonElement` on the wire.** `ProviderProfileId` is **never**
a request field or an authorization input — it is resolved from the assertion. All routes under
`api/v1/provider/service-requests`, auth = provider.

## Read

### `GET /{serviceRequestId:long}/detail`
The aggregate. `ProviderServiceRequestDetailDto`:
- request: id, code, title, description, status, priority, category/type codes, publishedAt, requestedStart/End,
  expiresAt, marina/city, `approxLatitude/Longitude`, `distanceKm?`, attachmentCount, offerCount, `isUpdated`.
- `workScope: WorkScopeItemDto[]` — itemType, title, description, quantity, unitCode, sortOrder (no prices to the
  provider unless the owner chose to publish `EstimatedUnitPrice` — **decide**; default: hide owner price).
- `vessel: VesselSummaryDto?` — BFF-enriched (name, typeCode, brand, model, lengthValue+unit, beam, hullMaterial,
  year) — nullable, one bulk call.
- `attachments: AttachmentMetaDto[]` — fileId, kind, contentType, sizeBytes, createdAt. **No signed URL here.**
- `myOffer: ProviderOfferDto?` — the caller's current offer with items + server totals (below), or null.
- privacy: no owner PII beyond what the owner published; no other providers' offers; snapped coords only.

Errors: `SR_DETAIL_NOT_FOUND` (also = access denied, deliberately indistinguishable).

### `GET /{serviceRequestId}/attachments/{fileId}/read-url`
Mints a short-lived signed **read** URL (FileStorage). One per click. Never stored, never in the aggregate.

### `GET /{serviceRequestId}/timeline`  (may be folded into detail or lazy)
`TimelineEntryDto[]` — durable events only (status history, offer submitted/viewed/accepted, message posted).
Marked `kind` so the UI separates audit history from ephemeral toasts.

## Offer write

### `POST /{serviceRequestId}/offer/draft` — get-or-create draft
Returns `ProviderOfferDto` (creates an empty Draft if none). **One active offer per (provider, request).**

### `PUT /{serviceRequestId}/offer/draft` — aggregate save (auto-save target)
Request `SaveOfferDraftDto`: `currencyCode`, `items: OfferItemInputDto[]`, commercial terms
(`estimatedStartDate`, `estimatedDurationMinutes`, `expiresAt`/validity, `depositType?`, `depositValue?`,
`paymentTermsNote?`, `warrantyNote?`, `providerNotes?`), and a **`concurrencyToken`**.
`OfferItemInputDto`: `itemType`, `title`, `description?`, `quantity` (decimal), `unitCode?`, `unitPrice`,
`taxRate`, `discountType?`, `discountValue?`, `sortOrder`. **No client-supplied line/tax/total/subtotal.**
Response: the recomputed `ProviderOfferDto` with a fresh concurrency token.
Errors: `SR_OFFER_STALE` (concurrency), `SR_OFFER_INVALID_ITEM`, `SR_OFFER_MIXED_CURRENCY`,
`SR_OFFER_UNKNOWN_UNIT`, `SR_OFFER_NEGATIVE_TOTAL`.

### `POST /{serviceRequestId}/offer/preview`
Same body as save; returns computed `ProviderOfferDto` **without persisting** — for "Önizle". Server-computed.

### `POST /{serviceRequestId}/offer/submit`
Header `Idempotency-Key`. Transitions Draft→Submitted, freezes content, sets `SubmittedAt`. Empty offer → reject.
Re-sending the same key returns the same result, not a second offer. Errors: `SR_OFFER_EMPTY`,
`SR_OFFER_ALREADY_SUBMITTED`, `SR_OFFER_REQUEST_CLOSED`.

### `POST /{serviceRequestId}/offer/withdraw`
Withdraw a submitted offer (reason). Keeps the row for history. Allowed while the request is open and no offer
accepted.

### `ProviderOfferDto` (response shape)
`offerId`, `status`, `currencyCode`, items (with **server-computed** `lineSubtotal`, `taxAmount`, `lineTotal`,
`discountAmount`), commercial terms, timestamps (`submittedAt?`, `viewedAt?`, `revisionRequestedAt?`,
`acceptedAt?`, `rejectedAt?`, `withdrawnAt?`), **totals**: `categoryTotals` (per item type), `discountTotal`,
`subtotal`, `taxTotal`, `grandTotal`, and `concurrencyToken`.

## Events (existing bridge, extended — target `provider:{profileId}`)

| Event | Producer | New? | UI effect |
|---|---|---|---|
| `ServiceRequestUpdated` / `Cancelled` / `UrgencyChanged` | exists | no | refresh detail |
| `OfferAccepted` / `OfferRejected` | exists (accepted) | rejected: new | status banner + timeline |
| `OfferViewedByCustomer` | **new** (needs `ViewedAt`) | yes | "viewed" banner |
| `OfferRevisionRequested` | **new** (needs `RevisionRequestedAt`) | yes | banner + enable revise |
| `ServiceRequestMessageAdded` | **new/verify** | maybe | timeline + thread |

Rules: idempotent consumers; dedupe by id; realtime is a hint (invalidate → refetch the truth); never broadcast
one provider's offer to another; the timeline's durable entries come from module state, not socket frames.
