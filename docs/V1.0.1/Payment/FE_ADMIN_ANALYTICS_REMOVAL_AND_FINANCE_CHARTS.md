# FE_ADMIN — remove the mock `/app/analytics` page + enrich `/app/finance/financial-reporting` with real financial charts

> **Repo:** `inktavia-marine-admin-web` (FE only). Two independent changes in one pass.
>
> **Part A — remove `/app/analytics`.** Its **Fleet Analytics** tab is 100% mock: `useAnalyticsDashboard` calls
> `GET /admin/analytics/dashboard`, which **does not exist on the backend (404)**, and the failure is silently masked by a
> hardcoded `PLACEHOLDER` object (fake fleet efficiency / fuel split / vessel utilization / system health / fleet
> sectors). That 404 is the "hata alınıyor". The only **real** content on the page is the **CargoDry Analytics** tab
> (`useCargoDryAnalytics` → `GET /cargodry/analytics`, a real endpoint). Decision (owner-approved): **delete the analytics
> page entirely and relocate the real CargoDry Analytics into the CargoDry sidebar menu.**
>
> **Part B — financial-reporting charts.** `FinancialReportingDashboardPage` (P12) currently has only one bar chart. Add
> three real, data-driven financial charts (revenue/expense composition donut, breakdown-by-account-line bars, net
> contribution trend line) reusing the **existing** `@shared/ui/charts/Charts` primitives, and fix its number/date
> locale to `tr-TR`.
>
> **Do NOT touch** backend/BFF, provider-web, CargoDry data logic, or any Payment module code. No new backend endpoints.
> Keep the Stitch Marine-Luxury dark theme. `npm run typecheck` + lint clean; tr+en i18n parity for every new key.

---

## PART A — delete the mock analytics page, relocate CargoDry Analytics

### A1. Create the relocated CargoDry Analytics page (keep the real content)
The real, working content is the body of `src/pages/app/analytics/components/CargoDryAnalyticsTab.tsx` (KPI row +
donut/line/bar recharts, fed by `useCargoDryAnalytics()` → `/cargodry/analytics`). **Move this into a standalone CargoDry
page**:
- New file `src/pages/app/cargodry/CargoDryAnalyticsPage.tsx`:
  - Renders a `PageHeader` (title/subtitle from i18n — see A4) + the existing CargoDry analytics KPI row and 4 charts.
  - Calls `useCargoDryAnalytics()` itself (move the `isLoading`/`data` handling that lived in `AnalyticsChartPage` into
    this page), keeping the current loading-skeleton behavior.
  - **Localize the hardcoded English labels while relocating** (the app is Turkish): `Active Kits`, `Avg Efficiency`,
    `Renewal Rate`, `Expiring (30d)`, `Kit Status Distribution`/`Current snapshot`, `Daily Activations`/`Last 30 days`,
    `Efficiency Distribution`/`Active kits by remaining lifetime`, `Product Mix`/`Active kits per product line`, and the
    legend/series names (`Active`, `Total`, `Activations`, `Renewals`, `Kits`). Add these under a new `cargodryAnalytics`
    namespace (A4). Keep the recharts config, colors, and DTO usage exactly as-is.
- You may keep the chart JSX inline in the new page, or keep a `CargoDryAnalyticsTab`-style child component moved under
  `src/pages/app/cargodry/` — your call; just don't leave it under `pages/app/analytics/`.

### A2. Wire the new route + sidebar entry
- **`src/app/router/routes.tsx`:** remove `ANALYTICS: '/app/analytics'`; add
  `CARGODRY_ANALYTICS: '/app/cargodry/analytics'` (place it alongside the other `CARGODRY_*` route constants).
- **`src/app/router/routeObjects.tsx`:** remove the `AnalyticsChartPage` import (line ~16) and its route entry
  (`{ path: 'analytics', element: <AnalyticsChartPage /> }`, line ~301–302). Add a route for the new page:
  `{ path: 'cargodry/analytics', element: <CargoDryAnalyticsPage /> }` (import `CargoDryAnalyticsPage` from the new file).
  Match the existing lazy/eager import style used for the other CargoDry pages in this file.
- **`src/app/layouts/DashboardLayout.tsx` `NAV_GROUPS`:**
  - Remove the top-level analytics item (line ~110:
    `{ key: 'analytics', icon: 'analytics', labelKey: 'analytics', path: ROUTES.ANALYTICS }`).
  - Add a child to the **CargoDry** collapsible parent (the `children` array that currently ends at
    `cargodry-rules-resolve-preview`), e.g. right after `cargodry-overview`:
    `{ key: 'cargodry-analytics', icon: 'analytics', labelKey: 'cargodryChildren.analytics', path: ROUTES.CARGODRY_ANALYTICS }`.

