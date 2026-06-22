# Vessel List + Register Final Gap Report

## Summary
All primary tasks from the package have been implemented and the solution builds with 0 errors.

## Implemented

| Feature | Status |
|---------|--------|
| Vessel module list query enriched with primary owner IDs | ✅ Done |
| Vessel module list query enriched with latest location snapshot | ✅ Done |
| `VesselListItemDto` extended with `OwnerUserId`, `OwnerProfileId`, `LastLocationMarinaName` | ✅ Done |
| Identity bulk user profiles endpoint `GET /api/v1/identity/admin/users/profiles/bulk` | ✅ Done |
| BFF `VesselListItemBffDto` extended with label and enrichment fields | ✅ Done |
| BFF list handler refactored as aggregation/enrichment handler | ✅ Done |
| BFF bulk owner enrichment — one Identity call per page, N+1 eliminated | ✅ Done |
| Status/type label mapping in BFF (static dictionaries) | ✅ Done |
| `LastLocationText` computation in BFF (marina name → coordinates fallback) | ✅ Done |
| `GET /api/v1/admin-panel/vessels/register` bootstrap endpoint | ✅ Done — fixes 404 |
| `POST /api/v1/admin-panel/vessels/register` create endpoint | ✅ Done |
| Vessel module admin create endpoint `POST /api/v1/admin/vessels` | ✅ Done |
| Route ordering — literal `/register` before `/{vesselId:long}` | ✅ Done |
| Auth preserved (`AdminPanelAccess`, `Roles = Admin`) | ✅ Done |
| Long IDs throughout — no `Guid` introduced for business entities | ✅ Done |
| Warning-based graceful degradation | ✅ Done |

## Known Remaining Gaps

### Minor / Low Risk

| Gap | Notes |
|-----|-------|
| `CoverMediaUrl` always `null` in vessel list | Requires FileStorage thumbnail enrichment — separate work item |
| `homePorts` in register bootstrap always empty | Needs ReferenceData marina/port lookup endpoint |
| `ownerCandidates` not role-filtered | Loads first 100 general profiles — should filter by boat-owner role context when Identity supports it |
| No FluentValidation on `RegisterAdminVesselBffRequest` | Frontend validation may compensate; add if required |
| `OwnerName` staleness from cache | `GetAllVesselsAdminQueryHandler` has 5-min `IAizenQueryHandlerCacheable` TTL — owner changes may reflect with delay |
| `Email` missing from `OwnerCandidateBffDto` | `UserProfileListItemDto` in Identity does not expose email |

### Security Follow-Up

| Item | Notes |
|------|-------|
| Identity tokens in Redis | Currently no token protection utility used — if session caching is added, protect token values before storage |

## Files Changed Summary

### Modules/Vessel
- `Abstraction/Dto/Vessel/VesselListItemDto.cs` — extended
- `Abstraction/Request/Vessel/CreateAdminVesselRequest.cs` — new
- `Application/Command/Vessel/CreateAdminVessel/CreateAdminVesselCommand.cs` — new
- `Application/Command/Vessel/CreateAdminVessel/CreateAdminVesselCommandHandler.cs` — new
- `Application/Query/Vessel/GetAllVesselsAdmin/GetAllVesselsAdminQueryHandler.cs` — enriched projection
- `Controller/V1/Admin/Vessel/VesselAdminController.cs` — added POST endpoint

### Modules/Identity
- `Application/Common/Query/GetUserProfilesByUserIds/GetUserProfilesByUserIdsQuery.cs` — new
- `Application/Common/Query/GetUserProfilesByUserIds/GetUserProfilesByUserIdsQueryHandler.cs` — new
- `Controller/V1/Identity/QueryController.cs` — added bulk endpoint

### Bff/AdminPanel
- `Application/AdminVessels/Dto/VesselListItemBffDto.cs` — extended with label/enrichment fields
- `Application/AdminVessels/Dto/AdminVesselRegisterBootstrapBffResponse.cs` — new (7 DTOs)
- `Application/AdminVessels/Query/GetAdminVesselListBffQueryHandler.cs` — rewritten as enrichment handler
- `Application/AdminVessels/Query/GetAdminVesselRegisterBootstrapQuery.cs` — new
- `Application/AdminVessels/Query/GetAdminVesselRegisterBootstrapQueryHandler.cs` — new
- `Application/AdminVessels/Command/RegisterAdminVesselBffCommand.cs` — new
- `Application/AdminVessels/Command/RegisterAdminVesselBffCommandHandler.cs` — new
- `Application/Common/RemoteClients/IIdentityAdminBffRemoteCall.cs` — added bulk method
- `Application/Common/RemoteClients/IVesselAdminBffRemoteCall.cs` — added admin create method
- `Controllers/V1/AdminVesselsController.cs` — added register GET/POST actions

## Build Result
```
dotnet build Aizen.sln --no-incremental
→ 855 Warning(s), 0 Error(s)  ✅
```
