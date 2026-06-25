# 02 — Fix Owner Identity Bulk Enrichment

## Problem

The Vessel list response contains `ownerUserId` and `ownerProfileId`, but does not include `ownerName` or `ownerAvatarUrl`.

## Target Flow

```text
BFF GetAdminVesselListBffQueryHandler
  1. Call Vessel module list endpoint
  2. Map base vessel rows
  3. Collect unique OwnerUserId values
  4. Call Identity bulk profile endpoint once
  5. Merge owner display data into each vessel row
```

## Files to Audit and Fix

```text
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminVessels/Query/GetAdminVesselListBffQueryHandler.cs
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminVessels/Dto/VesselListItemBffDto.cs
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/Common/RemoteClients/IIdentityAdminBffRemoteCall.cs
Modules/Identity/src/Aizen.Modules.Identity.Application/Common/Query/GetUserProfilesByUserIds/
Modules/Identity/src/Aizen.Modules.Identity/Controller/V1/Identity/QueryController.cs
```

## Expected Identity Route

```http
GET /api/v1/identity/admin/users/profiles/bulk?userIds=10003&userIds=10004
```

If the actual route differs, update the BFF RemoteCall interface to match the actual Identity controller route.

## Header Rules

The BFF must forward both headers to Identity:

```http
Authorization: Bearer <admin-panel-bff-keycloak-service-token>
X-Aizen-User-Token: Bearer <incoming identity user token>
```

Do not forward browser `Authorization`.

## Mapping Rules

- Dictionary key must be `UserId`, not `ProfileId`.
- `OwnerName` should be resolved in this order:
  1. `FullName`
  2. `FirstName + " " + LastName`
  3. `DisplayName`
  4. null
- `OwnerAvatarUrl` should be resolved in this order:
  1. `ProfilePhotoUrl`
  2. `AvatarUrl`
  3. null

## Failure Behavior

- If `ownerUserIds` is empty, do not call Identity.
- If Identity call fails, add `AdminBffWarning.ModuleUnavailable("Identity")`.
- Preserve vessel rows even if owner enrichment fails.