### A3. Delete the dead mock code
Delete these files (all are analytics-page-only and become unreferenced after A1–A2):
- `src/pages/app/analytics/AnalyticsChartPage.tsx`
- `src/pages/app/analytics/components/FleetEfficiencyChart.tsx`
- `src/pages/app/analytics/components/FuelConsumptionChart.tsx`
- `src/pages/app/analytics/components/VesselUtilizationChart.tsx`
- `src/pages/app/analytics/components/SystemHealthChart.tsx`
- `src/pages/app/analytics/components/FleetDistributionPlaceholder.tsx`
- `src/pages/app/analytics/components/CargoDryAnalyticsTab.tsx` (its content moved to the new page in A1)
- `src/shared/api/hooks/useAnalytics.ts` (the mock hook + `PLACEHOLDER` + `AnalyticsDashboardDto` and the fleet
  interfaces). **First grep** `useAnalyticsDashboard` / `AnalyticsDashboardDto` / `FleetEfficiencyPoint` etc. repo-wide to
  confirm nothing else imports them before deleting.
- Remove the now-empty `src/pages/app/analytics/` directory (and `components/` under it).

### A4. i18n (tr + en)
- **`navigation.json` (tr+en):** remove the top-level `analytics` label; add `cargodryChildren.analytics`
  (tr `"Analitik"`, en `"Analytics"`).
- **New namespace `cargodryAnalytics.json` (tr+en)** (or extend an existing CargoDry namespace) with full parity for the
  page title/subtitle, KPI labels, chart titles/subtitles, and series/legend names listed in A1. Turkish primary
  (e.g. `activeKits: "Aktif Kitler"`, `avgEfficiency: "Ort. Verim"`, `renewalRate: "Yenileme Oranı"`,
  `expiring30d: "Süresi Dolan (30g)"`, `statusDistribution: "Kit Durum Dağılımı"`, `dailyActivations: "Günlük Aktivasyon"`,
  `efficiencyDistribution: "Verim Dağılımı"`, `productMix: "Ürün Karması"`, …). Register the namespace in the i18n setup
  the same way the other page namespaces are registered.

> **Necessity note (why full removal, not a "demo" badge):** there is no fleet-telemetry backend and none is planned in
> this MVP; keeping the mock only invites confusion and a live 404. CargoDry analytics is the sole real analytic surface,
> so it belongs in the CargoDry menu next to the rest of the CargoDry pages.

---

## PART B — financial-reporting: real charts + locale fix

**File:** `src/pages/app/finance/FinancialReportingDashboardPage.tsx`. **Reuse existing primitives** from
`@shared/ui/charts/Charts` — they already exist, no new chart components needed:
`DonutChartCard` (`data: {name,value}[]`), `BarChartCard` (`data, bars:[{dataKey,name,color}]`), `LineChartCard`
(`data, lines:[{dataKey,name,color}], xAxisKey`).

### B1. Fix number/date locale to `tr-TR`
- `money()` (line ~60–65): change `toLocaleString('en-US', …)` → `'tr-TR'` so amounts read `1.234,56` like the other
  finance pages.
- Ledger `occurredAtUtc` cell (line ~374): change `toLocaleDateString('en-GB', …)` → `'tr-TR'`.
- `ledger.showing` total + any other `toLocaleString()` without an explicit locale → pass `'tr-TR'`.

### B2. Revenue/Expense composition — Donut
Add a `DonutChartCard` next to the KPIs (or in the charts row, B5). Data straight from the summary totals:
```ts
const compositionData = summary
  ? [
      { name: t('charts.composition.revenue'), value: summary.revenueTotal },
      { name: t('charts.composition.expense'), value: summary.expenseTotal },
    ]
  : []
```
Render `<DonutChartCard title={t('charts.composition.title')} subtitle={t('charts.composition.subtitle')}
data={compositionData} emptyMessage={t('breakdown.empty')} />`. (Guard: only render when `summary` present, inside the
existing `summary` block.)

