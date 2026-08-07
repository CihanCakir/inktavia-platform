# REPORT — CLEANUP S12 maintenance-schedule active toggle (deactivate/reactivate reachable)

> Executes `CLEANUP_S12_DEACTIVATE.md`. ServiceRequest module + AdminPanel BFF + admin-web. Closes the S12 FE
> follow-up: the entity had `Deactivate()`/`Reactivate()` but no command/endpoint reached them. **Additive, no
> migration** (uses existing `IsActive`). Backend + BFF build clean; admin-web tsc + eslint clean; tr + en.
> **NOT committed.**

---

## Starting state (confirmed in source)

- `MaintenanceScheduleEntity` already has `IsActive` (from `AizenEntityWithAudit`), `Deactivate()`, `Reactivate()`
  — reachable only from code.
- The upsert key is **(VesselId, ServiceCategoryCode, ServiceTypeCode) among active rows**, backed by a
  **partial-unique-active index** (`"IsActive" = true AND "IsDeleted" = false`) — so a dedicated set-active action
  is the right shape (not an `IsActive` field on upsert).
- `GetMaintenanceScheduleListQuery` was **active-only** (`IMaintenanceScheduleRepository.ListActiveAsync` filtered
  `x.IsActive`) → a deactivated schedule vanished and could not be reactivated from the UI.
- The N2 reminder job (`GetDueForReminderAsync` + `NeedsReminder`) and the completion-advance path
  (`GetActiveMatchForCompletionAsync`) already gate on `IsActive`, so a deactivated schedule stops producing
  reminders / stops advancing with **no job change needed**.

---

## Fix 1 — backend (ServiceRequest module)

**New — set-active command + handler**
`Application/Command/Maintenance/SetMaintenanceScheduleActive/SetMaintenanceScheduleActiveCommand(long scheduleId, bool isActive)` + handler:
- load by id → **not found = clean `AizenBusinessException`** ("Maintenance schedule not found.");
- **idempotent**: `schedule.IsActive == isActive` → return, **no write**;
- when reactivating (`isActive == true`): **reactivate guard** — `GetActiveByKeyAsync(vessel, category, type)`; if
  another active schedule (different id) already owns the key → **clean `AizenBusinessException`**
  ("Another active maintenance schedule already exists for this vessel and category…"), **no 500**, no write;
- otherwise `Reactivate()` / `Deactivate()`, `Update`, `SaveChanges`.
- New DTOs: `Request/Maintenance/SetMaintenanceScheduleActiveRequest { bool IsActive }`,
  `Response/Maintenance/SetMaintenanceScheduleActiveResponse(long scheduleId, bool isActive)`.

**New endpoint** on `MaintenanceScheduleController`:
`PUT /api/v1/admin/maintenance-schedules/{id}/active` body `{ isActive }`, `[ProducesResponseType]`,
envelope-correct (`AizenApiResponse<SetMaintenanceScheduleActiveResponse?>`).

**List includes inactive** — `IMaintenanceScheduleRepository.ListActiveAsync(vesselId)` →
`ListAsync(vesselId, includeInactive)` (EF filter now `!IsDeleted && (includeInactive || IsActive)`, ordered
**active-first then soonest-due** so deactivated rows sink to the bottom but stay visible/reactivatable).
`GetMaintenanceScheduleListQuery` gained `IncludeInactive` (**default true** for admin); controller `GET` gained
`includeInactive = true`. (The only caller of `ListActiveAsync` was this query — no other call sites; verified.)

Existing upsert / list-mapping / N2 job / reminder logic **unchanged**. No migration.

## Fix 2 — AdminPanel BFF passthrough

- `IServiceRequestRemoteCall`: added `SetAdminMaintenanceScheduleActive(long id, SetMaintenanceScheduleActiveRequest)`
  (`AizenRemoteCallPut` `/api/v1/admin/maintenance-schedules/{id}/active`) and `includeInactive = true` on
  `GetAdminMaintenanceScheduleList`.
- New `SetMaintenanceScheduleActiveBff` command + handler (ServiceRequests feature, `…Bff` convention,
  `IServiceRequestRemoteCall`) — returns `SetMaintenanceScheduleActiveResponse.Body`; the module's business error
  (incl. the reactivate conflict) passes through verbatim.
