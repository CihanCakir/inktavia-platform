# 05 — Data Ownership & Gap Matrix

Size XS/S/M/L/XL · MVP y/n · Blocker y/n. Owner = the module that owns the field.

## A. Request detail fields

| Field | Owner | Exists? | Gap | MVP | Blk |
|---|---|---|---|---|---|
| Id, RequestCode, Title, Description, Status, Priority | ServiceRequest | ✅ | reusable | y | n |
| Category / subcategory | ServiceRequest + RefData | ✅ codes; no subcategory list | code→i18n | y | n |
| **Structured work scope** (`ServiceRequestItem[]`) | ServiceRequest | ✅ (ItemType, Title, Description, Quantity(int), UnitCode, EstimatedUnitPrice, SortOrder) | confirm the detail query **projects** it | y | **y** |
| PublishedAt, RequestedStart/End, ExpiresAt | ServiceRequest | ✅ | — | y | n |
| Expected duration | ServiceRequest | ⚠️ on offer only, not request | S | n | n |
| **Budget** | — | ❌ decision: not built | do not render | — | n |
| Marina/city, approx coords, distance | ServiceRequest | ✅ (snapped coords, DistanceKm from geo) | reuse discovery rules | y | n |
| **Vessel summary** (type/model/year/length/beam/hull/engine) | Vessel | ❌ on request (snapshot removed 09b.1) | **BFF bulk-enrich, one call** | y | **y** |
| Attachment/photo counts + files | ServiceRequest + FileStorage | ✅ counts; files by `FileId` | signed read URL per click | y | n |
| Total offer count | ServiceRequest | ✅ aggregate | — | y | n |
| My offer state + items | ServiceRequest | ⚠️ offer exists; **confirm detail returns my offer WITH items** | S | y | **y** |
| Last update time | ServiceRequest | ✅ (`ContentUpdatedAt` added 13b) | — | y | n |

## B. Work-scope item fields (`ServiceRequestItemEntity`)

Name/Title ✅, ItemType ✅, Description ✅, Quantity ✅ (int), Unit ✅ (`UnitCode`), EstimatedUnitPrice ✅.
**Missing (mock-implied, low priority):** customer-provided vs provider-required materials, explicit
installation/inspection/delivery flags, special instructions. These are **request-authoring** concerns (owner
side), not the provider offer — **out of scope for this screen**; the provider reads the scope, prices against it.

## C. Offer fields (`ServiceRequestOfferEntity`)

| Field | Exists? | Gap | MVP | Blk |
|---|---|---|---|---|
| OfferId, RequestId, ProviderProfileId, Status, CurrencyCode, TotalAmount | ✅ | — | y | n |
| Description, ProviderNotes | ✅ | reuse as notes/assumptions | y | n |
| EstimatedStart/End, EstimatedDurationMinutes, ExpiresAt | ✅ | — | y | n |
| Accepted/Rejected/Withdrawn At + reasons | ✅ | — | y | n |
| **Version** | ❌ | **missing field** — no versioning | see doc 06 §lifecycle | **decide** | maybe |
| **ViewedAt** (customer viewed) | ❌ | missing field + event | S | n | n |
| **RevisionRequestedAt** | ❌ | missing field + event | S | n | n |
| **Subtotal, TaxTotal, per-category totals** | ❌ | **missing** — only one `TotalAmount` | see doc 06 | y | **y** |
| **Deposit amount/rate, payment terms, warranty** | ❌ | missing fields | M | **decide** | n |
| **Concurrency token** | ❌ | missing (audit only) | S | y | n |
| SubmittedAt | ⚠️ (audit `ModifyDate` on Submit) | add explicit `SubmittedAt` | S | y | n |

## D. Offer-item fields (`ServiceRequestOfferItemEntity`)

| Field | Exists? | Gap | MVP | Blk |
|---|---|---|---|---|
| ItemType, Title, Description, UnitPrice(decimal), CurrencyCode, SortOrder, IsDiscount | ✅ | — | y | n |
| **Quantity** | ⚠️ **`int`** | mock has "2 Adet" but labour/length need decimal → **change to `decimal`** | M | **y** |
| **UnitCode** | ❌ | mock shows "Adet", "10 Litre kova" → add `UnitCode` | S | y | n |
| **TaxRate, TaxAmount** | ❌ | mock KDV %20 → **add** (per line) | M | **y** |
| **LineSubtotal, LineTotal** | ❌ | server-computed snapshots | M | y | n |
| **DiscountType/Value/Amount** | ❌ | discount is a bare `IsDiscount` bool + Discount type | see doc 06 | y | n |
| Catalog source id, name/desc snapshot | ❌ | no catalog module → **manual only, MVP** | — | n | n |

## E. Attachments / messages / activity

- Attachments: `FileId` on the aggregate; signed read URL minted by the BFF per click (onboarding pattern).
  Photo gallery + documents. **MVP: read-only display.**
- Messages/questions + activity timeline: `Conversations`/`Messages`/`StatusHistory` exist on the aggregate.
  **Confirm a provider-scoped projection.** The timeline must distinguish durable audit events from ephemeral UI
  toasts (doc 11).
- Provider **posting** a message: check whether the provider→owner message command exists; if not, MVP shows the
  thread read-only and defers posting.

## F. Realtime events needed

Request updated / cancelled / urgency (✅ exist). **Missing:** offer submitted (→owner), **customer viewed**
(needs `ViewedAt`), **revision requested** (needs `RevisionRequestedAt`), message added (→provider). Producers
must be added module-side; consumers on the BFF hub. No parallel infra.

## G. Privacy (reuse discovery rules)

Provider sees only an authorized/open/own request (access check already: biddable | has offer | assigned → else
"not found"). Exact berth/customer address hidden pre-authorization; approximate (snapped) coords in detail;
customer PII excluded except what the owner chose to expose (name in "Hızlı Bilgiler" — confirm this is
owner-approved display, not a leak). Other providers' offers never exposed; only the aggregate count. Attachment
signed URLs short-lived and authorized. Browser location is discovery/distance UX only, never authorization.