### B3. Breakdown by account line — Bar
Turn the `breakdown` array into a bar series (one bar per account line, labeled, colored by nature). Build the rows from
the already-computed `revenueLines` / `expenseLines`:
```ts
const accountLineData = [
  ...revenueLines.map((b) => ({ name: tAccountLine(b.accountLine), amount: b.total, nature: 'revenue' })),
  ...expenseLines.map((b) => ({ name: tAccountLine(b.accountLine), amount: b.total, nature: 'expense' })),
]
```
Render a `BarChartCard title={t('charts.byAccountLine.title')} subtitle={t('charts.byAccountLine.subtitle')}
data={accountLineData} bars={[{ dataKey: 'amount', name: t('charts.byAccountLine.amount') }]}
emptyMessage={t('breakdown.empty')}`. (If per-bar color by nature is easy with the shared component, use green for
revenue / amber for expense; if the shared `BarChartCard` only supports one fill, a single series is fine — keep it
simple, don't fork the shared component.) This replaces the existing single revenue/expense/net `BarChartCard` **or**
sits beside it — see B5.

### B4. Net marketplace contribution — Trend line (FE aggregation, no backend change)
The summary is a point-in-time total with no series; the **ledger** endpoint carries `occurredAtUtc`, `amount`, `nature`
per entry. Aggregate a net time series **client-side** for the selected period:
- Add a dedicated bulk query (reuse `useLedgerEntriesQuery`) with a large `pageSize` (e.g. `500`) and **no**
  `accountLine`/`sourceType` filters, scoped to the same `from`/`to`/`currency`, used **only** to build the trend (keep
  the existing paged ledger query for the drill-down table untouched).
- Bucket entries by day (or by ISO week if the range spans > 60 days) and compute
  `net = Σ(Revenue amounts) − Σ(Expense amounts)` per bucket, sorted ascending by date; carry a running cumulative if
  that reads better (label it "cumulative" in i18n if you do). Reversals (`isReversal`) already carry the correct sign in
  `amount`? **Verify**: if reversal amounts are stored positive with an `isReversal` flag, subtract them; if stored
  signed, sum directly. Match whatever the ledger table does so the trend reconciles with the totals.
- Render `<LineChartCard title={t('charts.netTrend.title')} subtitle={t('charts.netTrend.subtitle')} data={trendData}
  lines={[{ dataKey: 'net', name: t('charts.netTrend.net') }]} xAxisKey="bucket" emptyMessage={t('breakdown.empty')} />`.
- If the period has more entries than the bulk page size, show the existing empty/normal state gracefully (don't crash);
  a `> pageSize` note is acceptable. **Scale-phase upgrade (out of scope, note in report):** a proper backend
  `GET financial-summary/timeseries` endpoint would remove the bulk-fetch heuristic.

### B5. Layout
Keep the page's information hierarchy: back-link → header → fidelity banner → filters → **KPIs** → memo cards →
**charts section** → breakdown list → ledger drill-down. Put the three charts in a responsive grid (e.g. composition
donut + net trend on one row, account-line bar full-width or beside the breakdown list). Keep the existing breakdown
**list** (`BreakdownGroup`) — the bar chart complements it, doesn't replace it. Don't bloat vertical space; use
`lg:grid-cols-2`/`lg:grid-cols-3` like the rest of the page.

### B6. i18n (tr + en)
Extend `financialReport.json` (tr+en) with a `charts` block, full parity:
`charts.composition.{title,subtitle,revenue,expense}`, `charts.byAccountLine.{title,subtitle,amount}`,
`charts.netTrend.{title,subtitle,net}`. Turkish primary (e.g. `composition.title: "Gelir / Gider Kompozisyonu"`,
`byAccountLine.title: "Hesap Satırı Bazlı Dağılım"`, `netTrend.title: "Net Katkı Trendi"`).

---

## Don't-break / QA
- No backend/BFF/Payment/CargoDry-logic changes; no new endpoints. `useCargoDryAnalytics` + `/cargodry/analytics`
  unchanged. Ledger/summary queries unchanged except the **new** bulk trend query (read-only, same endpoint).
- After deleting the analytics files, **repo-wide grep** for `AnalyticsChartPage`, `useAnalyticsDashboard`,
  `ROUTES.ANALYTICS`, `/app/analytics`, `AnalyticsDashboardDto` → zero remaining references. No dead imports.
- Every former analytics destination is gone by design except CargoDry Analytics, which is reachable at
  `/app/cargodry/analytics` and from the CargoDry sidebar menu (active-child highlight + auto-expand on that route work,
  since it's a normal child of the existing collapsible parent).
- The P12 financial-reporting page still loads, filters, paginates, and its ledger table + CSV-adjacent behavior are
  unchanged; the three charts render from real summary/ledger data and show clean empty states when the period has no
  data. Numbers read `1.234,56`, dates `03 Ağu 2026` (tr-TR).
- `npm run typecheck` + lint clean; tr+en parity for all new keys.

## Verification (on-screen)
Fresh admin login.
1. Sidebar has **no** top-level "Analitik"; visiting `/app/analytics` no longer resolves (404/redirect per router
   default). **No** `/admin/analytics/dashboard` request is made anywhere (the mock hook is gone).
2. **CargoDry** menu shows an **Analitik** child → opens `/app/cargodry/analytics` → the real KPI row + 4 CargoDry charts
   render in Turkish from `/cargodry/analytics`.
3. `/app/finance/financial-reporting` shows the composition **donut**, account-line **bar**, and net-contribution
   **trend line**, all populated from the real summary/ledger for the selected period; amounts/dates are `tr-TR`; empty
   states are clean when a period has no data.

## Report
`docs/V1.0.1/Payment/REPORT_FE_ADMIN_ANALYTICS_REMOVAL_AND_FINANCE_CHARTS.md`: files deleted (analytics) + the new
CargoDry Analytics page/route/menu entry, the reversal-sign decision used for the trend aggregation, the i18n keys added
(tr+en), and an on-screen before/after for both surfaces. Note the backend time-series endpoint as a scale-phase upgrade.
