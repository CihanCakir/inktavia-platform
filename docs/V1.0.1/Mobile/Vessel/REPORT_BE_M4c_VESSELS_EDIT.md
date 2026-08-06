# REPORT — BE_M4c_VESSELS_EDIT (participant vessel update)

**Goal:** expose + orchestrate participant vessel UPDATE on the mobile BFF (mirroring the M4b create composite), and wire the FE edit affordance. Reuse the module's existing update handlers — do not rebuild.
**Status:** BFF done + **live-verified** (core + spec + engine persist; immediate list freshness; ownership-gated). FE edit screen wired; `tsc --noEmit` = 0; mock-ON parity handler added. Full RN device run blocked headless (per prior slices).
**Scope:** `Bff/src/Marine.Participant.Mobile/**` + `inktavia-marine-mobile/src/features/vessels/**` (+ the navigation registration and mock handler the edit screen requires). No archive/status (M4d), documents (M4e), media (M4f). No other feature / SR-Payment WIP.

---

## 1. Endpoints orchestrated (BFF composite → module handlers)

`PUT /api/v1/mobile/vessels/{id}` → `UpdateMobileVesselCommandHandler`, which composes the module's existing owner-gated writes (the gap-doc M4c set):

| Step | Module endpoint | Handler | Notes |
|---|---|---|---|
| 1. Core (required) | `PUT /api/v1/vessels/{id}` | `UpdateVesselCommandHandler` | Full update — the BFF **merges** partial input over current values so unspecified fields are never nulled. |
| 2. Spec (optional) | `PUT /api/v1/vessels/{id}/specification` | `UpsertVesselSpecification…` | Merged over current spec; length/beam/draft unit codes preserved (default `METER`). |
| 3. Engine (optional) | `PUT /api/v1/vessels/{id}/engines/{engineId}` | `UpdateVesselEngineCommandHandler` | Updates the **primary/existing** engine (id from the detail read); adds one if the vessel has none. |

