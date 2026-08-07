# FE_S12 — admin maintenance-schedule management UI

> **Repos:** `inktavia-marine-admin-web` + AdminPanel BFF (`addesso-project/Bff/src/AdminPanel`). Surfaces **S12** (periodic
> maintenance schedules): an admin page to create/list/edit per-vessel, per-category maintenance schedules that drive the
> N2 reminder. Backend is **ready** (`api/v1/admin/maintenance-schedules` POST upsert + GET list). All **additive**; tr + en.
> **Do not commit** until the user says.

## Current state (investigated)
- Module `MaintenanceScheduleController` at **`api/v1/admin/maintenance-schedules`**: **POST** (upsert,
  `UpsertMaintenanceScheduleRequest` → `UpsertMaintenanceScheduleResponse`) + **GET** (list →
  `GetMaintenanceScheduleListResponse` of `MaintenanceScheduleDto`). **No BFF passthrough yet.**
- `MaintenanceScheduleDto` carries the schedule state (vessel, category [+ type], `RecommendedIntervalMonths`,
  `ReminderLeadDays`, `LastPerformedAt`, `NextDueAt`, `ReminderSentAt`, `IsActive`, …) — **read the DTO/request in source and
  mirror the exact field names/shape.**
- admin-web has the recent SR feature pattern (`features/service-requests/hooks/useDisputeListQuery.ts` etc.) + the
  collapsible **Service Requests** submenu (disputes child) to extend. Vessel + service-category pickers already exist
  elsewhere (reference-data / SR filters) — reuse them.

## Part 1 — AdminPanel BFF passthrough
- Add, under the BFF's **ServiceRequests** feature (MarineProvider convention — per-command/query subfolders, `…Bff`
  suffix, `I<Domain>RemoteCall`): an **`UpsertMaintenanceScheduleBff`** command + a **`GetMaintenanceScheduleListBff`** query
  wrapping the module POST/GET. Typed, envelope-correct (`AizenApiResponse<T>`), concrete DTOs mirroring the module
  request/response. Wire onto the `IServiceRequestRemoteCall`. Expose on the admin ServiceRequests controller (or a small
  MaintenanceSchedules controller) under the admin-panel route base — keep the URL consistent with the other admin SR
  routes.

## Part 2 — admin-web page
- **Nav:** add a **Maintenance Schedules / Bakım Programları** child under the **Service Requests** collapsible submenu
  (next to Disputes), icon e.g. `event_repeat` / `build`. Route e.g. `ROUTES.MAINTENANCE_SCHEDULES = '/app/maintenance-schedules'`.
- **List page:** table of schedules — vessel, category (+ type), interval (months), lead (days), **Last performed**,
  **Next due** (highlight overdue / due-soon), **Reminder sent**, active badge. Optional filters (vessel, active). Follow
  the dispute-list page pattern + the BFF numeric-enum gotcha (use a mapper if any enum is numeric-coded).
- **Create/edit drawer** (upsert): **vessel picker** + **service-category picker** (+ optional service-type),
  `RecommendedIntervalMonths`, `ReminderLeadDays`, active toggle, notes. Client-validate (interval ≥ 1, lead ≥ 0, lead <
  interval window sensibly); the config defaults (from `MaintenanceScheduleOptions`) can prefill. On save → upsert (the
  backend is idempotent by vessel+category[+type]); surface the duplicate-active / business-error envelope verbatim; refresh
  the list; show the computed `NextDueAt` after save.
- Display-only note: `NextDueAt`/`LastPerformedAt` advance automatically when a matching service completes (S12 backend) —
  the admin sets the interval/lead, the system tracks the cycle.

## Don't-break / QA
- Additive: BFF passthrough + one admin page + a nav child. Existing SR pages, disputes, and nav unchanged otherwise.
  Server owns the schedule state (NextDueAt computed server-side; the FE never computes it). tr + en for every new string;
  admin-web tsc + eslint clean; BFF builds clean + envelope-correct.

## Verify (on screen)
1. The Service Requests submenu shows a **Maintenance Schedules** child → opens the list.
2. Create a schedule (vessel + category + interval 12 + lead 14) → saves, appears in the list with a computed **Next due**;
   editing the interval updates it; the active toggle works.
3. An upsert for the same vessel+category updates the existing schedule (no duplicate); a business error surfaces cleanly.
4. Overdue / due-soon rows are visually distinct; existing SR/dispute pages unaffected.

## Report
`docs/V1.0.1/ServiceRequest/REPORT_FE_S12.md`: the BFF passthrough (upsert + list), the admin page (list + upsert drawer +
nav child), the pickers reused, i18n keys, and the on-screen check. Note owner self-service (create their own vessel
schedules) as an owner-app follow-up. **Do NOT commit.**
