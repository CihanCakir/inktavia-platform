# REPORT — FE_ADMIN nav cleanup: slim Payment dashboard + collapsible CargoDry menu

**Repo:** `inktavia-marine-admin-web` (FE only). Follow-up to the sidebar restructure. **Scope honored:** navigation +
removing redundant in-page nav sections only. No backend/BFF, provider-web, or CargoDry data/business logic touched. Stitch
design preserved. `npm run typecheck` + `eslint` clean.

## Part A — slimmed `PaymentDashboardPage`
`src/pages/app/payments/PaymentDashboardPage.tsx`: removed the redundant **sub-link tile wall** — it was the row of 12
`navigate(...)` buttons rendered in the `PageHeader` `actions` slot (PlatformFeeRules · ProfitProtection ·
CustomerDiscount · CustomerBenefitBudget · CommissionBenefitRules · CommissionBenefitEntitlements · ProviderPayouts ·
RefundQueue · ChargebackQueue · ProviderBalances · Sub-Merchant KYC · RefundAllocationPolicies). Every one of those
destinations is now a child of the **Ödemeler** sidebar menu (previous change), so the in-page grid was pure duplication.
- **Kept** all real content: the 4 **KPI cards** (Gross Volume, Commission, Payout Pending, Held in Escrow — all show live
  figures; their KPI-level click-throughs to transactions/commission-rules/payouts remain as shortcuts) and the **data
  cards** (Volume Trend, Status Distribution donut, Recent Transactions table, Pending Payouts). The page is now a compact
  landing (KPIs + data), no nav wall. No KPI card was pure-nav, so none were dropped.
- `PageHeader` now renders just title + subtitle.

## Part B — CargoDry: collapsible menu + removed in-page sub-links

### B.1 — Sidebar menu (`DashboardLayout.tsx` NAV_GROUPS)
Replaced the single `cargodry` link (in the `operations` group) with a **collapsible `cargodry` parent** (reusing the
Sidebar `children` capability) with 15 children, in order — all via existing `ROUTES.CARGODRY*` constants:
Kitler (`CARGODRY`) · Kit Listesi (`CARGODRY_KITS`) · Ürünler (`CARGODRY_PRODUCTS`) · Partiler (`CARGODRY_BATCHES`) ·
Yaşam Döngüsü Olayları (`CARGODRY_LIFECYCLE_EVENTS`) · Operasyonel Uyarılar (`CARGODRY_OPERATIONAL_ALERTS`) · QR Sorgu
(`CARGODRY_QR_LOOKUP`) · Yenileme Adayları (`CARGODRY_RENEWAL_CANDIDATES`) · Yenileme Hazırlıkları (`CARGODRY_RENEWALS`) ·
Fırsat Yönlendirme (`CARGODRY_OPPORTUNITY_ROUTING_PREVIEW`) · Ticari Panel (`CARGODRY_COMMERCIAL`) · Satış Atıfları
(`CARGODRY_COMMERCIAL_SALES_ATTRIBUTIONS`) · Settlement'lar (`CARGODRY_COMMERCIAL_SETTLEMENTS`) · Settlement Otomasyonu
(`CARGODRY_COMMERCIAL_SETTLEMENT_AUTOMATION`) · Kural Çözüm Önizleme (`CARGODRY_COMMERCIAL_RULES_RESOLVE_PREVIEW`).
- **One-level vs two-level:** shipped **one-level** (15 items), consistent with the Ödemeler menu; the longest-prefix
  active logic keeps the `Kitler` overview from lighting up on sub-routes, so the flat list reads cleanly. Two-level was
  optional; skipped to keep parity + low risk.
- **Stray item consolidation:** there was no separate top-level `cargodry-commercial` entry in NAV_GROUPS — the commercial
  pages are reached via the `Ticari Panel`/Satış Atıfları/Settlement'lar/… children, so nothing to consolidate.

