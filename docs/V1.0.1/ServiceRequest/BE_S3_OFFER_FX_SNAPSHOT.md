# BE_S3 — provider price-book FX + offer-time exchange-rate snapshot (frozen at acceptance)

> **Repo:** `addesso-project` — **ServiceRequest module** (+ the existing ReferenceData remote call; the Payment
> economics contract carries the snapshot through). SR second-wave phase S3 (§20.7). Lets a provider quote a line in a
> **foreign currency** (EUR/USD — common for marine parts/services) while the platform **settles in TRY**: the foreign
> price is **converted to TRY at offer-submit time** using the R1 point-in-time rate, the **rate is snapshotted on the
> offer**, and it is **frozen at acceptance** (no re-valuation — §20.7 "TL kabulde sabit"). Descriptive-for-the-invariants
> in the sense that the **8-equality economics runs in a single settlement currency (TRY)**; FX is a convert-at-entry +
> immutable rate snapshot, not a change to the invariant math.

## Scope decision (safest MVP-compatible — read first, redirect if wrong)
The platform settles in **TRY** (marketplace model: TL fixed at acceptance, float/FX≈0 unless iyzico revenue-share —
`payment_model_iyzico_marketplace`). So S3 does **not** make the economics multi-currency. Instead:
- A line may be **entered** in a foreign source currency (`ServiceRequestOfferItemEntity.CurrencyCode` already exists,
  per-line).
- At **offer submit**, each distinct source currency → **TRY** is resolved via **R1** `ResolveExchangeRate(source, "TRY",
  submitInstant)`, the **converted TRY unit price** becomes the amount the economics uses, and the **rate is snapshotted**
  on the offer.
- The offer's **settlement currency is TRY**; the **8-equality (S1/S6/S7/S8) runs on the TRY amounts exactly as today** —
  it always saw a single currency; nothing in the invariant math changes.
