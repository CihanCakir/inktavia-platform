# BE_MO8 — owner maintenance-schedule self-service (mobile)

> **Repos:** `addesso-project` (ServiceRequest module + Marine.Participant.Mobile BFF; reads Vessel for ownership) +
> `inktavia-marine-mobile`. Owner Economics track **MO8**: the owner manages **recurring maintenance schedules for their own
> vessels** (view / create-edit / activate-deactivate) — the "owner self-service" S12 deferred. The N2 reminder surfaces in
> MO9. Additive; identity from token; owner-scoped. **Do not commit.**

## Baseline (investigated) — and the MO2b-style blocker
- S12 built the engine: `MaintenanceScheduleEntity`, upsert (`GetActiveByKeyAsync` idempotent), list (`ListAsync(vesselId,
  includeInactive)`), set-active (deactivate/reactivate with the active-unique guard), + the **N2 reminder job**.
- **But the only endpoints are Admin-gated:** `MaintenanceScheduleController` is `[Authorize(Roles="Admin")]` at
  `api/v1/admin/maintenance-schedules`. The **mobile-bff SA is not Admin → 403** (the same blocker as MO2b). Also the
  **upsert request takes `OwnerUserId` from the body** and the handler does **no vessel-ownership check** (fine for a
  trusted admin, unsafe for owner self-service).
- Vessel module exposes ownership (`VesselOwnerDto` / owner vessel lists); the mobile BFF already has `IVesselRemoteCall`
  (M4/MO1).

## Fix approach (least-privilege, no admin relaxation)
Add **new owner-facing, non-admin SR endpoints** (owner-scoped) — do **not** grant the SA admin and do **not** relax the
admin controller (mirror the MO2c decision). Owner identity from the token; **`OwnerUserId` is the token owner, never the
body**; the owner may only touch schedules for **vessels they own**.

## BE — owner maintenance endpoints (additive; reuse the S12 engine)
1. **New owner controller/endpoints** (e.g. `api/v1/service-requests/maintenance-schedules` or an owner route),
   `[Authorize]` participant/service-token (callable by the mobile-bff SA via BffAssertion) — separate from the admin one:
   - **List (owner):** the owner's own schedules — add an **owner-scoped list** (by `OwnerUserId` = token, or by the owner's
     vessels), with `includeInactive`. Extend the list query/repo with an owner filter (additive) or a new
     `GetOwnerMaintenanceSchedules` query.
   - **Upsert (owner):** reuse the upsert command but **set `OwnerUserId` from the token** (ignore any body value) and
     **verify the `VesselId` belongs to the owner** (call the Vessel module — the owner's vessel set) → reject otherwise.
     Keep the idempotent-by-key + active-unique behaviour.
   - **Set-active (owner):** reuse the set-active with an **owner-ownership gate** (the schedule's vessel is the owner's) +
     the existing reactivate-conflict guard.
   - Do **not** modify the admin controller/handlers.
2. **Mobile BFF:** passthroughs — owner list, owner upsert, owner set-active; owner identity via BffAssertion; **enforce
   vessel-ownership** (reuse `IVesselRemoteCall` to confirm the vessel is the owner's, or rely on the module owner-gate);
   typed, envelope-correct.

## FE — maintenance self-service (`inktavia-marine-mobile`)
- A **Maintenance** screen (per vessel and/or a combined list): the owner's schedules — service category, interval,
  reminder lead, **last performed**, **next due** (highlight overdue / due-soon), active badge; loading/empty/error.
- **Create/edit** a schedule: **vessel picker limited to the owner's own vessels** + service-category picker (reference
  lookups) + interval (months) + reminder lead (days) + notes; client-validate (interval ≥ 1, lead ≥ 0). Save → owner
  upsert (idempotent by vessel+category); surface the reactivate-conflict / duplicate business error verbatim.
- **Deactivate / reactivate** toggle. tr+en; mock parity.
- Note: the actual **reminder** ("maintenance due") is an N2 notification → surfaced in MO9's notifications.

## Don't-break / QA
- Additive: new owner-facing endpoints (no admin relaxation, no SA admin grant) + owner list filter + BFF passthroughs + FE.
  The S12 engine (upsert/set-active/active-unique/N2 job) and the admin controller are **unchanged**. Owner-scoped:
  `OwnerUserId` from token; **vessel-ownership enforced** (an owner cannot schedule someone else's vessel). Builds clean; FE
  tsc+lint; tr+en.
- Tests: (1) owner lists only their own schedules; (2) owner upsert sets OwnerUserId from the token (a body value is
  ignored) and is rejected for a vessel the owner doesn't own; (3) idempotent upsert by (vessel,category,type); (4)
  deactivate stops N2 (existing) / reactivate-conflict → clean error; (5) the admin endpoints + S12 engine unchanged;
  (6) a non-owner/other-vessel attempt is rejected.

## Report
`docs/V1.0.1/Mobile/Owner/REPORT_BE_MO8_MAINTENANCE.md`: the new owner-facing (non-admin) maintenance endpoints (OwnerUserId
from token + vessel-ownership gate, admin controller untouched), the owner list filter, the BFF passthroughs, the FE
maintenance screen, and the tests. Note the reminder itself is N2 (surfaced in MO9). Then **MO9** (notifications & realtime —
the owner event set + push + preferences).
