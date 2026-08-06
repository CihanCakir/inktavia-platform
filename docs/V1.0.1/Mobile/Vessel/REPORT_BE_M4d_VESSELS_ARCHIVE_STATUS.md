# REPORT — BE_M4d_VESSELS_ARCHIVE_STATUS (archive/restore/status + FE archive action + FE-local active vessel)

**Goal:** expose the Vessel module's existing archive/restore/status handlers on the mobile BFF (M4b/M4c orchestration pattern), wire the FE archive/status action, and implement "active vessel" as a FE-local persisted choice (no backend anchor, per the gap doc).
**Status:** BFF done + **live-verified** (archive→list-drop fresh w/o flush, restore→return, status→detail+list fresh, ownership-gated clean not-found). FE archive/status options sheet + FE-local persisted active-vessel wired; `npx tsc --noEmit` = 0. Full RN device run blocked headless (expo-secure-store, per prior slices) — BFF contracts drive the already-wired reads.
**Scope:** `Bff/src/Marine.Participant.Mobile/**` (+ vessel remote-call) + one **1-line** module fix (status-change list invalidation, justified below) + `inktavia-marine-mobile/src/features/vessels/**` (+ the active-vessel store, endpoints, mock, Home card & switcher that consume it). No documents (M4e), media (M4f), other feature / SR-Payment WIP.

---

## 1. Endpoints exposed (BFF → module handlers)

All owner-gated, `[Authorize(ParticipantAuthenticated)]`, resolve-by-subject → assertion (module handlers scope via `IAizenInfoAccessor.UserInfo.UserId`, unchanged from M4a/b/c). Each returns the **re-read `MobileVesselDetailDto`** (never a re-scoped list — M4b lesson) so the FE mirrors exactly what persisted.

| Mobile BFF | Module handler | Notes |
|---|---|---|
| `POST /api/v1/mobile/vessels/{id}/archive` | `PATCH /vessels/{id}/archive` → `ArchiveVesselCommandHandler` | Soft (no hard delete). Body optional `{ reason?, notes? }`; reason is a `VesselArchiveReason` name (defaults `Other`). Returns detail `isArchived=true`. |
| `POST /api/v1/mobile/vessels/{id}/restore` | `PATCH /vessels/{id}/restore` → `RestoreVesselCommandHandler` | No body. Returns detail `isArchived=false`; vessel re-enters the active list. |
| `PUT /api/v1/mobile/vessels/{id}/status` | `PATCH /vessels/{id}/status` → `UpdateVesselStatusCommandHandler` | All-optional body `{ status?, reason? }`. **Status IS user-settable** — see §2. |

- **Ownership gate (clean not-found):** each handler first checks the caller's **default-page** owned set (`GetUserVessels(0,20)` — the same key writes invalidate); a foreign/unknown id → `"Vessel not found."` business error, never a 500, never another owner's vessel. (Archived vessels stay in that unfiltered module read, so the gate resolves restore of an archived id too — it is only the mobile *list projection* that hides archived rows.)
- **All-optional inputs (M4c gotcha):** the mobile request DTOs are all-optional (`ArchiveMobileVesselRequest`, `UpdateMobileVesselStatusRequest`). Status crosses to the module as a parsed `VesselStatus` name; a bare archive with no reason never 400s on required-field validation.
- **New remote calls** (`IVesselRemoteCall`): `ArchiveVessel(id, req)`, `RestoreVessel(id)`, `UpdateStatus(id, req)` — all `PATCH`.

---

## 2. Status — SETTABLE (operational states only)

Vessel status **is user-settable**, restricted to the module's valid-transition graph (`VesselStatusService.IsValidTransition`): the reachable states are **Active / Passive / UnderMaintenance** (Draft is the initial state; Sold has no incoming edge; Archived is owned by the archive endpoint). So the BFF setter accepts only `Active | Passive | UnderMaintenance` and rejects the archive-/system-owned states up front with a clean business error (rather than letting them 500 in the module). The module still enforces the full transition graph (e.g. Active→Active is rejected).

FE status strings map 1:1 to the enum names (`Active`/`Passive`/`UnderMaintenance`); the picker labels them Active/Passive/Maintenance (tr: Aktif/Pasif/Bakımda).

