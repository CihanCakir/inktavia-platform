# REPORT — BE_M4b Vessels Create (mobile BFF orchestration + FE wizard)

**Goal:** expose + orchestrate vessel **create** on the mobile BFF (module already had create/set-spec/set-engine handlers), and fix the FE wizard that collected basic/technical/engine/documents but sent only `basicInfo` to an in-memory mock. Source of truth: `docs/V1.0.1/Mobile/Vessel/VESSEL_FE_BE_GAP.md`.

**Result:** live-verified. `POST /api/v1/mobile/vessels` → **200**, persists **exactly once** (vessel + owner + spec + engine, one row each), `GET detail` reflects spec + engine, flag stored as a country code. FE wizard rewired off the mock; `tsc --noEmit` = 0.

> The platform-wide **UoW double-save** fix this slice depends on is reported separately in `docs/V1.0.1/Mobile/Core/REPORT_UOW_DOUBLE_SAVE_FIX.md` (isolated Core commit). This report covers only the vessel FE/BFF/module changes.

---

## BFF — `Bff/src/Marine.Participant.Mobile/**`

- **`POST /api/v1/mobile/vessels`** (`VesselsController`) → `CreateMobileVesselCommand`.
- **`CreateMobileVesselCommandHandler`** orchestrates the module's existing handlers as one logical create:
  1. `POST /api/v1/vessels` (create-core; the asserted caller becomes PrimaryOwner),
  2. `PUT /api/v1/vessels/{id}/specification` (optional; only when a spec field was captured),
  3. `POST /api/v1/vessels/{id}/engines` (optional; only when engine type + fuel were chosen),
  then returns the assembled `MobileVesselDetailDto` (reuses the M4a detail projection via the extracted `MobileVesselMapper`).
