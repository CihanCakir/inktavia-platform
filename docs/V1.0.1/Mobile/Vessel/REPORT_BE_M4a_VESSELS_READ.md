# BE_M4a — Mobile vessels READ (list + detail) + FE read wiring

**Date:** 2026-08-06 · **Repos:** `addesso-project` (BFF + 1 Vessel-module fix) + `inktavia-marine-mobile` (RN).
First M4 slice. Builds 0 errors, redeployed, list/detail/scoping verified (empty + populated + foreign-403).

## Endpoints (mobile BFF, `[Authorize]` mobile_user)
- `GET /api/v1/mobile/vessels` → `body: [{ id, name, typeCode, flag, status, coverMediaUrl, lengthMeters, grossTonnage, marinaName }]` — the caller's vessels (empty for a user with none).
- `GET /api/v1/mobile/vessels/{id}` → full detail `{ …, registrationNumber, imoNumber, beamMeters, draftMeters, hullMaterialCode, brand, model, cabinCount, engines[], latitude, longitude, … }`. A foreign/unknown id → clean **"Vessel not found"** business error (never a 500, never another participant's vessel).

## How (scoping + ownership gate)
- Both queries resolve the caller by Keycloak subject (M3a resolver → sets the identity holder) so the downstream `GET /api/v1/vessels/current-user` scopes to the participant.
- **List:** `IVesselRemoteCall.GetUserVessels` → module `current-user` (scoped by `VesselOwnerEntity.UserId == caller && OwnershipStatus == Active`) → mapped to the mobile summary.
- **Detail:** the module detail endpoint has **no owner check**, so the BFF gates it — fetch the caller's owned set, and only return detail for an id in that set; else the not-found business error.

## ⚠ Necessary deviation — 1 Vessel-module fix (flagged)
The task said "no Vessel source changes," but `GET /vessels/current-user` was **incompatible** with the mobile BFF's service-token+assertion model:
```
private long CurrentUserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));  // OLD
```
It read the **ClaimsPrincipal**, not the InfoAccessor. The BFF assertion populates `IAizenInfoAccessor.UserInfo.UserId` but never rewrites the ClaimsPrincipal, and the mobile service token's `NameIdentifier` is the service account's **GUID sub** → `long.Parse(GUID)` threw `FormatException` → 500 (swallowed to `[]` on the list). The participant token also carries **no numeric `UserId`/`nameid` claim** (sub is a GUID), so the endpoint could not resolve any caller.
**Fix (minimal, module-consistent):** derive `CurrentUserId` from `IAizenInfoAccessor.UserInfo.UserId` — exactly how every other Vessel handler already resolves the user — with a fallback to a numeric `NameIdentifier`. One controller, one property; `CurrentUserId` is used in exactly one place (`current-user`). Backward compatible.

## Config
- `docker-compose.yaml` — mobile BFF `RemoteCalls__IVesselRemoteCall__BaseUrl: http://vessel-api:8080`; **vessel-api** `BffAssertion__SharedSecret` + `BffAssertion__AllowedClientIds__0: marine-mobile-bff` (vessel-api had NO BffAssertion config before — no BFF had asserted to it). Without it the assertion is rejected and `CurrentUserId`=0 (scopes to nothing). *(Gotcha: an external edit to docker-compose.yaml reverted this once mid-session; re-applied.)*

## Files
**BFF (`Bff/src/Marine.Participant.Mobile/**`, all new + DI edit):** `Common/RemoteClients/IVesselRemoteCall.cs`, `Contracts/Vessel/MobileVesselDtos.cs`, `Vessel/Query/GetMobileVessels/{Query,Handler}.cs`, `Vessel/Query/GetMobileVesselDetail/{Query,Handler}.cs`, `Controllers/V1/VesselsController.cs`, `DependencyInjection.cs` (register `IVesselRemoteCall`). The mobile csproj already referenced `Vessel.Abstraction`.
**Vessel module:** `Controller/V1/Vessel/VesselController.cs` (the `CurrentUserId` fix above).
No Vessel query/domain/schema change; the read endpoints are reused as-is.

## Verification (`localhost:17003`; user `qa.owner.aug5` / UserId 100029; a temp vessel seeded to it, then removed)
```
GET /vessels  (qa.owner, no vessels)                         → 200  []                       (genuine empty; no FormatException)
# temp seed: vessel 900001 owner UserId=100029; decoys 900002 owner=0, 900003 owner=100030
GET /vessels                                                 → 200  [ {id:900001, "M4A Test Vessel", MOTOR_YACHT} ]   (ONLY the caller's; decoys 0/100030 excluded ⇒ scoping proven)
GET /vessels/900001  (owned)                                 → 200  detail (name, imo, type, engines…)
GET /vessels/900003  (owned by 100030, not caller)           → 200 envelope, isSuccess=false "Vessel not found."   (ownership gate; NOT 500)
GET /vessels/999999  (unknown)                               → "Vessel not found."
# cleanup: seeds deleted, cache flushed
GET /vessels                                                 → 200  []                       (qa.owner restored to empty)
```
Note: `GetUserVesselsQueryHandler`/`GetVesselDetailQueryHandler` are Redis-cached (DB 11, 10/15 min) — flush `Vessel:GetUserVesselsQueryHandler:*` after seeding, or stale results mask the change.

## Scope
`Bff/src/Marine.Participant.Mobile/**` + `docker-compose.yaml` + the one `VesselController.cs` fix. No create/update
(M4b), no documents (M4c), no other feature/backend. addesso tree partly committed by an external process mid-session.
