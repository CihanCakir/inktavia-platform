# REPORT — X4: currency inconsistency (₺ TRY vs USD) on provider Finance + CargoDry

> Executes `FIX_X4_CURRENCY_TRY.md`. **Diagnostic-first, backend-rooted.** The provider saw earnings/targets in **USD**
> on Finance › Hakedişler + the CargoDry pages, while Offers/SR (and Finance › Ödemeler/İşlemler) were **₺ (TRY)** — the
> marketplace settles in TRY. Additive/corrective — **no economics math change, only the `currencyCode` in read DTOs**.
> Verified on `localhost:3002`. **Not committed.**

---

## Phase 0 — where the USD came from (per surface)

The provider-web is **not** hardcoding USD (`currencyFormatter` defaults `'TRY'`; components render `money(amount,
currencyCode)` from the DTO). The USD was purely backend. Traced per surface:

| Surface | Backend endpoint / handler | Currency source | Verdict |
|---------|----------------------------|-----------------|---------|
| Finance › **Ödemeler** (payouts, balance) | Payment `GetProviderPayouts` → `balance.CurrencyCode` (`ProviderBalanceEntity` default **"TRY"**) | entity, TRY | ✅ already TRY |
| Finance › **İşlemler** (transactions) | Payment `GetProviderTransactions` → `x.CurrencyCode` | entity, TRY | ✅ already TRY |
| Finance › **Hakedişler** header (Bekleyen/Planlanan/Ödenen) | **CargoDry** `GetCargoDryProviderPayoutSummaryQueryHandler` | **hardcoded `= "USD"`** | ❌ stale literal |
| Finance › Hakedişler trend | CargoDry `GetCargoDryProviderCommissionTrend` | `product.CurrencyCode ?? "USD"` | ❌ stale (seed USD) |
| **CargoDry** tier / earnings / target / potential / commission / inventory | CargoDry `GetCargoDryProviderTier` / `…Earnings` / `…ProductPerformance` / `GetProviderInventoryList` | `product.CurrencyCode ?? "USD"` + DTO defaults `= "USD"` | ❌ stale (seed USD) |

**Root cause:** the **CargoDry provider read model + seed** carried a stale **"USD"** placeholder — 7 abstraction DTOs
defaulted `CurrencyCode = "USD"`, 5 read handlers fell back to `?? "USD"` off the product's stored currency, the
settlements-summary handler **hard-coded** `"USD"`, and the **seed** stored products/consignment as `"USD"`. Payment
was already correct (`ProviderBalanceEntity`/`PayoutRecordEntity` default `"TRY"`; payout-summary hard-codes `"TRY"`).

**Decision-clinching evidence:** the Finance › **Ödemeler** tab shows the *same* CargoDry settlements paid out in
**TRY** ("CargoDry settlement payout — 60 TRY / 40 TRY"), and the CargoDry **consignment-agreement entity itself
defaults to `"TRY"`**. So the money genuinely settles in TRY; CargoDry's read-model USD was a mislabel.

## Decision (gated on Phase 0)

**USD is a stale placeholder, not an intentional denomination.** The marketplace settlement currency is **TRY**
everywhere (Offers, SR, Payment payouts/transactions, the consignment agreement). CargoDry is reconciled to **TRY**.
No economics/amount change — only the `currencyCode` label/code.

## Fix (backend, CargoDry module only — 14 files, `"USD"` → `"TRY"`)

1. **7 read DTO defaults** (`CurrencyCode { get; init; } = "USD"` → `"TRY"`): `CargoDryProviderTierDto`,
   `CargoDryProductPerformanceDto`, `CargoDryEarningsTrendPointDto`, `CargoDryProviderInventoryDto`,
   `CargoDryProviderSettlementDto` (×2), `CargoDryProviderEarningsDto`.
2. **6 read handlers**: `?? "USD"` → `?? "TRY"` (`GetCargoDryProviderEarnings`, `…CommissionTrend`,
   `…ProductPerformance`, `GetProviderInventoryList`, `GetCargoDryProviderTier`) and the hard-coded
   `CargoDryProviderPayoutSummary` `= "USD"` → `= "TRY"`.
3. **Seed** (so fresh environments store TRY): `CargoDryProductSeed` (4×) + `CargoDryProviderMockSeed` (agreement /
   attribution / settlement, 3×) `"USD"` → `"TRY"`. **Amounts unchanged** (149.99 / 249.99 / 399.99 / 299.99 / 5000 /
   150 / 60 / 30 …) — only the currency code.

The two ISO-4217 example **comments** (`e.g. "TRY", "USD"`) were deliberately left. Payment and the FE were **not**
touched (Payment already TRY; the FE already respects `currencyCode`). CargoDry module **builds: 0 errors**; no test
asserts `"USD"` (nothing broke).

### Dev-DB data relabel (verification aid)

The seeds are `AnyAsync`-guarded, so the already-running dev DB held the USD-seeded rows. To verify on the live
instance, the stored placeholder currency was relabeled in the isolated `cargodry` schema (amounts untouched):
`UPDATE cargodry.{products, consignment_agreements, sales_attributions, sell_through_settlements} SET "CurrencyCode"
= 'TRY' WHERE "CurrencyCode" = 'USD'` → 4 products + 1 agreement updated. Fresh environments get TRY from the
corrected seed; this relabel is the data-migration equivalent for the existing dev DB. The `cargodry-api` image was
**rebuilt + restarted** to apply the settlements-summary hard-code fix (a hard-code can't be relabeled via SQL).

## Verify (on screen — localhost:3002, PROVIDER 2 AS)

| Surface | Before | After |
|---------|--------|-------|
| Finance › **Hakedişler** (Bekleyen/Planlanan/Ödenen + trend) | `0 USD` | **`0 TRY`** |
| Finance › **Ödemeler** | 60/40 TRY | 60/40 TRY (unchanged) |
| Finance › **İşlemler** | 90.560 TRY … | 90.560 TRY … (unchanged) |
| **CargoDry Envanter** (tier `5.000 USD`, commission/earnings/target `0/150 USD`, potential `60/30 USD`) | USD | **`5.000 TRY` / `0 TRY` / `150 TRY` / `60 TRY` / `30 TRY`** |
| **CargoDry Ürünler** (per-sale + retail) | `55/100/84/30 USD`, `249,99/399,99/299,99/149,99 USD` | **same numbers, `TRY`** |
| **CargoDry Yenilemeler** (`YENİLEME GELİRİ`, `Komisyonun`, `Fiyat`) | `30 / 30 / 149,99 USD` | **`30 / 30 / 149,99 TRY`** |

✅ Finance all tabs show **₺ (TRY)**, consistent with Offers/SR — no "USD". ✅ CargoDry tiles/tier/commission show
**TRY**. ✅ **Amounts unchanged in value** (only the currency label corrected); economics math untouched. ✅ The whole
provider app now reads **one consistent settlement currency (TRY)**. No console errors introduced; tr + en unaffected
(labels already localized; the currency is data-driven).

## Scope

Backend `Modules/CargoDry` only (14 source files, `"USD"` → `"TRY"`) + a dev-DB relabel + a `cargodry-api` rebuild for
verification. Payment/other modules, the BFF, and the FE are byte-for-byte clean. **Not committed.**

Cross-link: `docs/V1.0.1/Provider/CargoDry/REPORT_X4_CURRENCY_CROSSLINK.md`.
