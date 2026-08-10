# REPORT — BE_MO8 owner maintenance-schedule self-service

> Owner Economics **MO8** — the owner manages **recurring maintenance schedules for their own vessels** (view /
> create-edit / activate-deactivate) — the "owner self-service" S12 deferred. Additive; identity from token;
> owner-scoped. The N2 reminder itself surfaces in MO9. **NOT committed.**
>
> Repos: `addesso-project` (ServiceRequest module + Marine.Participant.Mobile BFF, reads Vessel for ownership) +
> `inktavia-marine-mobile` (FE).

## Outcome
- **BE builds clean** — ServiceRequest module: 0 errors; Marine.Participant.Mobile BFF: 0 errors.
- **Tests: 174/174 green** in `Aizen.Modules.ServiceRequest.Application.UnitTests` (5 new MO8 FakeRepo cases).
- **No admin relaxation, no SA admin grant.** The admin `MaintenanceScheduleController`
  (`api/v1/admin/maintenance-schedules`, `[Authorize(Roles="Admin")]`) and the S12 engine (upsert / set-active /
  active-unique / N2 job) are **unchanged**.

---

## The blocker (MO2b-style) and the fix
S12's only endpoints are Admin-gated → the mobile-bff SA is not Admin → 403; and the admin upsert takes `OwnerUserId`
from the **body** with no vessel-ownership check. MO8 fixes this the MO2c way: **new owner-facing, non-admin
endpoints** (do not grant the SA admin, do not relax the admin controller). `OwnerUserId` is the token owner (never
the body); the owner may only touch schedules for **vessels they own** (BFF-gated).

## BE — ServiceRequest module (additive; reuse the S12 engine)
- **New owner controller** `OwnerMaintenanceScheduleController` at `api/v1/service-requests/maintenance-schedules`,
  `[Authorize]` (participant/service-token — callable by the mobile-bff SA via BffAssertion), **separate** from the
  admin one. Injects `IAizenInfoAccessor`; resolves `CurrentUserId` exactly like `ServiceRequestController` (asserted
  `UserInfo.UserId`, NameIdentifier fallback).
  - **List** `GET` → `GetOwnerMaintenanceSchedulesQuery(CurrentUserId, vesselId?, includeInactive)`.
  - **Upsert** `POST` → **stamps `request.OwnerUserId = CurrentUserId`** (any body value is ignored) → reuses the S12
    `UpsertMaintenanceScheduleCommand` verbatim (idempotent by (vessel, category, type)).
  - **Set-active** `PUT {id}/active` → `SetOwnerMaintenanceScheduleActiveCommand(CurrentUserId, id, isActive)`.
- **New owner query** `GetOwnerMaintenanceSchedulesQuery` + handler → uses a new additive repo method
  `ListByOwnerAsync(ownerUserId, vesselId?, includeInactive)` (same active-first ordering + includeInactive as the
  admin `ListAsync`, plus `OwnerUserId == owner`). Reuses the S12 list response + DTO mapping. Returns empty when the
  owner id is ≤ 0 (never fabricates).
- **New owner set-active** `SetOwnerMaintenanceScheduleActiveCommand` + handler — a thin owner-gated copy of the S12
  set-active: loads by id, **verifies `schedule.OwnerUserId == command.OwnerUserId`** (else a clean not-found — never
  leaks existence), then applies the identical idempotent no-op + **reactivate-conflict guard** + domain
  `Deactivate()`/`Reactivate()`. The admin set-active command/handler are untouched.
- Repo: `ListByOwnerAsync` added to `IMaintenanceScheduleRepository` + the EF implementation (additive).

## BE — Marine.Participant.Mobile BFF (passthroughs, owner-asserted, vessel-gated, cost-free)
- **Remote calls** on `IServiceRequestRemoteCall`: `GetOwnerMaintenanceSchedules(vesselId?, includeInactive)`,
  `UpsertOwnerMaintenanceSchedule(request)`, `SetOwnerMaintenanceScheduleActive(scheduleId, request)`.
- **Cost-free mobile DTOs** (`Contracts/Maintenance/`): `MobileMaintenanceScheduleDto` (drops the `OwnerUserId` echo
  and the N2 `ReminderSentAt` marker), `MobileMaintenanceScheduleListDto`, `MobileUpsertMaintenanceScheduleRequest`
  (no `OwnerUserId`, no `IsActive`), `MobileSetMaintenanceScheduleActiveRequest`, + the two result DTOs.
- **Handlers** (`Maintenance/`): `GetMobileMaintenanceSchedulesQuery` (resolve → passthrough → map),
  `UpsertMobileMaintenanceScheduleCommand` (resolve → **vessel-ownership gate**: `GetUserVessels` +
  `Any(v.Id == VesselId)`, else "Vessel not found." → client-validate interval≥1 / lead≥0 → proxy; `OwnerUserId` is
  left unset — the SR owner endpoint stamps it from the token), `SetMobileMaintenanceScheduleActiveCommand` (resolve →
  passthrough; the module owner-gates by `OwnerUserId`). The reactivate-conflict / duplicate error surfaces verbatim.
- **New controller** `MaintenanceController` (`api/v1/mobile/maintenance-schedules`) — GET list, POST upsert,
  PUT {id}/active.

