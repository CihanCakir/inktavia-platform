# REPORT — Finance opened on a CargoDry-consignment "Settlements" tab; surface real marketplace earnings

> Executes `FIX_FINANCE_HAKEDIS_SCOPE.md`. `inktavia-marine-provider-web` — FE-only, additive, no economics change.
> `/app/finance` defaulted to the **Settlements (Hakedişler)** tab, which is **CargoDry-consignment-scoped** (empty
> for a provider with no consignment), while the provider's real **service-request earnings** live in Transactions +
> Payouts. Reframed so Finance opens on real earnings. Coordinated with **X4** (currency). **Not committed.**

---

## Confirmed (static)

The Settlements tab is entirely CargoDry-scoped: `usePayoutSummary()` → `financeApi.getPayoutSummary()` →
`endpoints.cargodry.settlementsSummary` (`/cargodry/settlements/summary`), the list → `useSettlements()` →
`/cargodry/settlements`, and the charts → `useEarningsTrend`/`useProductPerformance` → `cargodryApi`. So the *first
thing "Finance" showed* was an empty, CargoDry-consignment view. The provider's marketplace earnings are in
**Transactions** (BE-P8/S8 economics breakdown + BE-P10 refund/clawback, ₺) and **Payouts** (disbursement summary +
negative-balance strip, ₺).

## Changes (`FinancePage.tsx` + `finance.json` en/tr + one X4 follow-up)

1. **Default landing tab: `settlements` → `payouts`.** Finance now opens on the provider's real disbursements/earnings.
2. **Tab order reframed** (order = prominence): `payouts · transactions · settlements · invoices · subscription ·
   profile` — the real marketplace/service-request earnings (payouts + the transactions economics breakdown) lead;
   CargoDry consignment settlements is a secondary, product-specific surface (3rd).
3. **Relabelled the settlements tab as CargoDry-specific:** `tab.settlements` → **"CargoDry Settlements" /
   "CargoDry Hakedişleri"** (was the generic "Settlements / Hakedişler"). Its content is unchanged.
4. **Per-tab subtitles made accurate:**
   - **payouts** → "Disbursements sent to your account — service-request earnings and CargoDry settlements — and
     their status." / "Hesabına yapılan ödemeler — servis işi kazançların ve CargoDry hakedişlerin — ve durumları."
   - **transactions** → "…breakdown of the **service-request** payments you received." / "Aldığın **servis işi**
     ödemelerinin brüt, komisyon ve net kırılımı."
   - **settlements** subtitle kept CargoDry-specific ("Your CargoDry consignment payouts and settlement status.").
5. **X4 coordination** — removed the last 3 stale FE `?? 'USD'` currency fallbacks (they only bit on empty data,
   producing a residual USD flash): `FinancePage` SettlementsTab `cur` + SettlementCharts `cur`, and
   `CargoDryRenewalsPage` `cur` → `?? 'TRY'`. Backend already returns TRY (X4); this closes the FE side.

`tsc --noEmit` **0 errors**; `finance.json` en/tr **parity 247/247**; no residual `'USD'` literal in
`features/finance` or `features/cargodry`. Lint: the 5 `FinancePage` + 1 `CargoDryRenewalsPage` eslint errors are
**pre-existing** (set-state-in-effect, an `offset` reassignment) — verified present at HEAD; my change adds **none**
(5 → 5). They are unrelated lint debt, out of scope for this additive fix.

## Verify (on screen — localhost:3002/app/finance, PROVIDER 2 AS, tr)

- ✅ **Opens on real earnings, not empty CargoDry USD.** Finance lands on **Ödemeler (Payouts)** showing BEKLEYEN
  **60 TRY**, İŞLENİYOR 0 TRY, ÖDENEN **40 TRY**, NEGATİF BAKİYE **10.560 TRY** — all ₺. Subtitle: "…servis işi
  kazançların ve CargoDry hakedişlerin…".
- ✅ **CargoDry tab clearly labelled.** The 3rd tab reads **"CargoDry Hakedişleri"**; its subtitle is CargoDry-specific
  ("CargoDry konsinye hakedişlerin…"); it shows 0 TRY (post-X4) and its intact empty state ("Henüz hakediş yok") —
  the empty view that should never have been the landing page.
- ✅ **SR completion earnings visible with ₺.** **İşlemler** shows the economics breakdown — TOPLAM BRÜT
  **90.560 TRY**, KOMİSYON **11.299,2 TRY**, NET HAKEDİŞ **77.000,96 TRY**, per-row gross/commission/net + BE-P10
  refund (İade edildi / İtirazlı) — subtitle "Aldığın servis işi ödemelerinin… kırılımı." Payouts rows are the same
  service/CargoDry disbursements in ₺.
- ✅ **6 tabs, no regression.** `Ödemeler | İşlemler | CargoDry Hakedişleri | Faturalar | Abonelik | Ödeme Profili`;
  the tab render switch and each tab's loading/empty/error paths are unchanged (only order/default/labels + the
  currency fallbacks changed). English relabel ("CargoDry Settlements", "Payouts") verified via the 247/247 parity.

## Scope & cross-link

`src/features/finance/pages/FinancePage.tsx` (default tab, tab order, 2 currency fallbacks),
`src/features/cargodry/pages/CargoDryRenewalsPage.tsx` (1 currency fallback), `en/tr finance.json` (label + 2
subtitles). No backend, no economics math. **Not committed.**

Currency correctness (USD → ₺) is delivered by **X4** — `docs/V1.0.1/Provider/Finance/REPORT_X4_CURRENCY.md` (this
fix removes the remaining FE `'USD'` fallbacks to complete that coordination).
