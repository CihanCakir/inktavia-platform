# REPORT — remove mock `/app/analytics` + enrich `/app/finance/financial-reporting` with real charts

**Repo:** `inktavia-marine-admin-web` (FE only). Two independent changes in one pass. No backend/BFF/provider-web/CargoDry
/Payment-logic changes; no new endpoints. `npm run typecheck` + `eslint` clean; tr+en parity for every new key. Verified
on-screen with a fresh admin login.

---

## PART A — deleted the mock analytics page, relocated CargoDry Analytics

### Why (necessity)
The `/app/analytics` **Fleet Analytics** tab was 100% mock: `useAnalyticsDashboard` hit `GET /admin/analytics/dashboard`,
which **does not exist (404)**, silently masked by a hardcoded `PLACEHOLDER` (fake fleet efficiency / fuel / utilization /
health / sectors) — that 404 was the reported "hata alınıyor". There is no fleet-telemetry backend and none is planned in
this MVP, so a "demo" badge would only preserve a live 404. The **only real** surface was the **CargoDry Analytics** tab
(`useCargoDryAnalytics` → real `GET /cargodry/analytics`), so it was relocated into the CargoDry menu.

### New page + wiring
- **New file** `src/pages/app/cargodry/CargoDryAnalyticsPage.tsx` — `PageHeader` + the CargoDry KPI row + the 4 real charts
  (status-distribution donut, daily-activations line, efficiency-buckets bar, product-mix horizontal bar), calling
  `useCargoDryAnalytics()` itself with the original loading-skeleton behavior. Recharts config, colors, and DTO usage kept
  exactly as-is; **all English labels localized** via the new `cargodryAnalytics` namespace.
- **`src/app/router/routes.tsx`** — removed `ANALYTICS: '/app/analytics'`; added
  `CARGODRY_ANALYTICS: '/app/cargodry/analytics'` beside the other `CARGODRY_*` constants.