- `GetMaintenanceScheduleListBffQuery` gained `IncludeInactive` (default true), threaded to the remote call.
- `ServiceRequestsController` (BFF): `PUT service-requests/maintenance-schedules/{id}/active` + `includeInactive`
  on the GET, both typed + envelope-correct.

## Fix 3 — admin-web active toggle

`inktavia-marine-admin-web`:
- `entities/service-request/types/maintenanceSchedule.ts` — `SetMaintenanceScheduleActivePayload` / `…Result`.
- `entities/service-request/api/maintenanceScheduleApi.ts` — `setActive(id, { isActive })` (PUT); `list()` now
  sends `includeInactive: true`.
- `shared/api/endpoints.ts` — `MAINTENANCE_SCHEDULE_ACTIVE(id)`.
- `features/service-requests/hooks/useSetMaintenanceScheduleActiveMutation.ts` — invalidates the list on success,
  toasts deactivated/reactivated; on a business error **surfaces the server message verbatim** (the reactivate
  conflict).
- `pages/app/MaintenanceSchedulesPage.tsx`:
  - **row action** — a `toggle_on/toggle_off` icon-button per row (stops row-click propagation) firing the mutation;
  - **edit-drawer switch** — an Active/Inactive section with a Deactivate/Reactivate button (edit mode only) that
    closes the drawer on success and stays open on the conflict error;
  - **inactive rows dimmed** (`opacity-50`) + the existing Active/**Inactive** `StatusBadge` retained.
- i18n: 10 new keys × tr + en in `serviceRequests.json` (`maintenanceSchedule.*`: activeToggleLabel,
  activeToggleHintOn/Off, deactivate, reactivate, activeSavedTitle, reactivatedBody, deactivatedBody,
  activeFailedTitle) — **47/47 key parity** verified.

---

## Tests

`Modules/ServiceRequest/tests/…/SetMaintenanceScheduleActiveHandlerTests.cs` (xunit + FluentAssertions; an
in-memory `FakeRepo` mirroring the EF repository's filter/ordering, since the SR test project carries no mocking
library). **All green** — full SR suite **145/145**, maintenance subset **19/19**.

1. **Deactivate → `IsActive=false`, no more reminders** — `Deactivate_turns_off_and_stops_reminders`: the schedule
   was in `GetDueForReminderAsync` while active, is absent after deactivate, `NeedsReminder` false, one write.
2. **Reactivate → back on** — `Reactivate_turns_back_on`.
3. **Reactivate into a taken key → clean business error, no 500, no write** —
   `Reactivate_into_a_taken_key_fails_with_a_clean_business_error`: throws `AizenBusinessException` ("*already
   exists*"), the row stays inactive, `SaveChangesCalls == 0`.
4. **Admin list returns inactive** — `Admin_list_includes_inactive_schedules_by_default` (default IncludeInactive
   true, active ordered first) + `Admin_list_can_exclude_inactive_when_requested` (false → active only).
5. **Set-active idempotent** — `Setting_the_same_state_is_a_noop`: no state change → `SaveChangesCalls == 0`.
   Plus `Not_found_is_a_clean_business_error`.

Build / static checks:
- **Full solution `dotnet build Aizen.sln`: 0 Errors** (module, BFF, everything). No stray `ListActiveAsync` callers.
- **admin-web `tsc --noEmit`: clean; `eslint` on all changed files: clean.**

---

## Verify (per the doc)

Behavioral verification is covered programmatically by the handler tests above (they exercise the exact three
doc scenarios end-to-end at the command/query layer): deactivate stops the reminder scan and toggling on re-arms
it (1); the admin list returns the deactivated schedule (2); reactivating into a key another active schedule owns
fails with a clean message and no write (3). The FE wiring is tsc/eslint-clean and the toggle surfaces that same
business message verbatim.

**Remaining on-screen check (needs the running stack — bff-adminpanel + service-request-api + admin-web):** click
the row/drawer toggle to confirm the badge flips + inactive dimming, that a deactivated row stays listed, and that
the reactivate-conflict toast shows the server message. All three are backed by the passing unit tests.

**Note (in scope, no change):** upsert is unchanged, so editing an *inactive* schedule's fields and pressing Save
runs the idempotent upsert — which, finding no active row for the key, creates a fresh active schedule rather than
mutating the inactive one. Reactivate (the toggle) is the intended path to bring a schedule back; this edge is
called out rather than altering upsert.

**Do NOT commit.**