- **Auth:** `[Authorize(ParticipantAuthenticated)]`; resolve-by-subject → assertion; the module handlers scope via `IAizenInfoAccessor.UserInfo.UserId` (identity unchanged from M4a/M4b).
- **Ownership gate (clean not-found):** the handler first checks the caller's **default-page** owned set (`GetUserVessels(0,20)`); a foreign/unknown id → `"Vessel not found."` — never a 500, never another owner's vessel.
- **Patch semantics:** update uses dedicated all-optional inputs (`UpdateMobileVesselCoreInput` / `MobileVesselSpecInput` / `UpdateMobileVesselEngineInput`). (Reusing the create inputs 400'd — their non-nullable `VesselTypeCode` / `EngineTypeCode` / `FuelTypeCode` are implicitly *required* by model validation, which blocks partial payloads.)
- **Return by id, never a re-scoped list (M4b lesson):** after the writes it returns the re-read detail projection (`GetVesselDetail(id)` → `MapDetail`). The module invalidates the vessel + the **default list page (0,20)** — the exact key the mobile reads — so detail/list/Home refresh immediately.

**New remote calls** (`IVesselRemoteCall`): `UpdateVessel(id, UpdateVesselRequest)`, `UpdateEngine(id, engineId, UpdateVesselEngineRequest)`.

---

## 2. Verification — live (`localhost:17003`; `qa.owner.aug5`; vessel `100013`; **no DB11 flush**)

```
baseline  GET /mobile/vessels/100013 → name="FinalLoop 13208" type=MOTOR_YACHT cabins=2 length=18.0 engHP=250 engFuel=DIESEL

PUT /mobile/vessels/100013
  { core:{name:"Edited 7697", vesselTypeCode:MOTOR_YACHT}, spec:{cabinCount:7, lengthMeters:30.5},
    engine:{engineTypeCode:INBOARD, fuelTypeCode:GASOLINE, horsePower:999} }
  → 200  returns name="Edited 7697" cabins=7 length=30.5 engHP=999 engFuel=GASOLINE

detail re-read  GET /mobile/vessels/100013
  → name="Edited 7697" cabins=7 length=30.5 engHP=999 engFuel=GASOLINE   ← CORE + SPEC + ENGINE all persist ✅

list re-read (no flush)  GET /mobile/vessels
  → item 100013 name="Edited 7697"   ← FRESH immediately (page-scope-correct invalidation) ✅
```

**Patch semantics** — `PUT { spec:{ cabinCount:9 } }` (core & engine omitted):
```
→ 200  name="Edited 7697" (unchanged)  cabins=9  length=30.5 (unchanged)  engHP=999 (unchanged)   ✅ others preserved
```

**Ownership gate** — `PUT /mobile/vessels/20001` (another user's vessel) `{ core:{ name:"HACK" } }`:
```
→ ok=false  "Vessel not found."   and DB confirms vessel 20001 name still "BlueOctopus" (unchanged) ✅
```

BFF build: **0 errors**. Image rebuilt + redeployed.

---

## 3. FE — edit wiring (`inktavia-marine-mobile`)

- **`EditVesselScreen`** (new) — one flat form reusing the Add-Vessel field components (`FormField`/`TextInput`/`SelectInput`) + the **M3b lookups** (`VESSEL_TYPE`/`HULL_MATERIAL`/`ENGINE_TYPE`/`FUEL_TYPE`) + **countries** (flag), prefilled from `useVessel(id)`. Submits the `{ core, spec, engine }` shape via `updateVessel(id, …)` (`PUT /mobile/vessels/{id}`) with `useMutation`; on success invalidates `queryKeys.vessels.detail(id)` + `.list()` + `.all` (Home card reads the list) and returns to detail. Loading/error via the shared feedback components (`Loading`/`ErrorState`/`useToast`).
- **`VesselDetailScreen`** — the Edit FAB (previously a no-op stub) now navigates to `EditVessel` with `vesselId`.
- **Navigation** — registered `EditVessel: { vesselId }` (`VesselsNavigator` + `types.ts`) — required to host the edit screen.
- **`vesselsWriteApi.updateVessel`** — `PUT` mirroring `createVessel`; `UpdateVesselInput = Partial<CreateVesselInput>`.
- **Mock parity** — `PUT /mobile/vessels/{id}` RegExp handler merges `{ core, spec, engine }` over the current detail (mirrors the BFF patch) so mock-ON reflects the edit in detail + list + Home.
- `npx tsc --noEmit` → **0**. (Full RN device run blocked headless — expo-secure-store — per prior mobile slices; BFF response contracts drive the already-wired detail/list/Home reads.)

---

## 4. Files (isolated diff)
**BE** (`Bff/src/Marine.Participant.Mobile/**`):
- `Application/Contracts/Vessel/MobileVesselDtos.cs` — `UpdateMobileVesselRequest` + all-optional `UpdateMobileVesselCoreInput` / `UpdateMobileVesselEngineInput`.
- `Application/Vessel/Command/UpdateMobileVessel/UpdateMobileVesselCommand.cs` + `…CommandHandler.cs` *(new)*.
- `Application/Common/RemoteClients/IVesselRemoteCall.cs` — `UpdateVessel` + `UpdateEngine`.
- `Controllers/V1/VesselsController.cs` — `PUT {vesselId}`.

**FE** (`inktavia-marine-mobile`):
- `src/features/vessels/screens/EditVesselScreen.tsx` *(new)*, `…/screens/VesselDetailScreen.tsx` (FAB), `…/api/vesselsWriteApi.ts` (`updateVessel`).
- `src/app/navigation/VesselsNavigator.tsx` + `types.ts` (register EditVessel), `src/core/mock/handlers/vessels.handlers.ts` (PUT mock).

No archive/status/documents/media, no module changes, no other feature.