- **`src/app/router/routeObjects.tsx`** — removed the `AnalyticsChartPage` import + `{ path: 'analytics', … }` route; added
  the eager `CargoDryAnalyticsPage` import + `{ path: 'cargodry/analytics', element: <CargoDryAnalyticsPage /> }`
  (matching the sibling CargoDry pages' eager style).
- **`src/app/layouts/DashboardLayout.tsx`** — removed the top-level `analytics` nav item (from the `system` group); added
  `{ key: 'cargodry-analytics', icon: 'analytics', labelKey: 'cargodryChildren.analytics', path: ROUTES.CARGODRY_ANALYTICS }`
  right after `cargodry-overview` in the collapsible CargoDry parent.

### Deleted (dead after A1–A2; grep-confirmed zero references first)
`src/pages/app/analytics/AnalyticsChartPage.tsx`, `.../components/FleetEfficiencyChart.tsx`,
`.../FuelConsumptionChart.tsx`, `.../VesselUtilizationChart.tsx`, `.../SystemHealthChart.tsx`,
`.../FleetDistributionPlaceholder.tsx`, `.../CargoDryAnalyticsTab.tsx` (content moved), and
`src/shared/api/hooks/useAnalytics.ts` (the mock hook + `PLACEHOLDER` + `AnalyticsDashboardDto` + fleet interfaces). The
now-empty `src/pages/app/analytics/` (and `components/`) directory was removed. Post-delete repo-wide grep for
`AnalyticsChartPage`, `useAnalyticsDashboard`, `AnalyticsDashboardDto`, `ROUTES.ANALYTICS`, `/app/analytics`, and every
fleet-chart name → **zero remaining references**.

### i18n (A4)
- **`navigation.json` (tr+en):** removed the top-level `analytics` label; added `cargodryChildren.analytics`
  (tr `"Analitik"`, en `"Analytics"`).
- **New namespace `cargodryAnalytics.json` (tr+en)** — page `title`/`subtitle`, `kpi.{activeKits,avgEfficiency,renewalRate,
  expiring30d}`, `charts.{statusDistribution,dailyActivations,efficiencyDistribution,productMix}.{title,subtitle}`, and
  `series.{activations,renewals,kits,active,total}`. Registered in `namespaces.ts` (`CARGODRY_ANALYTICS: 'cargodryAnalytics'`)
  and imported/added to both resource bundles in `i18n.ts`, exactly like the other page namespaces.

---

## PART B — financial-reporting: real charts + locale fix

**File:** `src/pages/app/finance/FinancialReportingDashboardPage.tsx`. Reused the **existing** `@shared/ui/charts/Charts`
primitives (`DonutChartCard`, `BarChartCard`, `LineChartCard`) — no new chart components.

### B1 — locale
- `money()`: `toLocaleString('en-US', …)` → `'tr-TR'` (amounts now read `TRY 1.234,56`).
- Ledger `occurredAtUtc` cell: `toLocaleDateString('en-GB', …)` → `'tr-TR'` (dates read `27 Tem 2026`).
- `ledger.showing` total: `total.toLocaleString()` → `total.toLocaleString('tr-TR')`.

### B2 — composition donut
`DonutChartCard` fed from summary totals: `[{revenue}, {expense}]` → titled "Gelir / Gider Kompozisyonu".

### B3 — breakdown-by-account-line bar
`BarChartCard` over `[...revenueLines, ...expenseLines].map(b => ({ name: tAccountLine(b.accountLine), amount: b.total }))`
→ "Hesap Satırı Bazlı Dağılım". Single series (the shared `BarChartCard` applies one fill per series across bars; per-bar
color-by-nature would require forking the shared component, so kept simple as the spec allowed). This **replaced** the old
single revenue/expense/net bar (the donut now covers that comparison); the **breakdown list `BreakdownGroup` is kept**.

### B4 — net-contribution trend line (FE aggregation, no backend change)
Added a dedicated **bulk** `useLedgerEntriesQuery` (`pageSize: 500`, no `accountLine`/`sourceType`, same `from`/`to`/
`currency`) used **only** to build the trend — the paged drill-down query is untouched (distinct query key). Aggregation:
bucket by **day**, or by **ISO week (Monday)** when the range spans **> 60 days**; `net = Σ(Revenue) − Σ(Expense)` per
bucket, sorted ascending, `bucket` label formatted `tr-TR` short (`27 Tem`).

**Reversal-sign decision (verified against the type + the ledger table):** `LedgerEntryDto.amount` is **always positive**
(`// always positive; sign carried by isReversal + nature`), and the drill-down table renders `−money(amount)` when
`isReversal`. So the trend uses `signed = isReversal ? −amount : amount`, summing Revenue and Expense natures separately.
**On-screen this reconciled exactly:** for the default period the trend's single 27 Tem bucket read **−4.778**, matching the
Net KPI (revenue 7.222 − expense 12.000 = −4.778).

If a period has more than `TREND_PAGE_SIZE` (500) entries, the trend is built from the first page and a small note
(`charts.netTrend.partial`, "İlk 500 / N kayıt gösteriliyor …") is shown instead of crashing.
**Scale-phase upgrade (out of scope):** a backend `GET financial-summary/timeseries` endpoint would remove this bulk-fetch
heuristic and the 500-row cap entirely.

### B5 — layout
Hierarchy preserved: back-link → header → banner → filters → KPIs → memo cards → **charts row (composition donut + net
trend, `lg:grid-cols-2`)** → **breakdown row (list + account-line bar, `lg:grid-cols-2`)** → ledger drill-down. The
breakdown list still complements (not replaced by) the bar.

### B6 — i18n
Extended `financialReport.json` (tr+en) with a `charts` block: `charts.composition.{title,subtitle,revenue,expense}`,
`charts.byAccountLine.{title,subtitle,amount}`, `charts.netTrend.{title,subtitle,net,partial}` (Turkish primary, e.g.
`composition.title: "Gelir / Gider Kompozisyonu"`, `byAccountLine.title: "Hesap Satırı Bazlı Dağılım"`,
`netTrend.title: "Net Katkı Trendi"`).

---

## Quality gates
- `tsc --noEmit`: clean (whole project — confirms no dangling imports after the deletions).
- `eslint` on all changed/new files: clean (fixed one `react-hooks/exhaustive-deps` warning by reading `trendLedger?.items`
  inside the `useMemo` and depending on `trendLedger`).
- All touched JSON parses; tr/en parity for the new `cargodryAnalytics` namespace, `cargodryChildren.analytics`, and the
  `financialReport.charts` block.

## On-screen before/after (fresh admin login — `admin.user@inktavia.com`)
| Surface | Before | After (verified) |
|---|---|---|
| Sidebar / SİSTEM group | top-level **Analitik** item (→ mock page) | **no** Analitik top-level; visiting `/app/analytics` → **404** (`/not-found`). No `/admin/analytics/dashboard` request possible — the hook is deleted. |
| CargoDry menu | no analytics entry | **Analitik** child (after Kitler) → `/app/cargodry/analytics`; menu auto-expands + child highlighted. |
| `/app/cargodry/analytics` | (n/a — was a tab) | "CargoDry Analitik" + KPI row (Aktif Kitler 7, Ort. Verim 76,5%, Yenileme Oranı 10,0%, Süresi Dolan 2) + 4 charts, all Turkish, from real `/cargodry/analytics`. |
| `/app/finance/financial-reporting` | one bar chart; `en-US`/`en-GB` locale | **donut** (Gelir/Gider) + **account-line bar** + **net-trend line**; amounts `TRY 7.222,00`, dates `27 Tem`; trend point −4.778 reconciles with the Net KPI; breakdown list retained; clean empty states when a period has no data. |

Empty tables/charts elsewhere remain legitimate empty states, not defects.
