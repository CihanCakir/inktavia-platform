# FIX X4 — currency inconsistency (₺ TRY vs USD) on provider Finance + CargoDry

> **Repos:** `addesso-project` (Payment / CargoDry modules + their provider BFF DTOs) + `inktavia-marine-provider-web`
> (verify only). **Diagnostic-first, backend-rooted.** The provider sees earnings/targets in **USD** on Finance + CargoDry
> while offers are in **₺ (TRY)** — the marketplace settles in TRY. Go-live critical. Additive/corrective; no economics
> math change (only the currency **label/code** carried in read DTOs). **Do not commit** until reviewed.

## Root cause (already diagnosed)
The provider-web is **not** hardcoding USD: `src/shared/lib/formatters/currencyFormatter.ts` defaults to `'TRY'`, and the
finance components format via `money(amount, currencyCode)` where **`currencyCode` comes from the API DTO**
(`financeApi.ts` has `currencyCode: string` on 8 shapes; subscription even has an explicit `monthlyPriceTRY`). So the **USD
is coming from the backend** — the finance settlement/economics + CargoDry commission read models return
`currencyCode = "USD"` (a stale default; note `ServiceRequestOfferEntity.CurrencyCode` also defaulted to `"USD"` in source).
**Fix the backend to carry the settlement currency (TRY); the FE will render ₺ automatically.**

## Phase 0 — trace the USD source (per surface)
1. **Finance › Hakedişler / Ödemeler / İşlemler / economics breakdown / refund summary:** find where each DTO's
   `currencyCode` is set (Payment settlement/economics snapshot → provider finance BFF). Is it read from the economics
   snapshot's currency (which should be TRY at acceptance), or defaulted to `"USD"` somewhere in the mapping?
2. **CargoDry commission / tier / earnings** ("5.000 USD", "150 USD", "60 USD", "0 USD"): find the CargoDry provider
   read-model currency — is it a stale `"USD"` default, or is CargoDry consignment **intentionally USD-denominated**?
3. Confirm the **SR/offer path already yields TRY** (it does on screen) so the target is consistent: **TRY everywhere the
   marketplace settles.**

## Decision (gate on Phase 0)
- If the USD is a **stale default** (most likely): set/propagate the **settlement currency = TRY** through the finance +
  CargoDry read DTOs — read the real currency from the economics snapshot (TRY) instead of a hardcoded/default `"USD"`;
  fix any entity/DTO defaulting to `"USD"` on a read path (e.g. carry the acceptance-snapshot currency).
- If CargoDry commission is **genuinely USD** by product design: make that **explicit and consistent** (label it, and don't
  mix USD tiles with TRY earnings on the same provider surface) — but reconcile with the TRY marketplace; most likely it
  should also be **TRY**. Record the decision in the report.

## Fix
- Propagate the true settlement currency (**TRY**) into the finance + CargoDry provider read DTOs (from the economics/
  settlement snapshot, not a literal). Remove stale `"USD"` defaults on read paths. Additive — no change to stored amounts
  or economics math; only the `currencyCode` value returned.
- The FE needs **no change** (it already respects `currencyCode`); if any component ignores the field and assumes a
  currency, fix it to use the DTO's `currencyCode`.

## Verify (on screen — localhost:3002)
- [ ] Finance › all tabs show **₺ (TRY)**, consistent with Offers/Service-Requests; no "USD".
- [ ] CargoDry tiles/tier/commission show TRY (or, if USD is intentional, it's explicit + reconciled — documented).
- [ ] Amounts unchanged in value (only the currency label/code corrected); economics math untouched.
- [ ] The whole provider app reads one consistent settlement currency.

## Report
`docs/V1.0.1/Provider/Finance/REPORT_X4_CURRENCY.md`: where the USD originated per surface, the TRY-vs-intentional-USD
decision, the backend DTO/default fixes, and the on-screen confirmation. Cross-link from `CargoDry/`.
