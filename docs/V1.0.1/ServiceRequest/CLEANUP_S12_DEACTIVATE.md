# CLEANUP — S12 maintenance-schedule active toggle (deactivate/reactivate reachable)

> **Repo:** `addesso-project` — ServiceRequest module + AdminPanel BFF + admin-web. Closes the S12 FE follow-up: the entity
> has `Deactivate()`/`Reactivate()` but **no command/endpoint reaches them**, so the admin can't turn a schedule off, and a
> deactivated schedule may vanish from the list. Small, additive. **Do not commit** until the user says.

## Current state (investigated)
- `MaintenanceScheduleEntity` already has `IsActive`, **`Deactivate()`**, **`Reactivate()`** — reachable only from code, no
  API.
- `UpsertMaintenanceScheduleRequest` has **no `IsActive`** field; the upsert key is **(VesselId, ServiceCategoryCode,
  ServiceTypeCode) among active schedules** — so putting active-state into upsert is the wrong shape (a deactivated row with
  the same key would collide). A **dedicated set-active** action is the clean fit.
- `GetMaintenanceScheduleListQuery(vesselId)` — **check whether it filters to active-only**; if so, a deactivated schedule
  disappears (can't be reactivated from the UI). It must be able to return inactive rows too.

## Fix 1 — reachable deactivate/reactivate (backend)
- Add `SetMaintenanceScheduleActiveCommand(long scheduleId, bool isActive)` + handler → load the schedule, call
  `Reactivate()` or `Deactivate()`, persist. Not-found → clean business error. Idempotent (setting the same state is a
  no-op).
- Controller endpoint on `MaintenanceScheduleController`: `PUT /api/v1/admin/maintenance-schedules/{id}/active` with body
  `{ isActive: bool }` (or `POST .../{id}/deactivate` + `.../{id}/reactivate` — pick one, single set-active is simplest).
  `[ProducesResponseType]`, envelope-correct.
- **List must include inactive:** ensure `GetMaintenanceScheduleList` returns **all** schedules (active + inactive) — add an
  optional `includeInactive` (default true for admin) or drop the active-only filter if present — so a deactivated schedule
  stays visible and reactivatable (mirrors the dispute-list all-status fix). Keep the active badge to distinguish.
- **Reactivate guard:** reactivating a schedule whose (vessel, category, type) key now has **another active** schedule must
  fail loud (the partial-unique-active index would otherwise be violated) — surface a clean business error, don't 500.

## Fix 2 — AdminPanel BFF passthrough
- Add `SetMaintenanceScheduleActiveBff` command wrapping the module endpoint (ServiceRequests feature, `…Bff` convention,
  `IServiceRequestRemoteCall`), exposed on the admin ServiceRequests/MaintenanceSchedules controller. Typed, envelope-correct.

## Fix 3 — admin-web active toggle (FE)
- Wire the **active toggle** that was omitted: in the maintenance-schedules list (a row action) and/or the edit drawer, a
  toggle/switch calling the set-active mutation → optimistic or refetch; surface the reactivate-conflict business error
  verbatim. Show inactive rows (dimmed + inactive badge). tr + en for the new strings.

## Don't-break / QA
- Additive: one command + endpoint + BFF passthrough + FE toggle + the list-includes-inactive change. Existing upsert/list/
  job/reminder logic unchanged. No migration (uses existing `IsActive`). Builds clean; admin-web tsc + eslint clean; tr+en.
- Tests: (1) deactivate → `IsActive=false`, the schedule no longer produces reminders (the job already checks `IsActive`);
  (2) reactivate → back on; (3) reactivate into a key that has another active schedule → clean business error, no 500;
  (4) the admin list returns inactive schedules; (5) set-active is idempotent.

## Verify
1. Admin toggles a schedule off → it shows inactive in the list and stops generating reminders; toggling on re-arms it.
2. A deactivated schedule remains visible in the list (reactivatable), not hidden.
3. Reactivating when another active schedule owns the same (vessel, category, type) fails with a clean message.

## Report
`docs/V1.0.1/ServiceRequest/REPORT_CLEANUP_S12_DEACTIVATE.md`: the set-active command/endpoint, the list-includes-inactive
change, the BFF passthrough, the FE toggle, and the tests. **Do NOT commit.**
