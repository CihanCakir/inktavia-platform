# FE_ADMIN_P12 — financial reporting dashboard (FINALE: BFF passthrough + FE dashboard)

> **The crown of the P1–P12 wave.** An admin dashboard that aggregates the whole marketplace economy over a period:
> revenue, expense, and **NetMarketplaceContribution**, from the append-only financial ledger (BE-P12). The **module is
> done** (ledger + posting service + summary/drill-down endpoints) — this is a **vertical slice**: add the BFF
> passthrough, then build the dashboard. **Accounting fidelity is the whole point — do NOT lump VAT liability or
> provider-funded discount into revenue/expense/contribution** (§19.17).
>
> **Do NOT touch** the Payment ledger/posting logic, provider-web, or CargoDry. Reuse the admin Stitch design + the
> existing reports section + the ops-queues `enumMaps`/`DataTable`. Enum fields are int/string on the wire → labeled maps.

## Ground truth (confirmed)
**Module (DONE) — `PaymentFinanceController` (`api/v1/payment/finance`):**
- `GET reports/financial-summary?from&to&currency` → `FinancialSummaryReportDto` {
  `From`, `To`, `Currency`,
  `RevenueTotal` (net of reversals, **EXCLUDES VAT liability**),
  `ExpenseTotal` (payment/refund/chargeback/**platform-funded**-discount + expected/actual),
  `NetMarketplaceContribution` (= RevenueTotal − ExpenseTotal),
  `VatLiabilityTotal` (**separate — NOT revenue**),
  `ProviderFundedDiscountTotal` (**separate memo — NOT an Inktavia expense**),
  `Breakdown: List<LedgerLineBreakdownDto { AccountLine: LedgerAccountLine, Total (net of reversals), … }>` }.
- `GET reports/ledger-entries?…` → **paged audit drill-down** over the append-only ledger (per-entry rows).
- (Existing: `reports/invoice-statement` + export — leave as-is.)
`LedgerAccountLine` enum = the §15 (15) + §19.17 (13) account lines (revenue vs expense nature); **do not merge lines**.

**BFF (to add):** admin passthrough for the two P12 reports.
**admin-web:** a reports section already exists — `pages/app/ReportsPage.tsx`, `pages/app/finance/*ReportPage.tsx`,
`shared/api/hooks/useReports.ts`, `shared/api/types/report.types.ts`, `reports.json` (tr+en). Add the P12 dashboard
alongside these (same section/nav).

## Part 1 — BFF (AdminPanel) passthrough
- `IAdminPaymentBffRemoteCall` (or the admin finance remote-call): add
  `[AizenRemoteCallGet("/api/v1/payment/finance/reports/financial-summary")] GetFinancialSummaryAsync(from,to,currency)`
  and `[AizenRemoteCallGet(".../reports/ledger-entries")] GetLedgerEntriesAsync(paged filters)`.
- BFF query handlers + typed result DTOs (reuse the module Abstraction DTOs directly if exposed, else typed mirrors —
  `FinancialSummaryReportDto`, `LedgerLineBreakdownDto`, a ledger-entry item + paged wrapper).
- `AdminPaymentController` (or an AdminFinanceController): `GET payment/finance/reports/financial-summary`,
  `GET payment/finance/reports/ledger-entries`, `[Authorize(Policy="AdminPanelAccess")]`, `AizenApiResponse` envelope.
  Build 0; no module changes.

## Part 2 — FE dashboard (admin-web)
`FinancialReportingDashboardPage` (in the finance/reports section; route + nav). Endpoints + `paymentApi`/hooks +
types from the BFF DTOs.
- **Filters:** period (from/to date range — default a sensible current period) + currency. Drive both report calls.
- **Headline KPIs:** three cards — **Net Marketplace Katkısı (`NetMarketplaceContribution`)** as the hero (gold),
  **Toplam Gelir (`RevenueTotal`)**, **Toplam Gider (`ExpenseTotal`)**. Formula shown: Katkı = Gelir − Gider.
- **Two separate memo figures (accounting fidelity — clearly OUTSIDE the contribution):**
  **KDV Yükümlülüğü (`VatLiabilityTotal`)** with a "gelir değildir / ayrı yükümlülük" note, and **Sağlayıcı-Finanslı
  İndirim (`ProviderFundedDiscountTotal`)** with a "Inktavia gideri değildir" note. Do NOT add these into revenue/
  expense/contribution — show them as informational.
- **Breakdown:** the `Breakdown` list grouped by nature — **Gelir kalemleri** (commission / platform-fee / subscription /
  premium …) and **Gider kalemleri** (payment-processing / refund / chargeback / platform-funded-discount / expected …),
  each `AccountLine` → labeled (map `LedgerAccountLine` to tr+en labels; reuse/extend the ops-queues `enumMaps`
  discipline) + its `Total`. A revenue-vs-expense chart (recharts, already used in the reports section) is welcome.
- **Ledger drill-down:** a paged audit table (reuse `DataTable`/`QueueTable`) over `ledger-entries` — per-entry
  AccountLine (labeled), amount, reversal flag, source ref, timestamp — so an admin can trace any figure to its
  immutable ledger rows. Filters (account line, date) as the endpoint supports.
- i18n tr+en full parity (KPIs, memos, every account-line label, chart legends).

## Don't-break / QA
- Reuse the reports section patterns + Stitch; envelope-tolerant (exact DTO field names above); labeled enums, no raw
  ints. `npm run typecheck` clean; module + BFF build 0; existing reports/finance screens unregressed;
  provider-web/CargoDry git-clean.
- **Fidelity guardrail:** RevenueTotal already excludes VAT; contribution = Revenue − Expense; VAT liability +
  provider-funded discount are separate memos. The FE must present them that way and never recompute a total.

## Verification (on-screen; keycloak-init ran, fresh admin login admin.user@inktavia.com)
Open the Financial Reporting dashboard → pick a period/currency covering the seeded activity (the Wave-B/P11 seeds +
accepted-offer economics + refunds posted ledger entries). Confirm the KPIs (Net Contribution = Revenue − Expense),
the VAT-liability + provider-funded-discount memos rendered separately, the revenue/expense account-line breakdown with
labels + a chart, and the ledger drill-down listing immutable entries that reconcile to the summary. typecheck/build clean.

## Report
`docs/V1.0.1/Payment/REPORT_FE_ADMIN_P12.md`: BFF endpoints added, the dashboard (KPIs + memos + breakdown + drill-down +
chart), the `LedgerAccountLine` label map, the fidelity handling (VAT/provider-funded-discount kept out of contribution),
and the on-screen transcript. **This completes the P1–P12 FE wave** (admin rule-CRUD + conflict fix + provider Wave B +
ops queues + cleanup + P11 boost + keyed-DI fix + P12 reporting). Note any remaining backlog (e.g. the P9 live iyzico
sandbox gate needs real keys; the boost confirm-modal gateway-aware copy).
