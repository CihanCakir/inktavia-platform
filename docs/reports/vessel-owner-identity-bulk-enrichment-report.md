# Vessel Owner Identity Bulk Enrichment Report

## Scope
Implementation of bulk owner display name enrichment for the Vessel list page via Identity module.

## Files Created / Modified

| File | Action |
|------|--------|
| `Modules/Identity/src/Aizen.Modules.Identity.Application/Common/Query/GetUserProfilesByUserIds/GetUserProfilesByUserIdsQuery.cs` | Created |
| `Modules/Identity/src/Aizen.Modules.Identity.Application/Common/Query/GetUserProfilesByUserIds/GetUserProfilesByUserIdsQueryHandler.cs` | Created |
| `Modules/Identity/src/Aizen.Modules.Identity/Controller/V1/Identity/QueryController.cs` | Modified — added bulk endpoint |
| `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/Common/RemoteClients/IIdentityAdminBffRemoteCall.cs` | Modified — added `GetUserProfilesByUserIds` |
| `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminVessels/Dto/VesselListItemBffDto.cs` | Modified — added `OwnerUserId`, `OwnerProfileId`, `OwnerAvatarUrl` |
| `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminVessels/Query/GetAdminVesselListBffQueryHandler.cs` | Rewritten — bulk enrichment loop added |

## Identity Bulk Endpoint

### New endpoint
```
GET /api/v1/identity/admin/users/profiles/bulk?userIds=X&userIds=Y
Authorization: Roles = Admin
```

Returns `List<UserProfileListItemDto>` for the provided user IDs.

### Query
`GetUserProfilesByUserIdsQuery` → `GetUserProfilesByUserIdsQueryHandler`  
Filters `UserProfileEntity` by `p.UserId` in the provided `long[]` array.

## BFF Enrichment Strategy

1. Fetch vessel page from Vessel module.
2. Map base items (location, status labels computed inline).
3. Collect unique `OwnerUserId` values from page items.
4. **Single call** to `GET /api/v1/identity/admin/users/profiles/bulk` with all IDs.
5. Build `Dictionary<long, UserProfileListItemDto>` keyed by `UserId`.
6. Merge `FirstName + LastName` → `OwnerName` and `ProfilePhotoUrl` → `OwnerAvatarUrl` per item.

## N+1 Prevention
The BFF makes exactly **one** Identity call per vessel list page regardless of page size.

## Null/Failure Safety
- If `ownerUserIds` is empty, Identity is not called.
- If Identity call throws, `Warnings` gets `AdminBffWarning.ModuleUnavailable("Identity")`.
- Vessel rows are preserved; only `OwnerName` and `OwnerAvatarUrl` remain `null`.

## ID Convention
- `OwnerUserId`: `long?` ✅
- `OwnerProfileId`: `long?` ✅
- No `Guid` introduced ✅

## Build Validation
```
dotnet build Aizen.sln --no-incremental
→ 0 Error(s)
```

## Remaining Gaps
- Identity module does not expose `Email` on `UserProfileListItemDto` — `OwnerCandidateBffDto.Email` will always be null until Identity exposes it.
- If a vessel has no `IsPrimary && IsActive` owner, `OwnerUserId` is null and enrichment is skipped for that row.
