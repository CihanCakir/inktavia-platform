# BE_MO2 — owner offers inbox + cost-free economics breakdown (mobile)

> **Repos:** `addesso-project` (ServiceRequest module + MarineMobile BFF) + `inktavia-marine-mobile`. Owner Economics track
> phase **MO2** (after MO1 SR create/list/detail). The owner sees the **provider offers received on their service request**
> with the **full customer-facing economics breakdown** (line items, KDV, discounts, **customer total**), can **compare**
> offers, and can **reject** one with a structured reason. **No money moves yet** — accept → checkout is **MO3**. All
> **cost-free** (never provider cost/commission internals). Additive; identity from the token. Follow the MO1/mobile
> invariants. BE + FE one slice + `REPORT_BE_MO2_*.md`.

## Baseline (investigated)
- Owner offer actions exist at `api/v1/service-requests/{srId}/offers`: **Accept**, **Reject** (N-E `OfferRejectReason`),
  + **commission-preview** / **part-terms-preview** — the last two are **provider-facing** (provider's commission / part
  cost) and **must NOT be exposed to the owner**.
- **No owner "offers received on my SR" list query** — only provider-side `GetProviderOffers`. MO2 adds the owner one.
  Repo has `GetByServiceRequestIdAsync(srId)`.
- **The DTOs are already cost-free** ✅: `ServiceRequestOfferDto` = customer totals (`Subtotal`, `TaxTotal`, `DiscountTotal`,
  `GrandTotal`) + items + FX snapshots; `ServiceRequestOfferItemDto` = ItemType/Title/Qty/UnitPrice/TaxRate/LineSubtotal/
  TaxAmount/LineTotal/DiscountAmount + S3 FX (`SourceUnitPrice`/`SourceCurrencyCode`/`SettlementCurrencyCode`) — **no
  commission base, no funding split, no provider net.** (Note: both DTOs default `CurrencyCode="USD"` — display the
  **settlement currency (TRY)** / the offer's real currency, per X4.)

## BE — owner offers surface
1. **SR module:** add `GetServiceRequestOffersForOwner(serviceRequestId)` query + handler — **owner-scoped** (verify the
   caller owns the SR via `OwnerUserId` from the token; reject otherwise, like the provider detail's access check).
   Returns the offers **received** on the SR — filter to owner-visible statuses (Submitted / UnderReview / Accepted /
   Rejected / Withdrawn as appropriate; **exclude other providers' Drafts**). Reuse `GetByServiceRequestIdAsync`. Add an
   **owner offer detail** (one offer with its full item breakdown) if the list doesn't already carry items. Cost-free DTO
   only (the existing offer + item DTOs are safe; do **not** add commission/funding fields).
2. **Mobile BFF:** extend MO1's `IServiceRequestRemoteCall` with the owner offers-list + offer-detail; expose a
   **cost-free mobile owner offer DTO** (provider display name, status, ETA, `Subtotal/TaxTotal/DiscountTotal/GrandTotal`,
   the cost-free line items with S3 FX display, deposit/warranty/payment-terms notes). **Do NOT** proxy the
   commission-preview / part-terms-preview (provider-only). Add a **reject** passthrough (N-E `OfferRejectReason` + optional
   note). **Do NOT** wire accept (MO3). Controller `api/v1/mobile/service-requests/{srId}/offers` (+ `/{offerId}`,
   `/{offerId}/reject`), `[Authorize]` participant; owner identity server-side.

## FE — offers inbox on the owner SR detail (`inktavia-marine-mobile`)
- On the SR detail (MO1), add an **Offers** section/screen: list the received offers (provider, **GrandTotal in ₺**, status,
  ETA), sorted/comparable; loading/empty ("no offers yet") / error states.
- **Offer detail:** the cost-free breakdown — line items (title · qty · unit · line total), **KDV**, **discount**, and the
  **customer total** the owner would pay; S3 FX shown if a line was foreign-priced ("≈ ₺… · kur kabulde sabitlenir"). **No
  cost/commission anywhere.**
- **Reject** with the N-E `OfferRejectReason` picker (+ optional note). **Accept** → a **coming-soon** stub (MO3 wires
  checkout).
- Currency: display the offer's **settlement currency (TRY)**, not the stale USD default.

## Don't-break / QA
- Additive: new owner offers query + BFF passthrough + FE section. Provider offer flows, the commission/part previews, and
  the economics math are unchanged. **Cost-free:** grep the owner offer payload — no commission base, funding split, provider
  net, or part cost/margin. Envelope mapping per MO invariants; identity from token; secrets from env. BE builds 0 errors;
  FE tsc+lint clean; tr+en; mock parity.
- Tests: (1) owner sees offers on their own SR only (another owner's SR → rejected); (2) other providers' drafts excluded;
  (3) the payload carries customer totals + cost-free items and **no** provider internals; (4) reject with an N-E reason
  transitions the offer + surfaces the reason; (5) accept is not exposed yet (MO3).

## Report
`docs/V1.0.1/Mobile/Owner/REPORT_BE_MO2_OFFERS.md`: the owner offers query (scoping + status filter), the cost-free mobile
DTO + BFF passthrough (commission-preview deliberately excluded), the FE offers inbox + breakdown + reject, the
cost-free/currency verification, and the tests. Then **MO3** (accept → checkout, iyzico-gated).