### B.2 — Removed in-page sub-link sections (data kept)
- `src/pages/app/CargoDryListPage.tsx` (the `/app/cargodry` landing): removed the **"Navigation shortcuts"** `NavChip`
  grid (Kit Registry · Batch Registry · QR Lookup · Product Catalog · Lifecycle Events · Operational Alerts · Opportunity
  Routing · Renewal Candidates · Renewal Preparations + a Generate-Batch chip). All those destinations are now sidebar
  children; the generate-batch chip duplicated the header's "Add Kit" button. Removed the now-unused `NavChip` component.
  **Kept**: hero KPI cards (Inventory Health / Active Nodes / Sensor Arrays / Featured Hardware), the 7-metric stats bar,
  the overview-driven operational alert banner, the Recent Batch Activity table, and the analytics panels — all data
  untouched.
- `src/pages/app/cargodry/commercial/CargoDryCommercialDashboardPage.tsx`: removed the **"Commercial Sections"** `NavChip`
  grid (Sales Attributions · Sell-Through Settlements · Settlement Automation · Rule Resolution Preview) and the unused
  `NavChip` component. **Kept**: the 4 KPI cards and the Recent Settlements table (and its row/View-All navigation). No
  commercial data logic, queries, or calculations changed.

## Part C — i18n + icons
`src/shared/i18n/locales/{tr,en}/navigation.json`: added a `cargodryChildren` block (15 keys, mirroring
`paymentsChildren`/`financeChildren`), full tr/en parity. Each child has a Material-symbols icon consistent with the set
(`dashboard`, `list_alt`, `category`, `layers`, `history`, `warning`, `qr_code_scanner`, `pending_actions`, `autorenew`,
`route`, `storefront`, `attribution`, `receipt_long`, `play_circle`, `rule`).

## Extra fix — Sidebar collapsed-flyout active state (`Sidebar.tsx`)
While verifying the CargoDry collapsed-rail flyout, the flyout highlighted **both** the overview leaf (`Kitler`,
`/app/cargodry`) and the actual active child, because the flyout's active check used per-item `matchesPath` (exact-or-
descendant) instead of the longest-prefix `bestMatch` the inline list already uses. Fixed the flyout to compute
`bestMatch` once over its leaves and highlight only the most-specific match — so an overview item no longer lights up on
every sub-route. This is a small correctness polish to the reused capability; it improves the Ödemeler/Finans flyouts too.
No API/scope change.

## Quality gates
- `npm run typecheck` (tsc --noEmit): **clean**.
- `eslint` on all changed files (PaymentDashboardPage, CargoDryListPage, CargoDryCommercialDashboardPage, DashboardLayout,
  Sidebar): **clean**.
- No routes/pages removed; every removed in-page link already had a sidebar menu entry before its tile was deleted.

## On-screen verification (admin session)
1. **`/app/payments`** — compact landing: header with **no button tile wall**, 4 KPI cards + Volume Trend + Status
   Distribution + Recent Transactions + Pending Payouts all present. Every former tile target reachable from the Ödemeler
   menu.
2. **CargoDry menu** — now a collapsible sidebar parent. Deep link to `/app/cargodry` **auto-expanded** it with **Kitler**
   active and the parent accent-colored; all 15 children render.
3. **SPA nav** — clicking **Ticari Panel** navigated in-app to `/app/cargodry/commercial`; the CargoDry menu stayed open
   and Ticari Panel became the active child.
4. **CargoDry landing** (`/app/cargodry`) — the NavChip shortcut grid is gone; KPI hero row, stats bar, inventory table,
   and analytics panels retained.
5. **Commercial dashboard** (`/app/cargodry/commercial`) — the "Commercial Sections" grid is gone; KPI cards + Recent
   Settlements table retained.
6. **Collapsed rail flyout** — collapsing the sidebar and hovering the CargoDry icon shows the fixed flyout ("CARGODRY"
   header + all 15 children in full labels), with **only Ticari Panel** highlighted after the active-state fix.
7. Existing top-level items and the Ödemeler/Finans menus from the previous change unchanged. Mobile drawer uses the same
   expanded `<Sidebar>` and inherits the collapsible behavior.
