# REPORT — S12 recurring maintenance schedule + N2 due-reminder

> `addesso-project` — **ServiceRequest module** (S12 schedule + recompute + reminder job) + **Notification module**
> (N2 reminder type + consumer + template). Additive, UTC-safe, migration append-only. Uses the existing
> `AizenRecurringJob` framework (scan-based, `ReminderSentAt` idempotency marker — not Hangfire per-item scheduling).
> **Separate from CargoDry renewal** (§20.14 — no CargoDry code touched). **Not committed.**

## S12 — `MaintenanceScheduleEntity` (per-vessel, per-category cadence)

**Model** (`Domain/Entities/Maintenance/MaintenanceScheduleEntity.cs`, `: AizenEntityWithAudit`):
`VesselId`, `ServiceCategoryCode` (+ optional `ServiceTypeCode`), `OwnerUserId`, `RecommendedIntervalMonths`,
`LastPerformedAt?`, `NextDueAt`, `ReminderLeadDays`, `ReminderSentAt?`, `Notes?`, plus inherited `IsActive`.
All timestamps UTC (timestamptz).
- Validating factory `Create(...)`: `NextDueAt = (LastPerformedAt ?? now) + RecommendedIntervalMonths`; reminder un-armed.
- `RecordPerformed(performedAtUtc)`: stamps `LastPerformedAt`, recomputes `NextDueAt`, **resets `ReminderSentAt = null`**
  (re-arms the next cycle).
- `UpdateSchedule(interval, lead, notes)`: admin re-config, recompute `NextDueAt` from the same anchor, re-arm.
- `IsInReminderWindow(now)` / `NeedsReminder(now)`: `now ≥ NextDueAt − ReminderLeadDays`, active, un-armed — the
  job's selection predicate, centralised + unit-tested.
- **Configurable, no hardcoded constants**: interval/lead are per-schedule; defaults come from
  `Application/Maintenance/MaintenanceScheduleOptions.cs` (keys under `ServiceRequest:` — `MaintenanceDefaultIntervalMonths`
  12, `MaintenanceDefaultReminderLeadDays` 14, `MaintenanceReminderBatchSize` 200, `MaintenanceAutoCreateOnCompletion`
  false), added to `configuration/appsettings.json`.

**Persistence**: `maintenance_schedules` (schema `servicerequest`) with a **partial unique index** on
`(VesselId, ServiceCategoryCode, ServiceTypeCode) WHERE IsActive = true AND IsDeleted = false` (one active schedule per
key) + indexes on `NextDueAt` and `OwnerUserId`. Repository `IMaintenanceScheduleRepository` (registered in
`Repository/DependencyInjection.cs`). Migration `20260806172200_AddMaintenanceSchedule` — **append-only** (creates only
the new table + indexes; `Down` drops only it); all DateTime cols are `timestamp with time zone`.

**Create/upsert command** (`Application/Command/Maintenance/UpsertMaintenanceSchedule/…`): admin-only, idempotent by the
unique key — an existing active schedule is **updated** (never a second active created → the duplicate-active guard),
else created. Interval/lead default from config when omitted. Exposed at
`POST /api/v1/admin/maintenance-schedules` (+ `GET …?vesselId=` list) via `Controller/V1/Maintenance/MaintenanceScheduleController.cs`
(`[Authorize(Roles = "Admin")]`).

**Recompute on completion** (`Consumers/CompletionApprovedScheduleAdvancer.cs` + `Application/Command/Maintenance/AdvanceMaintenanceSchedule/…`):
a consumer of the SR module's own `ServiceRequestCompletionApprovedMessage` (auto-registered by the messagebus assembly
scan; the SR host runs `AppType.Worker`) dispatches `AdvanceMaintenanceScheduleCommand`. The handler finds the active
schedule matching the completed SR's (vessel, category, type) — exact-type match preferred, else category-level (null
type) — and calls `RecordPerformed(completion.ReviewedAt)`. **No matching schedule → no-op** (auto-create only if the
`MaintenanceAutoCreateOnCompletion` flag is on, default off). Decoupled from the approval transaction so a schedule miss
never blocks completion.

## N2 — `MaintenanceReminderDueJob` + notification

