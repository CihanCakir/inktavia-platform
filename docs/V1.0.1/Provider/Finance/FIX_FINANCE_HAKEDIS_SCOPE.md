# FIX — Finance opens on a CargoDry-consignment "Settlements" tab; surface real marketplace earnings

> **Repo:** `inktavia-marine-provider-web` — FE-mostly. `/app/finance` **defaults to the Settlements (Hakedişler) tab,
> which is CargoDry-consignment-scoped** (empty + USD for a provider with no consignment), while the provider's real
> **service-request completion earnings** live one tab over. Reframe so Finance opens on the provider's actual earnings.
> Coordinate with **X4** (currency). Additive; no economics change. **Do not commit** until reviewed.

## What's known (static)
Finance has 6 tabs: `settlements | payouts | transactions | invoices | subscription | profile` (default = **settlements**).
- **SettlementsTab** = **CargoDry consignment**: `useEarningsTrend` + `useProductPerformance` call **`cargodryApi`**, the
  header reads "CargoDry konsinye hakedişlerin", amounts in **USD**. For a provider with no consignment this is an empty,
  USD, CargoDry-specific view — yet it's the **first thing "Finance" shows**.
- The provider's **marketplace earnings** are elsewhere: **Transactions (İşlemler)** carries the BE-P8/S8 "Kazanç kırılımı"
  economics breakdown + BE-P10 refund/clawback (in ₺), and **Payouts** carries the payout summary/status. These are the
  real service-request earnings.

## Fix
1. **Reframe the Settlements tab as CargoDry-specific:** label it clearly (e.g. "CargoDry Hakedişleri / CargoDry
   Settlements") so it isn't read as all-of-finance; keep its content. (Its USD → TRY is handled by **X4**.)
2. **Default landing tab = the provider's real earnings:** change the initial `tab` from `settlements` to **`payouts`**
   (or a new lightweight **Overview**) so opening Finance shows marketplace payout/earnings state, not an empty CargoDry
   consignment view. If an Overview is preferred, compose it from `usePayoutSummary` + recent transactions (reuse existing
   queries — no new backend).
3. **Make service-request earnings prominent:** ensure the Transactions tab (economics breakdown) + Payouts are the primary
   earnings surfaces; the header subtitle per tab should describe each accurately (settlements = CargoDry; payouts/
   transactions = marketplace/service earnings).
4. Coordinate with **X4**: after X4, all marketplace figures are ₺; the CargoDry tab currency follows the X4 decision.

## Verify (on screen — localhost:3002/app/finance)
- [ ] Finance opens on the provider's real earnings (payouts/overview), not an empty USD CargoDry settlements view.
- [ ] The CargoDry settlements tab is clearly labelled as CargoDry-specific.
- [ ] Service-request completion earnings are visible (Transactions economics breakdown + Payouts) with correct ₺ (post-X4).
- [ ] No regression to the 6 tabs; loading/empty/error intact.

## Report
`docs/V1.0.1/Provider/Finance/REPORT_FIX_FINANCE_HAKEDIS_SCOPE.md`: the default-tab change + CargoDry-settlements relabel,
how SR earnings are surfaced, and the on-screen confirmation. Cross-link X4.
