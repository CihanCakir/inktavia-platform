# Vessel List Enrichment Audit Report

## Scope
Audit of the AdminPanel BFF vessel list handler and Vessel module admin list query to determine why the Owner, Last Location, and Status columns were empty in the React Admin Web UI.

## Files Audited

| File | Role |
|------|------|
| `Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Controllers/V1/AdminVesselsController.cs` | BFF controller |
| `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminVessels/Query/GetAdminVesselListBffQueryHandler.cs` | BFF list handler |
| `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminVessels/Dto/VesselListItemBffDto.cs` | BFF list item DTO |
| `Modules/Vessel/src/Aizen.Modules.Vessel.Application/Query/Vessel/GetAllVesselsAdmin/GetAllVesselsAdminQueryHandler.cs` | Vessel module list query handler |
| `Modules/Vessel/src/Aizen.Modules.Vessel.Abstraction/Dto/Vessel/VesselListItemDto.cs` | Vessel module list item DTO |
| `Modules/Vessel/src/Aizen.Modules.Vessel.Domain/Entities/Vessel/VesselEntity.cs` | Vessel aggregate |
| `Modules/Vessel/src/Aizen.Modules.Vessel.Domain/Entities/Ownership/VesselOwnerEntity.cs` | Owner entity |
| `Modules/Vessel/src/Aizen.Modules.Vessel.Domain/Entities/Location/VesselLocationSnapshotEntity.cs` | Location snapshot entity |

## Root Causes Found

### 1. OwnerName — Empty
- `GetAllVesselsAdminQueryHandler` projected `VesselListItemDto` without joining `VesselOwners`. 
- `OwnerName` was never populated in the query projection.
- `VesselListItemDto` had `OwnerName` field but no `OwnerUserId`/`OwnerProfileId` to allow BFF enrichment.

### 2. Latitude / Longitude / LastPositionDate — Empty  
- `GetAllVesselsAdminQueryHandler` did not join `VesselLocationSnapshots`.
- The entity has a `_locationSnapshots` navigation collection, but the query projection only read direct scalar fields from `VesselEntity`.
- No `LastLocationMarinaName` field existed on the DTO for human-readable location text.

### 3. OperationalStatus / OwnershipStatus / Status — Numeric without labels
- The handler mapped raw numeric fields correctly.
- However `VesselListItemBffDto` had no label fields (`OperationalStatusLabel`, etc.).
- The UI showed dashes because it expected label strings, not numeric codes.

### 4. OwnershipStatus — Not in projection
- `OwnershipStatus` was listed in `VesselListItemDto` but was **not populated** in the `GetAllVesselsAdminQueryHandler` projection — the field was always `null`.

## BFF Route Audit

| Route | Status |
|-------|--------|
| `GET /api/v1/admin-panel/vessels` | ✅ Existed |
| `GET /api/v1/admin-panel/vessels/register` | ❌ Missing (404) |
| `POST /api/v1/admin-panel/vessels/register` | ❌ Missing |
| `GET /api/v1/admin-panel/vessels/form-options` | ✅ Existed (static only) |

## Fixes Applied
See individual reports for full details.

## Build Validation
```
dotnet build Aizen.sln --no-incremental
→ 855 Warning(s), 0 Error(s)
```

## Remaining Gaps
- `CoverMediaUrl` is hardcoded `null` in `GetAllVesselsAdminQueryHandler` — thumbnail requires FileStorage integration.
- Cache invalidation not extended for the enriched list query (5-min TTL on `IAizenQueryHandlerCacheable` may serve stale owner data after profile update).

## Rollback Notes
All changes are additive. Revert the following files to undo:
- `VesselListItemDto.cs` (remove `OwnerUserId`, `OwnerProfileId`, `LastLocationMarinaName`)
- `GetAllVesselsAdminQueryHandler.cs` (remove owner/location joins in selector)
