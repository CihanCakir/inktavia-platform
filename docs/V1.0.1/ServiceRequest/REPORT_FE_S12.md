# REPORT — FE_S12 admin maintenance-schedule management UI

**Scope:** Surface S12 (periodic maintenance schedules) in the admin panel — an admin page to create/list/edit
per-vessel, per-category maintenance schedules that drive the N2 reminder. Backend was already implemented
(`api/v1/admin/maintenance-schedules` POST upsert + GET list). All **additive**, tr + en. **Not committed.**

Repos touched: `addesso-project/Bff/src/AdminPanel` (AdminPanel BFF) + `inktavia-marine-admin-web`.

---

## Part 1 — AdminPanel BFF passthrough

Mirrored the existing ServiceRequests-feature convention (`I<Domain>RemoteCall` + per-command/query `…Bff`
subfolders + CQRS handlers auto-registered by assembly scan). Typed, envelope-correct (`AizenApiResponse<T>`),
concrete module DTOs — no re-shaping.

- **`IServiceRequestRemoteCall`** (`…Application/Common/RemoteClients/IServiceRequestRemoteCall.cs`) — two Refit methods:
  - `GetAdminMaintenanceScheduleList([Refit.Query] long? vesselId)` → `AizenApiResponse<GetMaintenanceScheduleListResponse>` (module `GET /api/v1/admin/maintenance-schedules`)
  - `UpsertAdminMaintenanceSchedule([AizenRemoteCallBody] UpsertMaintenanceScheduleRequest)` → `AizenApiResponse<UpsertMaintenanceScheduleResponse>` (module `POST /api/v1/admin/maintenance-schedules`)
- **Query** `ServiceRequests/Query/GetMaintenanceScheduleListBff/` — `GetMaintenanceScheduleListBffQuery(long? vesselId)` + handler (returns `result.Body`).
- **Command** `ServiceRequests/Command/UpsertMaintenanceScheduleBff/` — `UpsertMaintenanceScheduleBffCommand(UpsertMaintenanceScheduleRequest)` + handler (returns `result.Body`).
- **Controller** `ServiceRequestsController` (`api/v1/admin-panel`, policy `AdminPanelAccess`) — two actions under the
  admin SR route base:
  - `GET  service-requests/maintenance-schedules?vesselId=` → `GetMaintenanceScheduleListResponse`
  - `POST service-requests/maintenance-schedules` → `UpsertMaintenanceScheduleResponse`

The module request/response types are the shared abstraction DTOs
(`Aizen.Modules.ServiceRequest.Abstraction.Request|Response.Maintenance.*`) — referenced directly, mirroring
exact field names (`VesselId`, `OwnerUserId`, `ServiceCategoryCode`, `ServiceTypeCode?`, `RecommendedIntervalMonths?`,
`ReminderLeadDays?`, `LastPerformedAt?`, `Notes?` in; `ScheduleId`, `Created`, `NextDueAt` / `Schedules`, `TotalCount` out).
BFF builds clean.

Field notes learned from source (drove the FE):
- Category/type are **free-form string codes** (lookup groups `SERVICE_PROVIDER_CATEGORY` / `MAINTENANCE_SERVICE_TYPE`), NOT enums → no numeric-enum mapper needed.
- Upsert **requires `OwnerUserId` > 0** (validated; used only on create). List GET's only filter is `vesselId`; it returns **active** schedules only.
- Server owns the cycle — `NextDueAt` is computed backend-side (create = now/lastPerformed + interval; update recomputes). FE never computes it.

---

## Part 2 — admin-web page

- **Nav:** added a **Maintenance Schedules / Bakım Programları** child under the collapsible **Service Requests**
  submenu (`DashboardLayout.tsx`, icon `event_repeat`, next to Disputes). Route `ROUTES.MAINTENANCE_SCHEDULES =
  '/app/maintenance-schedules'` (`routes.tsx`) registered in `routeObjects.tsx`.
- **Data layer** (`entities/service-request/`): `types/maintenanceSchedule.ts` (DTO/list/upsert-payload/upsert-result,
  mirroring module field names) + `api/maintenanceScheduleApi.ts` (thin axios passthrough, `list(vesselId?)` GET,
  `upsert(payload)` POST; envelope unwrapped by `normalizeSuccess`). Endpoint + queryKeys blocks added to
  `shared/api/endpoints.ts` + `queryKeys.ts`.
- **Hooks** (`features/service-requests/hooks/`): `useMaintenanceScheduleListQuery` (react-query, null-on-failure like
  the dispute hook) + `useUpsertMaintenanceScheduleMutation` (invalidates the list + toasts on success; on a server
  business-error surfaces `result.message` **verbatim** and does NOT invalidate, leaving the drawer open to correct/retry).
- **List page** (`pages/app/MaintenanceSchedulesPage.tsx`): table — vessel, category (+ type), interval (mo), lead (d),
  last performed, **next due** (overdue = red/danger, due-soon = amber/warning within the lead window, else neutral —
  display-only styling off the server date), reminder sent, active badge. Optional vessel filter. Follows the
  dispute-list pattern (LoadingSkeleton / ErrorState / empty state / clickable rows).