**Job** (`Jobs/MaintenanceReminderDueJob.cs`, `: AizenRecurringJob`, cron `0 8 * * *`, mirrors `KitExpiryReminderJob` /
`CompletionAutoApprovalJob`): scans active, un-armed schedules whose reminder window has opened (`NeedsReminder(now)`),
publishes `MaintenanceReminderDueMessage` (scheduleId, ownerUserId, vesselId, vesselName, categoryCode, nextDueAt) and
stamps `ReminderSentAt = now`. **Idempotent + multi-replica safe**: the recurring trigger fires once cluster-wide
(scheduler distributed lock), the per-item `ReminderSentAt` marker (re-checked under each item's own scope) guarantees
one reminder per cycle — a same-day re-run re-selects nothing; the next reminder only after `RecordPerformed` re-arms.
The scan query is translation-safe (DB pre-filters active/un-armed/soonest-first; the exact per-row lead window is
applied in memory via `NeedsReminder`).

**Notification** — new `NotificationType.MaintenanceReminderDue = 103` (in the 100–139 ServiceRequests-gated range).
`Consumers/ServiceRequest/MaintenanceReminderDueConsumer.cs` sends a `SendNotificationCommand` to the owner through the
**N-B path** (`Channel = InApp` baseline write; push/email honour the ServiceRequests-category preferences), variables
`{{vessel}}` / `{{category}}` / `{{date}}`. **Category map**: 103 falls in the existing `>= 100 and <= 139 →
ServiceRequests` range (no code change; comment updated + test added). **Template seeded** (the N3 silent-no-op trap):
`SR_MAINTENANCE_REMINDER_DUE_INAPP` added to `NotificationTemplateSeed.BuildTemplates()` ("Bakım hatırlatması —
{{vessel}}" / "{{vessel}} için {{category}} bakımı {{date}} tarihinde planlanmalı…") — inserted idempotently on boot
(no migration for templates).

## Tests (all green)
- `MaintenanceScheduleEntityTests` (12): **(1)** create → `NextDueAt` from interval (last-performed + now-anchored);
  **(2)** `RecordPerformed` advances `LastPerformedAt`/`NextDueAt` and **resets `ReminderSentAt`**; **(3)** `NeedsReminder`
  opens exactly at `NextDueAt − lead`, once-guard after `MarkReminderSent`, off when deactivated; `UpdateSchedule`
  recompute + re-arm; invalid-input guards.
- `MaintenanceScheduleOptionsTests` (3): config defaults, overrides, invalid→fallback (config-driven, no magic constants).
- `NotificationCategoryMapN3Tests` (+1 case): **(4)** `MaintenanceReminderDue → ServiceRequests` (toggleable / N-B gated,
  not the always-deliver Account fallback). Template presence is guaranteed by the seed entry paired to the consumer's
  `Type + Channel`.
- **(5)** no active schedule → `AdvanceMaintenanceScheduleCommandHandler` returns `Advanced = false` (no-op); **CargoDry
  untouched** — `git status` shows zero CargoDry files changed.

## Verification
- Both module hosts (`Aizen.Modules.ServiceRequest`, `Aizen.Modules.Notification`) **build clean** (0 errors).
- 12 SR + 6 Notification unit tests **pass**.
- Migration **applies cleanly to a throwaway DB** (`sr_s12_migtest`): full SR migration chain up to
  `AddMaintenanceSchedule`; the table + the partial-unique index (`WHERE IsActive = true AND IsDeleted = false`) are
  created as expected; throwaway dropped. `inktavia_store` not mutated (it auto-migrates on next deploy).

## FE follow-ups
- **Admin schedule management (now)**: the BE surface is ready — `POST/GET /api/v1/admin/maintenance-schedules` (upsert +
  list); wire an admin-web page (create/edit interval+lead per vessel/category, view next-due).
- **Owner self-service + "book maintenance" deep-link** when the owner app lands: owner-scoped create/list, and a reminder
  CTA that opens "create service request" pre-filled with the vessel/category.
- **Vessel-name enrichment**: the reminder currently sends `VesselName = null` (the SR module has no vessel names) and the
  consumer falls back to `#{vesselId}`; enrich via a vessel lookup (or FE resolution) for a friendlier notification.

**Do NOT commit.** Deploy note: on next `service-request-api` + `notification-api` deploy the migration applies and the
template seeds (idempotent); the recurring job + consumers auto-register (Worker/Scheduler on both hosts).