---

## 3. Active vessel — FE-LOCAL persisted (no backend field)

Per the gap doc (§0.5, capability #11): there is **no backend anchor** for an active/primary-per-vessel flag, so "active vessel" is a **FE-local persisted preference**, not a backend field.

- **Store** (`src/features/vessels/state/activeVesselStore.ts`): a zustand store persisting a `{ userId → vesselId }` map to **expo-secure-store** (the app has no AsyncStorage dep; secure-store is the existing token store). Keyed per user via `authStore.user?.id`.
- **`useActiveVessel()` hook:** resolves the effective active vessel = persisted choice **if still present in the live list**, else the **first** vessel, else null. Because the list already excludes archived (BFF, §4), a persisted choice that was archived/restored-away/deleted **auto-falls-back to the first remaining vessel** with no crash — the required graceful fallback comes for free.
- **Consumers:** the top-bar `VesselSwitcher` (every instance — Home, Services, Profile, Vessels list) now reads/writes the store (was ephemeral `useState`, so selection didn't survive navigation); the **Home `ActiveVesselCard`** reads `activeVessel` from it (was hard-coded `vessels[0]`). Selecting a vessel in the picker persists it; relaunch restores it.

No backend field added; N/A for cross-device sync (flagged for product if ever wanted).

---

## 4. Archive drops from the active list (BFF projection)

The module `GetUserVesselsQueryHandler` filters **only by ownership** — it does **not** exclude archived vessels (they stay in the read model with `IsArchived=true`). So the "active list" semantics live in the BFF: `GetMobileVesselsQueryHandler` now returns `items.Where(v => !v.IsArchived)`. Archive + restore both invalidate the module's default list page (0,20), so the transition shows immediately. `MobileVesselDetailDto` gained `IsArchived` + `ArchivedAt` so the FE detail/options-sheet know the state.

---

## 5. The 1-line module fix (deliberate, justified deviation)

`ArchiveVesselCommandHandler` / `RestoreVesselCommandHandler` already invalidate **both** the vessel detail **and** the user list page. `UpdateVesselStatusCommandHandler` invalidated only detail + status-history — so a status change reflected in **detail** immediately but the **list/Home status badge stayed stale to TTL** (the list item carries `Status`). The task requires "list/detail/Home reflect immediately," and this cannot be fixed from the BFF (no cross-service cache access). Fix = one line mirroring the sibling handlers:

```csharp
await _invalidation.InvalidateUserVesselListAsync(currentUserId, cancellationToken);
```

This is the sole module edit — minimal, low-risk, consistent with Archive/Restore. Verified below (list badge now fresh).

---

## 6. Verification — live (`localhost:17003`; `qa.owner.aug5`; **no DB11 flush**; both images rebuilt+redeployed)

```
baseline  GET /mobile/vessels → count=6  (100014,100013,…100009)

1) POST /mobile/vessels/100014/archive  {reason:Sold,notes:…}
   → ok=true  id=100014 isArchived=true archivedAt=2026-08-06T14:29:14Z status=Archived
   GET /mobile/vessels → count=5   ← 100014 DROPPED, fresh, no flush ✅

2) POST /mobile/vessels/100014/restore
   → ok=true  id=100014 isArchived=false status=Passive
   GET /mobile/vessels → count=6   ← 100014 BACK ✅  (module Restore resets Archived→Passive)

3) POST /mobile/vessels/20001/archive  (another user's vessel)
   → ok=false errCode=9999 ("Vessel not found.")   ← clean not-found, no 500; 20001 untouched ✅

4) PUT /mobile/vessels/100013/status {status:Active}   → ok=true detail-status=Active
   GET /mobile/vessels/100013 (detail) → status=Active ✅
   (pre-module-fix: list showed stale "Draft")
   after 1-line module fix + vessel-api redeploy:
   PUT …/100013/status {status:UnderMaintenance} → detail=UnderMaintenance
   GET /mobile/vessels → 100013 status=UnderMaintenance   ← list badge FRESH immediately ✅

5) PUT /mobile/vessels/100013/status {status:Archived}  (not user-settable)
   → ok=false errCode=9999   ← rejected cleanly at the BFF, never reaches module ✅
```

BFF Application + Web builds: **0 errors**. Vessel module build: **0 errors**. Both `bff-marine-mobile` + `vessel-api` images rebuilt + redeployed healthy.

---

## 7. FE — archive/status + active-vessel (`inktavia-marine-mobile`)

- **`VesselOptionsSheet`** (new) — bottom sheet opened from the detail-screen header (`⋮`, previously a no-op `settings` button). Not-archived → operational-status segment (Active/Passive/Maintenance) + archive-reason chips + Archive button (RN `Alert.alert` confirm → archive). Archived → Restore only. On any success: invalidate `vessels.detail(id)` + `.list()` + `.all`; archive additionally pops back (the vessel left the active list). Loading/error via the shared `useToast`.
- **`VesselDetailScreen`** — header button opens the sheet; `onArchived → navigation.goBack()`.
- **Active vessel** — `VesselSwitcher` + Home `ActiveVesselCard` now read the FE-local persisted store (`useActiveVessel`); selection persists (secure-store) and survives relaunch; archived/deleted active → first-remaining fallback.
- **Write API** (`vesselsWriteApi`): `archiveVessel(id,{reason?,notes?})` (POST), `restoreVessel(id)` (POST), `updateVesselStatus(id,status,reason?)` (PUT). `VesselDetailApi` gained `isArchived?`/`archivedAt?`. Endpoints `ARCHIVE`/`RESTORE`/`STATUS` added.
- **Mock parity** — RegExp handlers for `POST …/archive` (marks detail archived + drops from `M4A_VESSELS`), `POST …/restore` (re-adds), `PUT …/status` (updates detail+list item), so mock-ON mirrors the BFF; mock-OFF hits the BFF.
- **i18n** — tr + en keys for the manage sheet / status / archive-reason / toasts (fallback strings inline regardless).
- `npx tsc --noEmit` → **0**.

---

## 8. Files (isolated diff)

**BE — BFF** (`Bff/src/Marine.Participant.Mobile/**`):
- `Application/Contracts/Vessel/MobileVesselDtos.cs` — `ArchiveMobileVesselRequest`, `UpdateMobileVesselStatusRequest` (all-optional); `IsArchived`/`ArchivedAt` on detail.
- `Application/Common/RemoteClients/IVesselRemoteCall.cs` — `ArchiveVessel`/`RestoreVessel`/`UpdateStatus` (PATCH).
- `Application/Vessel/Command/{ArchiveMobileVessel,RestoreMobileVessel,UpdateMobileVesselStatus}/…Command.cs + …CommandHandler.cs` *(new)*.
- `Application/Vessel/MobileVesselMapper.cs` — map `IsArchived`/`ArchivedAt`.
- `Application/Vessel/Query/GetMobileVessels/GetMobileVesselsQueryHandler.cs` — exclude archived from the active list.
- `Controllers/V1/VesselsController.cs` — `POST /{id}/archive`, `POST /{id}/restore`, `PUT /{id}/status`.

**BE — module (1-line fix, §5):**
- `Modules/Vessel/src/Aizen.Modules.Vessel.Application/Command/Vessel/UpdateVesselStatus/UpdateVesselStatusCommandHandler.cs` — add `InvalidateUserVesselListAsync`.

**FE** (`inktavia-marine-mobile`):
- `src/features/vessels/state/activeVesselStore.ts` *(new)*, `src/features/vessels/components/VesselOptionsSheet.tsx` *(new)*.
- `src/features/vessels/screens/VesselDetailScreen.tsx`, `src/features/vessels/api/{vesselsReadApi,vesselsWriteApi}.ts`, `src/shared/components/ui/VesselSwitcher.tsx`, `src/features/home/screens/HomeScreen.tsx`, `src/core/api/endpoints.ts`, `src/core/mock/handlers/vessels.handlers.ts`, `src/core/i18n/locales/{en,tr}.json`.

**Not touched:** documents/media, other features. (Pre-existing unrelated working-tree edits in `Modules/ServiceRequest/**/GetProviderJobDetail*` are NOT from this slice.) Isolated commits (BE + module + FE).
