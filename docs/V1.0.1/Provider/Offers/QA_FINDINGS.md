# Provider QA — Offers (P-QA2)

## Static findings
- **Implemented** (`OffersPage`, 4 query hooks) but **nav marks it `planned`** → false "soon" badge (fix in Navigation).
- The offer builder carries the economics surfaces built this session: S1 line economics, **S2 pricing-attribute picker**,
  **S3 offer-level FX display** (₺converted + rate + "fixed at acceptance"), **S4 travel sub-form** (Flat/PerKm), **S5 part
  cost-free allowance hint**, S6/S7 commission+discount preview. Offer-library (`offer-library`) has no api/hooks dir —
  verify it's real (reuses offers api) or reclassify.

## Live walkthrough checklist (localhost:3002/app/offers + offer builder)
- [ ] Offers list loads (loading/empty/error), no false "soon" badge in the sidebar.
- [ ] Build an offer: line items compute server-side totals; **attribute picker** shows real R4 lookup options; required
      attribute blocks submit.
- [ ] **FX:** a non-TRY offer shows ₺converted + rate + freeze note; a TRY offer shows no FX chrome; no-rate → clear inline error.
- [ ] **Travel line:** PerKm km×rate stays consistent with the line; Flat fee works; KILOMETER shown.
- [ ] **Part line:** cost-free allowance hint (max discount + funded split + min-receivable) — **no cost/margin anywhere**.
- [ ] Economics preview (commission/net/customer total) is coherent; submit → offer appears; withdraw/edit work.
- [ ] Offer-library: templates/catalog load and seed an offer.

## Fix candidates
- Nav flag (→ Navigation). Any runtime/visual issues found in the walkthrough → `FIX_*` doc here.
