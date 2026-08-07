# Admin QA — Finance / Packages / Commerce / Reports (A-QA11)

Pages: `src/pages/app/finance/*`, `PackagesPage.tsx`, `OrdersListPage.tsx`, `ReportsPage.tsx`.

## Static + live findings
**Finance = gold standard.** All 5 sub-reports + overview are **REAL, wired, TRY (`tr-TR`), fully i18n'd** (`financialReport`
ns). Live-confirmed at `/app/finance/financial-reporting`: TR labels, currency=TRY, read-only ledger note (KDV/VAT +
provider-funded discount shown as separate memos, excluded from net — matches §15/P12), real ledger drill-down. Ledger uses
`ledgerEnumMaps` for the one integer-enum endpoint. This is the reference pattern the other modules should follow. Minor:
- `FinancialReportingDashboardPage.tsx:27` currency filter `['TRY','USD','EUR']` — offering USD/EUR on a TRY marketplace can
  surface confusing/empty reports (default is TRY, acceptable but consider scoping to TRY).
- `CommissionRuleUsageReportPage.tsx:60-62` KPI totals sum **current page only**, not the full dataset — misleading.
- Reconciliation/invoice pages render `row.currencyCode`/`status` verbatim from BFF DTO (strings) — faithful; if stale USD
  ever appears it's backend/seed data, not an FE hardcode.

**Packages** `PackagesPage.tsx` — REAL (₺, `useProviderPlansQuery`/`useParticipantPlansQuery` + mutations). Issues:
- activate/deactivate mutations have **no `onError`** (`useSubscriptionPlansQuery.ts:97-137`) → **silent failure** on the
  known 400 for seed plans.
- Hardcoded strings: "Most Popular", "FREE"/"PAID", "Ücretsiz", "/ay".
- cargodry/commerce tabs are ComingSoon stubs.

**Commerce/Orders** `OrdersListPage.tsx` — **STUB** (18 lines): empty `<div/>` behind `FeatureFlag COMMERCE_MODULE`, English
hardcoded fallbacks, no api/hooks. Orphaned from nav.

**Reports** `ReportsPage.tsx` — REAL KPIs (`useReportKpi`/`useExportReport`/`useCargoDryStatsQuery`) but 8 `REPORT_DEFINITIONS`
labels/descriptions + CargoDry KPI labels are **hardcoded** and `reports.json` is a 2-key namespace → English fallback.

## Live walkthrough checklist
- [ ] All 5 finance reports load with correct ₺ figures vs snapshots; ledger drill-down maps enums.
- [ ] Packages activate/deactivate shows a toast on 400 (not silent); i18n strings translated.
- [ ] Commerce/Orders: built or hidden from routing.
- [ ] Reports: labels localized; KPIs + export work.

## Fix candidates
`FIX_A_QA11_FINANCE_POLISH` (USD/EUR filter scope, full-set KPI totals), `FIX_PACKAGES_ONERROR`, hide/build Commerce/Orders,
Reports i18n keys. **Gated:** VAT/KDV numbers need YMM sign-off.