- The **raw foreign price + source currency are retained per line** for transparency/display ("500 EUR = 21.300 TRY @
  42.60").
- At **acceptance (P8)** the FX snapshot is **frozen immutable** alongside the line economics snapshot → the accepted TRY
  total never re-values.

**Alternative not taken (flag):** full multi-currency settlement (economics in the offer currency, convert only at
payout). Rejected for MVP — it would rewrite the 8-equality in N currencies and contradicts "TL fixed at acceptance."
If you actually want the offer to settle in a non-TRY currency, say so and this spec changes materially.

## Current state (investigated)
- **Price-book already exists:** `ProviderCatalogItemEntity` (`DefaultUnitPrice`, `CurrencyCode`, `UnitCode`,
  `DefaultTaxRate`) with full CRUD (`CreateCatalogItemCommand`/`Update`/`Delete`/`ListCatalogItemsQuery`) + provider offer
  templates (`ProviderOfferTemplate(Item)`). **Do not rebuild it** — extend it for FX awareness only.
- **Offer + lines carry a per-item `CurrencyCode`** (`ServiceRequestOfferItemEntity`, defaults "USD"); offer-level
  `CurrencyCode` too. Computed line totals (`LineSubtotal`/`TaxAmount`/`LineTotal`/`CommissionBaseAmount`) are set by the
  calculation service; the Payment economics command (`CalculateServiceRequestEconomics`) is **single-currency**
  (`request.CurrencyCode`) — it does **no** conversion today.
- **R1 remote call is live:** `IServiceRequestReferenceDataRemoteCall.ResolveExchangeRate(from, to, asOfUtc)` →
  `SrExchangeRateResolveDto { HasRate, Rate, RateDate, AsOfUtc }` (empty result when no rate is effective at the instant).
- **No FX snapshot entity exists yet** — build it.

## S3a — convert foreign lines to TRY at offer submit (single resolve instant)
- Define the **settlement currency = "TRY"** (a module constant for MVP; do not hardcode it in ten places — one
  `SettlementCurrency` const/config).
- When an offer is **submitted** (`SubmitOffer` — the single deterministic instant; not on every draft edit), for each
  **distinct source currency** among its lines where `source != "TRY"`:
  - resolve `ResolveExchangeRate(source, "TRY", submitInstantUtc)` via the remote call;
  - **fail loud** if `HasRate == false` (cannot price a foreign line without an effective rate) — new SR error code, e.g.
    `SR_FX_RATE_UNAVAILABLE`;
  - the **converted TRY unit price** = `round(sourceUnitPrice * rate, 2)` (bankers'/`MidpointRounding.AwayFromZero` per
    the project's existing money-rounding convention — match S1, don't invent a new one).
- The economics then runs on the **TRY** unit prices (single currency) — **the 8-equality is untouched**. Keep the
  original `CurrencyCode` + a new `SourceUnitPrice` (raw foreign) on the line for display; the effective `UnitPrice` used
  by the math is TRY.
- TRY-only offers (the common case) resolve nothing — **no remote call, no behavior change**.

## S3b — `OfferFxSnapshotEntity` (offer-level, one row per source currency)
- New immutable-once-accepted child of the offer: `OfferFxSnapshotEntity(offerId, sourceCurrencyCode, settlementCurrencyCode,
  rate, rateDate, resolvedAtUtc)`. One row per distinct non-TRY source currency used by the offer's lines. Written/updated
  on submit (a re-submit before acceptance re-resolves — the rate is only *frozen* at acceptance).
- UTC-safe (`resolvedAtUtc`, `rateDate` — timestamptz rule; `Kind=Utc`). Validating factory; no free mutators.

## S3c — carry the FX snapshot through to the Payment acceptance snapshot (freeze)
- Thread the FX rows (source ccy, settlement ccy, rate, rateDate, resolvedAt) SR→Payment through the existing
  **`CalculateServiceRequestEconomics` remote-call request** (extend the request DTO additively — same pattern S2d used to
  carry attribute labels). The economics itself stays TRY-only; the FX rows ride along as **frozen metadata**.
- In the **P8 acceptance path** (`CreateFromLines`), persist an immutable `OfferLineFxSnapshot`-style record (or fold the
  rate into the existing line economics snapshot) capturing, per converted line: source ccy, source unit price, applied
  rate, rateDate, resolved TRY unit price — **denormalized/self-contained**, no re-lookup or re-resolve after acceptance
  (§20.7, §20.15). FK Restrict, no mutators; **tamper → throw** (mirror the S2d snapshot discipline).
- After acceptance the TRY total is **fixed** — no path re-resolves FX.

## S3d — price-book FX awareness (small, additive)
- When a **catalog item priced in a foreign currency** is pulled into an offer line (the "seed line from catalog/template"
  path, if present — otherwise this is a no-op), copy its `CurrencyCode` + price as the line's **source** price; conversion
  still happens at submit via S3a (single instant, not at catalog-pull time — so the rate is the offer-time rate, not the
  catalog-edit-time rate). No new catalog schema needed; the catalog already stores `CurrencyCode`.
- The provider offer-preview (`PreviewOffer`) should surface the **converted TRY** figure + the source figure so the
  provider sees the TRY the customer will be charged (FE follows; backend must return both in the preview/detail DTO).

## Don't-break / QA
- **Additive:** new `SourceUnitPrice` column on the offer line + `OfferFxSnapshotEntity` + the acceptance FX snapshot +
  the R1 resolve call at submit + additive fields on the economics request DTO. Existing offer/line economics
  (S1/S6/S7/S8), P8 acceptance, and the **8-equality invariants are unchanged** — they run in TRY exactly as today (a
  TRY-only offer is byte-for-byte identical; the S8 8-equality suite must stay green).
- Migration applies cleanly; `SourceUnitPrice` nullable/back-compat (existing rows = null → treated as TRY-native).
  Cacheable ReferenceData reads per convention (R1 is already cached by from/to/as-of-day). UTC-safe. Fail-loud on missing
  rate. Builds clean.
- Unit tests: (1) TRY-only offer → **no** resolve call, economics + 8-equality identical to pre-S3 (regression guard);
  (2) EUR line → converted at the resolved rate, TRY economics correct, 8-equality holds in TRY; (3) `HasRate=false` →
  `SR_FX_RATE_UNAVAILABLE` (no silent 0/1.0 rate); (4) acceptance freezes the FX snapshot — post-acceptance the stored
  rate is immutable and tamper throws; a rate change in ReferenceData after acceptance does **not** move the accepted
  total; (5) rounding matches S1's money convention.

## Verify
1. Provider builds an offer with one EUR line (e.g. 500 EUR) + one TRY line → on submit the EUR line converts to TRY at
   the R1 offer-time rate; preview shows both "500 EUR" and the TRY figure; the offer's `OfferFxSnapshot` row records the
   rate.
2. No effective EUR/TRY rate at submit instant → submit is rejected with `SR_FX_RATE_UNAVAILABLE` (not a silent
   mispricing).
3. Owner accepts → the FX snapshot is frozen; changing the EUR/TRY rate in ReferenceData afterward leaves the accepted
   TRY total unchanged (no re-valuation); the acceptance snapshot reads the rate self-contained (no re-resolve).
4. A TRY-only offer behaves exactly as before S3 (no resolve call; economics + 8-equality byte-identical).

## Report
`docs/V1.0.1/ServiceRequest/REPORT_S3.md`: the settlement-currency decision, the submit-time resolve + rounding, the
`OfferFxSnapshot` model, the acceptance freeze (self-contained, no re-valuation), the price-book FX-awareness touch, the
regression proof that TRY-only offers + the 8-equality are unchanged, and the test results. Then FE (provider offer FX
display + admin visibility) + next SR phase (S4 travel — I2 ready / S5 part terms).
