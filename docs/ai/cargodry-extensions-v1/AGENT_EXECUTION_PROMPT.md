# CargoDry Extensions v1 — Agent Execution Guide

## Package Contents

| File | Purpose |
|---|---|
| `PROMPT_1_CARGODRY_USAGE_REPORT.md` | Kit usage report: backend query + export, BFF endpoint, frontend ReportsPage extension |
| `PROMPT_2_CARGODRY_ANALYTICS.md` | Analytics: per-kit metrics endpoint, useCargoDryAnalytics hook, AnalyticsChartPage CargoDry tab |
| `PROMPT_3_NOTIFICATION_BACKEND.md` | Full notification backend (references notification-module-v1 prompts) + CargoDry consumers + frontend inbox |

---

## Prerequisites

Before executing any prompt in this package, the following must already be complete:

- [ ] `cargodry-module-v1` fully deployed (PROMPT_A through PROMPT_E executed)
  - CargoDry backend running: `/cargodry/kits`, `/cargodry/stats`, `/cargodry/batches/generate`
  - BFF endpoints live: `/api/v1/admin-panel/cargodry/kits`, `/cargodry/stats`
  - Frontend `CargoDryListPage`, `CargoDryStatsBar`, `CargoDryKitDetailDrawer`, `CargoDryBatchGenerateModal` in place
- [ ] `notification-module-v1` directory available with PROMPT_A/B/C (for PROMPT_3)

---

## Execution Order

The 3 prompts are **independent** of each other (no cross-dependencies). Execute them in any order, or in parallel with separate agents.

### Recommended order for a single agent:

```
PROMPT_1 → PROMPT_2 → PROMPT_3
```

PROMPT_3 is the heaviest (backend + frontend). Run it last so PROMPT_1 and PROMPT_2 are available for testing first.

---

## Per-Prompt Key Facts

### PROMPT 1 — CargoDry Usage Report

**New files:**
- `Aizen.Modules.CargoDry.Abstraction/Dto/CargoDryKitUsageReportDto.cs`
- `Aizen.Modules.CargoDry.Application/Queries/GetCargoDryUsageReport/` (Query + Handler)
- `Aizen.AdminPanel.BFF.CargoDry/Dto/CargoDryReportBffDto.cs`
- `Aizen.AdminPanel.BFF.CargoDry/Queries/GetCargoDryUsageReport/` (Query + Handler)
- `src/features/cargodry/hooks/useCargoDryUsageReport.ts`

**Modified files:**
- `ICargoDryKitRepository` — add `GetAllForReportAsync`
- `ICargoDryBatchRepository` — add `GetAllForReportAsync`
- `CargoDryAdminController` — add 2 endpoints
- `IAdminCargoDryBffRemoteCall` — add 2 Refit methods
- `AdminCargoDryController` (BFF) — add 2 endpoints
- `src/shared/api/types/report.types.ts` — add `cargodry-kit-usage` union type + new DTOs
- `src/shared/api/endpoints.ts` — add `CARGODRY_REPORT_USAGE`, `CARGODRY_REPORT_USAGE_EXPORT`
- `src/pages/app/ReportsPage.tsx` — add CargoDry report card + CargoDry KPI row

**Critical:** EfficiencyPercent is computed in the handler on-the-fly. It is NOT a DB column. Never add it to EF entity config.

---

### PROMPT 2 — CargoDry Analytics

**New files:**
- `Aizen.Modules.CargoDry.Abstraction/Dto/CargoDryAnalyticsDto.cs`
- `Aizen.Modules.CargoDry.Application/Queries/GetCargoDryAnalytics/` (Query + Handler)
- `Aizen.AdminPanel.BFF.CargoDry/Dto/CargoDryAnalyticsBffDto.cs`
- `Aizen.AdminPanel.BFF.CargoDry/Queries/GetCargoDryAnalytics/` (Query + Handler)
- `src/features/cargodry/hooks/useCargoDryAnalytics.ts`
- `src/pages/app/analytics/components/CargoDryAnalyticsTab.tsx`

