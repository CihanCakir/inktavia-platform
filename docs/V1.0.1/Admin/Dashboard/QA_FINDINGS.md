# Admin QA — Dashboard (A-QA1)

## Static + live findings
`src/pages/app/DashboardPage.tsx` is **~90% hardcoded mock** (live-confirmed at `/app/dashboard`).
- **Wired (real):** "Active Vessels" = `data?.totalVessels` (live 14), "Open Service Requests" = `data?.activeServiceRequests`
  (live 0) — via `useDashboardOverviewQuery` (`src/features/dashboard/api/dashboardApi.ts`).
- **Hardcoded + USD (🔴 X3):** "Pending Payouts **$124,500**" (line 122), "Critical Inventory **7**" (line 128).
- **Hardcoded consts:** `revenueData` (24-37, "Revenue & Commission Overview" chart), `serviceVolumeData` (39-45),
  `fleetStatusData` (47-52, "Vessel Fleet Status" renders empty live), `stockHealthData` (54-59), `RECENT_SERVICE_REQUESTS`
  (61-67, links to fake SR-1024… ids), `PENDING_APPROVALS` (69-75, **USD** $4,200/$12,000/$8,500), `RECENT_MESSAGES` (77-81).
- **i18n mismatch:** page calls `t('metrics.activeVessels')`/`t('metrics.openServiceRequests')` but `dashboard.json` only
  defines `metrics.totalVessels`/`activeServiceRequests`/`pendingServiceRequests` → silent English fallback. Chart/panel
  titles use no `t()`. Live: sidebar TR, dashboard cards English.
- BFF `DashboardOverview` already exposes `pendingReviews`, `pendingServiceRequests`, `recentActivity` — **unused**.
- Live console: SignalR negotiation error on load (see Messages/X7).

## Live walkthrough checklist
- [ ] Every metric card reflects real backend numbers (not $124,500 / 7 / fake lists).
- [ ] All money in **₺ TRY**; no `$`.
- [ ] Charts render real series (or are removed until wired); "Vessel Fleet Status" not empty.
- [ ] Recent-SR / approvals / messages lists link to real records.
- [ ] tr/en parity on all cards, titles, panels.

## Proposed cockpit (real, ₺)
Pending approvals count (ApprovalsQueue source), open disputes + SLA breaches (disputeEnums), refund + chargeback queue depth
(payments/queues), sub-merchant KYC backlog, pending provider-payout total **₺** (finance BFF), today's settled GMV +
commission **₺**, open vs overdue SRs, expiring CargoDry kits (`useCargoDryStatsQuery` already exists), recent real activity
(`recentActivity`).

## Fix candidates
`FIX_A_QA1_DASHBOARD_COCKPIT` — wire real KPIs + attention items in ₺, delete hardcoded consts, fix `metrics.*` i18n keys.
