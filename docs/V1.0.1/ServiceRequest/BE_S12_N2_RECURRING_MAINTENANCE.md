# BE_S12 + N2 — periodic maintenance schedule + due-reminder (recurring job)

> **Repos:** `addesso-project` — **ServiceRequest module** (S12: the schedule + recompute) + **Notification module** (N2:
> the reminder type + consumer). SR §20.14 + Notification N2. A per-vessel, per-category **maintenance schedule** with a
> configurable interval, and a **daily recurring job** that reminds the owner before it's due — using the existing
> `AizenRecurringJob` framework (mirror `KitExpiryReminderJob`). **Separate from CargoDry renewal** (§20.14 — do not merge).
> Additive. **Do not commit** until the user says.

## Mechanism (confirmed) — recurring job, not per-item scheduling
The project uses `AizenRecurringJob` (base class, `CronExpression` + `ProcessAsync`) **auto-discovered** by
`AddAizenRecurringJob()` assembly scanning — a new job in the SR module registers automatically. `KitExpiryReminderJob`
(daily `0 9 * * *`, scans a repo for due items, publishes a bus message per target → Notification consumes) is the exact
template. **Scan-based daily job + a `ReminderSentAt` idempotency marker**, multi-replica safe via the framework — not
Hangfire per-item `Schedule(at)`.

## Current state (investigated)
- `ServiceRequestEntity` carries `VesselId`, `ServiceCategoryCode`, `ServiceTypeCode`, `OwnerUserId` — the schedule keys on
  (vessel, category). Completion approval already emits `ServiceRequestCompletionApprovedMessage` (used by N3) — reuse it
  to advance the schedule.
- `NotificationType` SR area: 100–102, completion 130–133, dispute 140–141. **Reminder needs a new type.** The N0–N-E
  delivery platform (preferences/channels/category map) is done — route through it. **Gotcha (from N3):** a type with **no
  seeded template is a silent no-op** — seed the template.

## S12 — `MaintenanceScheduleEntity` (ServiceRequest module)
- Fields: `VesselId`, `ServiceCategoryCode` (+ optional `ServiceTypeCode`), `OwnerUserId`, `RecommendedIntervalMonths`,
  `LastPerformedAt?`, `NextDueAt`, `ReminderLeadDays`, `ReminderSentAt?`, `IsActive`, `Notes?`. **All intervals/leads are
  configurable — no hardcoded constants** (per-schedule value; a `MaintenanceScheduleOptions` config supplies defaults,
  admin-tunable). Unique per (VesselId, ServiceCategoryCode[, ServiceTypeCode]) active schedule.
- Domain: `NextDueAt = (LastPerformedAt ?? CreatedAt) + RecommendedIntervalMonths` (UTC, timestamptz-safe). A validating
  factory + a domain method `RecordPerformed(performedAtUtc)` that sets `LastPerformedAt`, recomputes `NextDueAt`, and
  **resets `ReminderSentAt = null`** (re-arms the reminder for the next cycle).
- **Create/upsert command** (admin now; owner later when the owner app lands): set vessel+category+interval+lead. Migration
  append-only + idempotent (upsert by the unique key). Guard against duplicate active schedules.
- **Recompute on completion:** a consumer of `ServiceRequestCompletionApprovedMessage` (or a hook in the approval path)
  finds an active schedule for the SR's (vessel, category) and calls `RecordPerformed(completionApprovedAt)` so the cycle
  advances automatically when a matching service is completed. If no schedule exists, no-op (optionally auto-create if the
  category is flagged recurring — config-driven, default off for MVP).

## N2 — `MaintenanceReminderDueJob` + notification
- **Job** (`MaintenanceReminderDueJob : AizenRecurringJob`, daily cron e.g. `0 8 * * *`, mirror `KitExpiryReminderJob`):
  scan schedules where `IsActive && ReminderSentAt is null && now ≥ NextDueAt − ReminderLeadDays` → for each, publish
  **`MaintenanceReminderDueMessage`** (ownerUserId, vesselId, vesselName, categoryCode, nextDueAt, scheduleId) and set
  `ReminderSentAt = now`. **Idempotent** via the marker (a re-run the same day re-selects nothing); multi-replica safe (the
  framework runs it once). One reminder per cycle — no spam; the next reminder only after `RecordPerformed` re-arms it.
- **Notification:** new `NotificationType.MaintenanceReminderDue` (next free in the SR range, e.g. **103**) +
  `MaintenanceReminderDueConsumer` → notify the **owner** ("{vessel} için {kategori} bakımı {tarih} tarihinde — planlayın"
  / EN) **through the N-B preference/channel/category path**. **Category-map** the new type + **seed a notification
  template** for it (else silent no-op). Deep-link to the vessel / a "create service request" action if the platform
  supports it.

## Don't-break / QA
- Additive: new SR entity + upsert command + completion-advance consumer + the recurring job + one new notification type +
  consumer + template seed + category-map entry. **Separate from CargoDry renewal** — no CargoDry code touched. Existing
  SR/completion/notification flows unchanged. Migration append-only; UTC-safe (`NextDueAt`/`LastPerformedAt`/`ReminderSentAt`
  timestamptz). Idempotent job + upsert; multi-replica safe. Builds clean.
- Unit/integration tests: (1) create a schedule → `NextDueAt` computed from interval; (2) completion of a matching (vessel,
  category) SR advances `LastPerformedAt`/`NextDueAt` and resets `ReminderSentAt`; (3) the job selects a schedule at
  `NextDueAt − ReminderLeadDays`, publishes once, sets the marker, and does **not** re-fire the same day; (4) the new
  notification routes through N-B (respects preferences) and has a template (not a silent no-op); (5) no active schedule →
  completion is a no-op; CargoDry untouched.

## Verify
1. Create a maintenance schedule (vessel + category + interval e.g. 12 months, lead 14 days) → `NextDueAt` set correctly.
2. At `NextDueAt − 14d` the daily job publishes the reminder once; the owner receives the `MaintenanceReminderDue`
   notification (honouring preferences); a second run the same day sends nothing.
3. Completing a matching service request advances the schedule (`LastPerformedAt`/`NextDueAt` move, reminder re-arms).
4. Nothing in CargoDry renewal changes; existing notifications unaffected.

## Report
`docs/V1.0.1/ServiceRequest/REPORT_S12_N2.md`: the `MaintenanceSchedule` model + recompute-on-completion, the recurring
job (cron, scan, idempotency marker, multi-replica), the new notification type + consumer + **template seed** (N-B path),
the config-driven interval/lead, and the tests. Note FE follow-ups (admin schedule management now; owner self-service +
the "book maintenance" deep-link when the owner app lands). **Do NOT commit.**