**Modified files:**
- `ICargoDryKitRepository` — add `GetAllAsync`
- `CargoDryAdminController` — add analytics endpoint
- `IAdminCargoDryBffRemoteCall` — add Refit method
- `AdminCargoDryController` (BFF) — add analytics endpoint
- `src/entities/cargodry/types/cargodry.types.ts` — append analytics DTOs
- `src/shared/api/endpoints.ts` — add `CARGODRY_ANALYTICS`
- `src/pages/app/analytics/AnalyticsChartPage.tsx` — add tab switcher + CargoDryAnalyticsTab

**Critical:** Do NOT merge into `useAnalyticsDashboard` or `AnalyticsDashboardDto`. CargoDry analytics is a fully separate hook and endpoint.

---

### PROMPT 3 — Notification Backend + Frontend Inbox

**References:** `notification-module-v1/PROMPT_A`, `PROMPT_B`, `PROMPT_C` (execute those first)

**New files (backend):**
- 5 CargoDry consumer implementations (activated, expiring, expired, renewed, revoked)
- 5 notification template seed entries
- `INotificationBffRemoteCall` Refit interface
- BFF `NotificationsController`

**New files (frontend):**
- `src/shared/api/types/notification.types.ts` — extended
- `src/features/notifications/hooks/useNotificationsQuery.ts`
- `src/features/notifications/hooks/useNotificationUnreadCount.ts`
- `src/features/notifications/hooks/useNotificationHub.ts`
- `src/features/notifications/hooks/useMarkNotificationRead.ts`
- `src/shared/ui/notification-bell/NotificationBell.tsx`
- `src/pages/app/NotificationsInboxPage.tsx`

**Modified files (frontend):**
- `src/shared/api/endpoints.ts` — add NOTIFICATIONS_* endpoints
- `src/shared/api/queryKeys.ts` — add notifications key group
- `src/router/routes.ts` — add `/notifications` route
- `src/router/routeObjects.tsx` — add nav entry
- App header component — import and render `NotificationBell`
- Authenticated layout — call `useNotificationHub(userId)`

**Critical:** SignalR auth uses `accessTokenFactory: () => localStorage.getItem('aizen_access_token')`. This must match the key used by the existing `@microsoft/signalr` messaging hub pattern (already installed).

---

## Common Failure Points

| Problem | Fix |
|---|---|
| `GetAllAsync` / `GetAllForReportAsync` not on repo interface | Add to `ICargoDryKitRepository` in Abstraction project first, then implement |
| EfficiencyPercent in EF config | Remove — it is `[NotMapped]` / not in config |
| `useAnalyticsDashboard` broken after changes | You must NOT modify that hook — CargoDry analytics is in `useCargoDryAnalytics` |
| SignalR hub 401 | Check `accessTokenFactory` key matches auth token storage key |
| `tsc --noEmit` errors on new types | Run after each prompt, not at the end |
| Recharts import errors | Do not `npm install recharts` — already installed; just import from `'recharts'` |
| BFF envelope missing | All BFF controllers must return `AizenResponse.Success(data)` |

---

## Test Checklist (run after all 3 prompts)

**PROMPT 1:**
- `GET /api/v1/admin-panel/cargodry/reports/usage` → 200 with `byProduct`, `byBatch`, `dailyActivations`
- `GET /api/v1/admin-panel/cargodry/reports/usage/export?format=csv` → 200 CSV file download
- `ReportsPage` → "CargoDry Kit Usage" card visible with CSV + XLSX buttons

**PROMPT 2:**
- `GET /api/v1/admin-panel/cargodry/analytics` → 200 with `statusDistribution` array having `color` field
- `AnalyticsChartPage` → tab switcher visible; "CargoDry Analytics" tab renders 4 charts
- No console errors from Recharts

**PROMPT 3:**
- Backend: notification created in DB after kit activation
- `GET /api/v1/notifications` → returns list for authenticated user
- `GET /api/v1/notifications/unread-count` → returns `{ unreadCount: N }`
- Frontend: notification bell visible in header with badge
- `/notifications` page loads with notification list
- Clicking unread notification marks it read and removes highlight
- SignalR: activating a kit triggers instant bell refresh (no page reload)
