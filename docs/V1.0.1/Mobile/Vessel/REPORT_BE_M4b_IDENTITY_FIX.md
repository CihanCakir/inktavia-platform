# REPORT — BE_M4b_IDENTITY_FIX (mobile vessel create↔read loop)

**Goal:** a mobile `POST /mobile/vessels` succeeds and the created vessel appears immediately in `GET /mobile/vessels` (cache fix e1899af already in), with no false-failure from the create wrapper.
**Status:** fixed + **live-verified** end-to-end (bff-marine-mobile rebuilt/redeployed; `qa.owner.aug5`). FE `tsc --noEmit` = 0, no FE change required.
**Scope:** mobile BFF vessel create/read path only (4 files). No module / FE / UoW / cache changes.

---

## 1. Diagnosis — the divergence was **page size**, NOT identity

The kickoff (and my own note in the cache-fix report) hypothesized a *create-owner-id vs read-scope-id* identity split. **Tracing it live disproved that** — the identities already agree:

| Path | Id used | Source |
|---|---|---|
| CREATE owner | `UserId = 100029` | module `CreateVesselCommandHandler` → `_info.UserInfoAccessor.UserInfo.UserId` (BFF assertion via resolve-by-subject) |
| READ scope | `UserId = 100029` | module `VesselController.CurrentUserId` → same `_info.UserInfoAccessor.UserInfo.UserId` |

`qa.owner.aug5` = `UserProfiles.Id (ProfileId) 100030, UserId 100029`. The resolver sets `holder.Set(UserId=100029, ProfileId=100030)`; both create and read assert the same `UserId=100029`. Proof the identity round-trips: a `GET /mobile/vessels` returns exactly the vessels created for `100029` — a split identity could never show them.

**The real root cause — read page size ≠ invalidation page size.** The read result is cached per `(UserId, PageIndex, PageSize)` tuple, and `InvalidateUserVesselListAsync` (post cache-fix) evicts only the **default page** `(…, 0, 20)`. But the mobile BFF read with **non-default sizes**:

| Mobile BFF caller | Requested page | Physical key evicted by create? |
|---|---|---|
| `GetMobileVesselsQueryHandler` (list) | `GetUserVessels(0, 100)` | ❌ `PageSize_100` — never evicted |
| `GetMobileVesselDetailQueryHandler` (ownership gate) | `GetUserVessels(0, 200)` | ❌ `PageSize_200` — never evicted |
| `CreateMobileVesselCommandHandler` (post-create re-read) | `GetUserVessels(0, 200)` | ❌ `PageSize_200` — never evicted |

So after a create, the `PageSize_100/200` cached pages stayed stale → the list didn't show the new vessel until TTL, and the wrapper's `(0,200)` re-read returned a stale page whose name-match failed → **false "Vessel could not be created."** (My earlier "identity split" was a mis-read: I compared the read's `(100029,0,100)` cache key against a `(100029,0,20)` key and wrongly blamed the UserId.)

**Live confirmation of the page-size cause (before the fix):**
```
clean cache → GET list(100) = 3 vessels (cached under …PageSize_100…)
POST create #4 → wrapper ok=TRUE, id=100012
immediate GET list(100) = STILL 3  ← STALE: create evicted PageSize_20, not PageSize_100
```
The 3 existing vessels showing correctly proves identity is fine; only the *new* one lagged, purely due to the page-key mismatch.

---

## 2. Fix (minimal, BFF-only, identity untouched)

**A) Align every mobile `GetUserVessels` read to the module's DEFAULT page `(0, 20)`** — the exact key the write path invalidates — so create/update/archive are reflected immediately:
- `GetMobileVesselsQueryHandler`: `GetUserVessels(0, 100)` → `(0, 20)`.
- `GetMobileVesselDetailQueryHandler` (ownership gate): `GetUserVessels(0, 200)` → `(0, 20)`.

A participant owns a handful of vessels (**observed max across the DB = 4**), so the default page is the entire owned set; the previous 100/200 were themselves arbitrary bounds. Broader ownership would need real pagination + a wider/looped invalidation (noted §5).

**B) Fix the create-wrapper false-failure — resolve the committed id deterministically, not via a re-scoped list.** The module builds its create DTO *before* the UoW commit, so `Id = 0` in the response. Instead of re-reading the caller's paged list and matching by name (the stale-page trap), recover the id from the just-created vessel's globally-unique **`VesselCode`** (present in the create response), which a brand-new code makes a guaranteed cache-miss → fresh DB read → real id:
```csharp
var vesselId = created.Id;
if (vesselId <= 0 && !string.IsNullOrWhiteSpace(created.VesselCode))
    vesselId = (await _vessel.GetVesselByCode(created.VesselCode))?.Body?.Vessel?.Id ?? 0;
```
New `IVesselRemoteCall.GetVesselByCode` → module `GET /api/v1/vessels/code/{vesselCode}`. The wrapper then returns the created vessel by id via the existing `GetVesselDetail` projection (spec/engine reflected). No list dependency remains in the create path.

Identity resolution (resolve-by-subject → assertion → `InfoAccessor.UserInfo.UserId`) is unchanged.

---

## 3. Verification — the full loop (live; `localhost:17003`; `qa.owner.aug5`; **no DB11 flush**)

```
[A] baseline GET /mobile/vessels                → 4 vessels
[B] POST /mobile/vessels {core+spec+engine, "FinalLoop 13208"}
                                                → 200  wrapper ok=TRUE  id=100013  name/flag correct  (no false failure)
[C] IMMEDIATE GET /mobile/vessels               → 5 vessels, "FinalLoop 13208" PRESENT ✅ (fresh, no flush)
[D] GET /mobile/vessels/100013 (gated BFF)      → ok, cabins=2, engines=1  (spec+engine persisted; gate passes)

back-to-back 2nd create "FinalLoop2" (old wrapper false-failed here on repeat)
                                                → 200 ok=TRUE id=100014;  list=6, present ✅
ownership gate: GET /mobile/vessels/20001 (another user's vessel)
                                                → "Vessel not found." (no leak) ✅
```

**FE:** BFF response contracts are unchanged (only internal id-resolution + read page size changed), so the already-wired wizard / Home active-vessel card / top-bar picker consume the now-fresh endpoints unchanged. `npx tsc --noEmit` on `inktavia-marine-mobile` = **0**, working tree clean (no FE edit). Full on-device RN run remains blocked headless (expo-secure-store, per prior mobile slices).

---

## 4. Files touched (isolated diff)
- `…/Common/RemoteClients/IVesselRemoteCall.cs` — add `GetVesselByCode`.
- `…/Vessel/Command/CreateMobileVessel/CreateMobileVesselCommandHandler.cs` — id-by-VesselCode; drop the list re-read.
- `…/Vessel/Query/GetMobileVessels/GetMobileVesselsQueryHandler.cs` — read `(0, 20)`.
- `…/Vessel/Query/GetMobileVesselDetail/GetMobileVesselDetailQueryHandler.cs` — ownership-gate read `(0, 20)`.

No module, FE, UoW, cache, or SR/Payment-WIP changes.

## 5. Follow-ups (out of scope)
- **Pagination for >20 owned vessels:** the mobile list + ownership gate now read only the default page (ample for real users, max 4 observed). A true "many vessels" experience needs real pagination and either a looped/wildcard invalidation or a dedicated **is-owner** module check for the gate (instead of a page-scan).
- The cache-fix report's "identity split" note is corrected here and in memory: the blocker was page-size, not identity.