- **Create/edit drawer** (shared `Drawer`): vessel picker + service-category picker (+ optional service-type),
  `RecommendedIntervalMonths`, `ReminderLeadDays`, optional `LastPerformedAt`, notes. Config defaults **12 / 14**
  prefilled on create. Client validation (interval ≥ 1, lead ≥ 0 → Save disabled). On save → upsert (idempotent by
  vessel+category[+type]); the server-computed `NextDueAt` is shown in an inline banner + the list refreshes.
- **Pickers reused:** `useVesselsListQuery` (feeds the vessel `SelectField`; pageSize 200, non-archived) and
  `lookupApi.listItems('SERVICE_PROVIDER_CATEGORY' | 'MAINTENANCE_SERVICE_TYPE')` (feeds the category/type
  `SelectField`s). No dedicated picker components exist in this repo — `SelectField` is the established idiom.
- **OwnerUserId sourcing:** the upsert needs it but the vessel **list** row only optionally carries `ownerUserId`.
  On **create** the drawer reads it from the selected vessel row and, if absent, falls back to the vessel **detail**
  (`owners[0].userId`) via a lazy query; Save stays disabled until it resolves (with an explicit "vessel has no owner"
  message if it truly can't). On **edit** the schedule's own `ownerUserId` is reused. Vessel + category + type are
  **read-only** in edit (they form the immutable upsert key).

### i18n
`serviceRequests.json` (en + tr) gained a `maintenanceSchedule` block (38 keys each, parity verified);
`navigation.json` (en + tr) gained `serviceRequestsChildren.maintenanceSchedules` ("Maintenance Schedules" /
"Bakım Programları"). Dates format per `i18next.language` (en-GB / tr-TR).

---

## Reference-data gotcha (fixed during verification)

The admin-web `LookupItem` type declares `value`/`localizations`, but the AdminPanel BFF's
`GET /reference-data/lookup/{groupCode}/items` actually returns the **module `LookupItemDto`** shape
(`code`, `name`, …) — no `value`/`localizations`. Category options first rendered as raw codes
(`MOTOR_MAINTENANCE`); switched the label to read `name` (via a local `RefItem` type) → now shows
"Motor Maintenance" etc. Localized reference names aren't exposed by that endpoint, so category/type labels are the
seeded (English-ish) `name` in both locales — acceptable; a localized lookup endpoint would be a separate follow-up.

## Stale-container gotcha (resolved)

The running `service-request-api` image predated the S12 backend, so the BFF's authenticated call 404'd (the direct
401 was just auth-before-routing, not proof of existence). Rebuilt + restarted `service-request-api` **and**
`bff-adminpanel` (single-service builds to avoid the known parallel-build OOM). After that the list GET went 500→200.
Note: a failed list `ApiResult` resolves to `null` (not a thrown error), so the page shows the empty-state rather than
the error-state on a 5xx — that masked the initial 404/500; worth keeping in mind, but behaviour matches the dispute
hook convention.

---

## On-screen verification (localhost:3000, admin logged in, docker BFF+module rebuilt)

1. ✅ Service Requests submenu shows **Bakım Programları / Maintenance Schedules** → opens the list (GET 200).
2. ✅ Create (Blue Octopus + Motor Maintenance + interval 12 + lead 14) → POST 200, banner "Bakım programı
   oluşturuldu. Sonraki tarih: **6 Ağu 2027**" (now + 12 mo, server-computed); row appears. Editing interval → 6
   updated the row and Next due recomputed to **6 Şub 2027** (now + 6 mo).
3. ✅ Re-saving the same vessel+category (edit) returned "**Mevcut program güncellendi**" and the list stayed at
   **one** row (idempotent, no duplicate). Business-error path is wired (verbatim `result.message` toast, no
   invalidate) — not force-triggered on screen since client validation blocks the reachable invalid states.
4. ✅ Overdue (Boat Cleaning, Next due 1 Jun 2025 — **red**) and due-soon (Electrical Service, Next due 15 Aug 2026,
   within 14-day lead — **amber**) rows are visually distinct from the neutral one; existing SR/dispute pages + nav
   unaffected. Verified in **both** tr and en (columns, "12 mo"/"14 d", localized dates, Active badges).

admin-web `tsc --noEmit` clean, `eslint` clean on all changed files; BFF builds clean.

Test data: three demo schedules for vessel "Blue Octopus" remain in the local dev DB (created during verification).

---

## Follow-up notes

- **Owner self-service (owner app):** owners creating their own per-vessel maintenance schedules would need an
  owner-scoped upsert/list endpoint (the current one is `[Authorize(Roles=Admin)]`, and `OwnerUserId` is admin-supplied).
  A marine-mobile-BFF passthrough scoped to the current owner (deriving `OwnerUserId`/vessel ownership server-side) is
  the natural owner-app follow-up.
- **Deactivate:** the S12 upsert request has no `IsActive` field and the module exposes no deactivate endpoint (the
  entity has `Deactivate()` but it's unreachable), so the admin UI shows the active badge but intentionally has **no
  active toggle** — all listed schedules are active. Exposing deactivate (module command + BFF + a list toggle) is a
  small follow-up if needed.
- **Localized reference labels:** category/type labels come from the lookup `name` (not localized at that endpoint).

**DO NOT COMMIT** (per instructions).