- **Payload** `CreateMobileVesselRequest { core, spec, engine }` (`Contracts/Vessel/MobileVesselDtos.cs`). Length/beam/draft are metres → unit code defaulted to `METER` server-side; engine name defaults to "Main Engine".
- **Create-core → real-Id resolution:** the module builds `CreateVesselResponse` from the entity **before** the deferred UoW commit, so the returned `Id` is `0` (identity assigned at commit). The row *is* committed by the time the response returns, and create invalidates the list cache — so the handler resolves the real Id by re-reading the caller's vessels and matching on `Name` (name → `Slug` is globally unique ⇒ exactly one match), then drives spec/engine/detail off that Id. No module change needed for this.
- Scoping: `ParticipantProfileResolver` sets the identity holder → every downstream call is asserted as the participant; spec/engine writes pass the module's owner-gated `EnsureCanEditAsync`.
- **Wiring gaps M4a had left (fixed here — the vessel calls could not reach vessel-api before):** registered `IVesselRemoteCall` in `DependencyInjection.cs`; added `RemoteCalls__IVesselRemoteCall__BaseUrl` to the `bff-marine-mobile` compose service (+ `depends_on: vessel-api`) and to the BFF `configuration/appsettings*.json`. (vessel-api's `BffAssertion__AllowedClientIds = [marine-mobile-bff]` already existed.)

## Module — `Modules/Vessel/**` (two required minimal fixes; both surfaced only because M4b is the first end-to-end create through the endpoint)

1. **Owner FK on create** — `CreateVesselCommandHandler` inserted the owner via a separate repository with a **scalar** `VesselId` captured *before* the vessel was persisted (`entity.Id == 0`), so the single UoW commit violated `FK_vessel_owners_vessels_VesselId`. Fix: attach the owner through the aggregate (`VesselEntity.AddOwner(...)`) so EF assigns the FK from the vessel's generated key on save. Owner row now persists with the correct `VesselId` (verified).
2. **Detail projection dropped sub-entities** — `VesselSnapshotService.BuildDetailAsync` eager-loads the aggregate (`GetByIdWithDetailsAsync` includes spec/engines/docs/media/location/status/owners) but only mapped the **core `Vessel`**, so detail *always* returned null spec / empty engines. Fix: project the full aggregate (Repository can't reference the Application `ToDetailDto` mapper, so mapped inline). Detail now reflects spec + engine (and owners/docs/media/location/status for all consumers).

`VesselController.cs` also carries the M4a `CurrentUserId` InfoAccessor fix (unchanged here).

## FE — `inktavia-marine-mobile`

- **Wizard rewired:** `AddVesselDocumentsScreen` submit now builds the full `{core, spec, engine}` payload from the three accumulated steps and calls the real `POST /mobile/vessels` via `useMutation` (`vesselsWriteApi.ts`), invalidates `queryKeys.vessels.all`, and `navigation.reset`s to `[MyVessels, VesselDetail]` on the new id. Specs/engine are no longer dropped. (`AddVesselDocumentsScreen` file uploads stay unwired — M4e.)
- **Flag → country picker:** `AddVesselBasicInfoScreen` replaced the free-text flag with a `SelectInput` bound to `useCountries()` (`/reference/countries`), so the stored value is a country **code** matching BE `FlagCountryCode`. Type/fuel/engine/hull dropdowns already use the M3b lookups.
- **Mock parity:** the mock `POST /mobile/vessels` handler now accepts `{core, spec, engine}` and returns a full detail (keyed store), so mock-ON completes the wizard and the detail screen renders real data.
- `npx tsc --noEmit` → **0 errors**.

---

## Verification (live; `localhost:17003`; user `qa.owner.aug5@inktavia.com`)

```
POST /mobile/vessels {core:{name, MOTOR_YACHT, flag TR, reg}, spec:{2021,24.5,6.2,1.9,FIBERGLASS,3}, engine:{INBOARD,DIESEL,480}}
 → 200  body: id=100007, flag=TR, len=24.5, beam=6.2, draft=1.9, hull=FIBERGLASS, cabins=3, year=2021,
        engines=[{Main Engine, INBOARD, DIESEL, 480, isPrimary=true}]
GET /mobile/vessels/100007
 → spec {len 24.5, beam 6.2, draft 1.9, FIBERGLASS, cabins 3, year 2021}; engines=[INBOARD/DIESEL/480/primary]
DB exactly-once:  vessels=1  owners=1  specifications=1  engines=1
vessel-api SQL log (one create):  INSERT vessels ×1, vessel_owners ×1, vessel_specifications ×1, vessel_engines ×1
GET /mobile/vessels  → the new vessel present (after cache refresh — see caveat)
```

### Known pre-existing caveat (NOT introduced by M4b) — list read-cache staleness
The vessel module's cache-invalidation service computes its key from `SHA256("UserId_{id}|")`, which does **not** match the framework's actual cacheable-query key (a hash over all query props incl. pageIndex/pageSize). So `InvalidateUserVesselListAsync` never clears the real `GetUserVesselsQueryHandler` entry — the caller's list stays stale for up to the 10-min TTL after a create (M4a already documented the "flush Redis DB 11 after seeding" workaround). Data is correct; only the list read lags. The create→**detail** path (what the FE navigates to) is immediate and correct. Fixing this spans all 10 invalidation methods + the framework key format → separate module-caching task, out of M4b scope. Flushing vessel-api Redis DB 11 makes the list immediately show the new vessel (verified).

### FE runtime
`tsc` clean; the wizard posts the exact `{core,spec,engine}` shape verified against the live BFF above; mock handler updated for mock-ON parity. Full on-device RN run not performed (headless RN limitation, per prior mobile slices).

## Scope / git
BE changes confined to `Bff/src/Marine.Participant.Mobile/**`, `Modules/Vessel/**` (the two required fixes above), the vessel remote-call base URL in `docker-compose.yaml` + BFF appsettings, and these docs. FE changes confined to `src/features/vessels/**`, the flag picker, the vessels mock handler, and the write API. The Core UoW fix is a **separate** diff/commit + report. No edit/archive/documents/media work (M4c–M4f).
