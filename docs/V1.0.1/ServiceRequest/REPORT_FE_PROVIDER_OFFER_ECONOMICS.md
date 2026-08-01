# REPORT_FE — Provider Offer Economics ("Sen ne alırsın" panel)

**Repo:** `inktavia-marine-provider-web` · **Spec:** `FE_PROVIDER_OFFER_ECONOMICS.md` · **Backend:** BFF-Wave 5 (S7 commission + S6 customer discount, runtime-verified)

## What was built (additive — existing builder untouched)

The itemized offer builder (`src/features/service-requests/offer/`) was **extended**, not rewritten. The
"server owns every total" rule is preserved: the FE never computes commission/net/discount — it previews them
from the BFF and reads back the authoritative numbers.

1. **`endpoints.ts`** — added the two provider preview paths under `offers`:
   - `offers.commissionPreview = '/provider/offers/commission-preview'`
   - `offers.customerDiscountPreview = '/provider/offers/customer-discount-preview'`

2. **`api/offerApi.ts`** — extracted the line-input mapping into an exported `toLineInputs(items, currencyCode)`
   (reused by both the draft/preview bodies and the new economics adapter — one source of truth for "inputs only").

3. **`api/offerEconomicsApi.ts`** (new) — `commissionPreview()` + `customerDiscountPreview()`. POST **only the line
   inputs** via the authenticated `httpClient` (Authorization: Bearer, provider by-subject). Response is defensively
   coerced (null-safe `num`/`bool`, tolerant of missing/renamed fields), and any transport error is caught and
   returned as an `ApiResult` failure so the caller degrades instead of throwing. DTO field names aligned to the
   S7/S6 resolver shapes named in `REPORT_BFF.md` (BFF-Wave 5):
   - commission → per-line `{lineRef, commissionable, commissionBase, resolvedRate, commissionAmount, providerNet}`
     + `{transactionCommission, transactionProviderNet, transactionCommissionBase}`
   - discount → `{found, requestedDiscount, funding{platform/provider/shared}, perLine[], requiresProviderConsent}`

4. **`hooks/useOfferEconomics.ts`** (new) — debounced (1500 ms, same cadence as auto-save), fires on line-input
   change only (input signature excludes unrelated re-renders), aborts in-flight requests, runs both previews in
   parallel. Status machine: `idle` (no lines → panel hidden) · `loading` · `ready` (≥1 endpoint ok) · `error`
   (both failed → soft note). Never touches the draft or the customer totals.

5. **`components/OfferEconomicsPanel.tsx`** (new) — the "Sen ne alırsın" panel, rendered **below** the existing
   customer totals card (which is unchanged):
   - **Provider net (take-home)** — `transactionProviderNet`, large + gold-emphasized.
   - **One-glance flow** — "Müşteri öder X → komisyon Y → sen alırsın Z".
   - **Commission + effective rate** (`transactionCommission / transactionCommissionBase`) with a collapsible
     **per-line breakdown**: line → base × rate → commission → net.
   - **Exempt / pass-through lines** (Travel / MarinaFee → `commissionable=false`): base/rate shown as "—",
     commission 0, providerNet = full line, tagged with a "Muaf" badge + explanatory note.
   - **Customer discount + funding split** (platform / shared / provider) — provider-funded portion shown in
     danger color as a reduction with a transparent note; `requiresProviderConsent` surfaces a consent hint.
   - **Null-safe degrade** — both endpoints failing → soft "önizleme alınamadı" note; no lines → panel hidden.

6. **`OfferBuilder.tsx`** — one `useOfferEconomics(draft.items, draft.terms)` call + `<OfferEconomicsPanel>`
   render. No change to draft/preview/submit or the customer-facing totals.

7. **i18n** — full `economics.*` key set added to `tr` + `en` `offerBuilder.json` (title, net, flow, commission,
   effective rate, per-line columns, exempt, funding split, consent hint, unavailable note).

## Verify

- **`tsc --noEmit` / `tsc -b`** — **0 errors.**
- **`vite build`** — bundling succeeds (`✓ built`). The only build failure is a **pre-existing** PWA workbox
  precache limit (main chunk 2.18 MB on baseline → 2.19 MB with this feature, both > the 2 MiB default); reproduced
  on a clean checkout with the feature stashed, so it is not introduced here.
- **`eslint`** (offer feature + endpoints) — **0 errors.**
- **`vitest run`** — **13/13 pass**, no regression.
- **Flow verified by construction:** panel renders provider-net / commission / discount from the BFF on line
  changes (debounced 1.5 s); per-line breakdown labels match rows by `lineRef`→`sortOrder` (index fallback);
  exempt lines render distinctly (commission 0, full net); endpoint error degrades to a soft note; existing
  draft/preview/submit flow and the customer totals card are unchanged.

## Notes / assumptions

- Response parsing is defensive (coerces the raw resolver DTO the BFF wraps via `SetResponse`); if a field name
  differs from the S6/S7 shape it degrades to a neutral value rather than breaking the panel.
- `requiresProviderConsent` drives the transparency hint; the optional FE_PROVIDER_I1 "net shown once payment
  setup is complete" string is available as `economics.netPendingSetup` for the split-eligibility banner to reuse.
- **CargoDry untouched.**