Why the vessel-gate is BFF-side: the S12 upsert does no vessel-ownership check (fine for a trusted admin). The mobile
BFF already holds `IVesselRemoteCall`, and `GET /api/v1/vessels/current-user` is caller-scoped by the assertion, so
`VesselId ∈ owner's vessels` is the correct, least-privilege gate — consistent with MO1–MO7 (the BFF is the
owner-gate; the module trusts the assertion). Set-active/list need no vessel call — the schedule stores `OwnerUserId`,
so the module owner-gates them directly.

---

## Tests (`OwnerMaintenanceMo8Tests`, module Application.UnitTests — 174/174 green)
1. **Owner lists only their own** — `GetOwnerMaintenanceSchedulesQuery(100)` returns only OwnerUserId=100 rows; a
   different owner sees only theirs.
2. **No identity → empty** — owner id 0 returns an empty list (never fabricates).
3. **Set-active owner-gated** — owner 100 toggles their own; owner 200 hitting owner 100's schedule → clean
   not-found, no toggle. (tests 2/6)
4. **Set-active idempotent + reactivate-conflict** — same-state is a no-op (no write); reactivating into a key another
   active schedule owns → clean business error, no write. (test 4)
5. **Owner upsert idempotent by key + stamps the owner** — two upserts for the same (vessel, category, type) → one
   row, `Created=false` the second time, `OwnerUserId` = the id the owner controller passes (the token). (test 3)

### How the remaining acceptance criteria are enforced
- **(2) OwnerUserId from token (body ignored) + non-owned vessel rejected** — the owner controller overwrites
  `request.OwnerUserId = CurrentUserId` before dispatching (build-verified); the mobile BFF's vessel-ownership gate
  (`GetUserVessels` + `Any`) rejects a foreign vessel with "Vessel not found." (build-verified).
- **(5) admin + S12 engine unchanged** — the admin controller/commands are untouched; grep:
  `api/v1/admin/maintenance-schedules` still `[Authorize(Roles="Admin")]`; the owner surface is a separate controller +
  additive query/command/repo-method. Deactivate stops N2 via the existing `IsActive` gate on the job (unchanged).

---

## FE — inktavia-marine-mobile
Mirrors the MO7 membership slice + the vessel/reference pickers. **`npx tsc --noEmit` = 0 errors; i18n en/tr
`maintenance` 36/36 (whole file 586/586); owner-scoped/cost-free.** Not committed.

**New feature `src/features/maintenance/`** — `api/maintenanceApi.ts` (`fetchSchedules`/`upsertSchedule`/
`setScheduleActive`; never sends OwnerUserId), `api/useMaintenance.ts` (`useMaintenanceSchedules`/`useUpsertSchedule`/
`useSetScheduleActive`), and two screens:
- `MaintenanceSchedulesScreen` — the owner's schedules list: category (via `useReferenceLookup('SERVICE_PROVIDER_CATEGORY')`),
  vessel name (joined from `useVessels()`), interval / reminder-lead, last-performed ("Never" fallback), **next-due
  with overdue (red) / due-soon (amber, within lead) / ok (green)** coloring, an active badge, an activate/deactivate
  toggle (reactivate-conflict surfaced verbatim), Loading/Error/Empty states, and a "+"/empty CTA → create.
- `MaintenanceEditScreen` — create/edit form (FormField + SelectInput + numeric TextInput + TextareaInput + useToast).
  Vessel picker limited to the owner's own vessels + category picker; on **edit** the vessel+category (the idempotent
  key) are immutable/prefilled; interval (default 12, ≥1) / lead (default 14, ≥0) / notes editable; save → upsert →
  toast + goBack; business errors (vessel-not-found / conflict) verbatim.

**Wiring** — `endpoints.ts` (`MAINTENANCE`), `queryKeys.ts` (`maintenance`), Profile stack registration
(`types.ts` + `ProfileNavigator.tsx`), and `ProfileScreen` adds a "Maintenance schedules" row.

**Mock parity** — new `maintenance.handlers.ts` (registered in `setupMocks.ts`): schedules on vessels 1/2
(engine/hull/electrical) — one overdue, one due-soon, one ok; GET list (vesselId + includeInactive filter), POST
upsert (idempotent by vessel+category+type, recomputes next-due), PUT `{id}/active` (toggle + reactivate-conflict
`err(400, …already exists…)`).

**Deviations** — no lint config (tsc is the sole gate); plain `useState` form state (matches MembershipScreen);
service **type** not exposed in the form (MVP passes `serviceTypeCode: null` / keeps the existing type on edit — a
type picker follows once `MAINTENANCE_SERVICE_TYPE` is seeded).

---

## Don't-break / QA
Additive: new owner-facing endpoints (no admin relaxation, no SA admin grant) + an owner list filter + BFF
passthroughs + FE. The S12 engine and admin controller are unchanged. Owner-scoped: `OwnerUserId` from token;
vessel-ownership enforced (an owner cannot schedule someone else's vessel). The N2 reminder itself is unchanged
(surfaced in MO9).

## Next
**MO9** — notifications & realtime (the owner event set + push + preferences).
