# Vessel Owner Identity Bulk Runtime Validation Report

## Scope

Validate the AdminPanel BFF Identity bulk owner enrichment flow at code level and confirm runtime readiness.

---

## Identity Bulk Endpoint

**Route:** `GET /api/v1/identity/admin/users/profiles/bulk?userIds=10003&userIds=10004`

**Controller:** `Modules/Identity/src/Aizen.Modules.Identity/Controller/V1/Identity/QueryController.cs`

```csharp
[HttpGet("admin/users/profiles/bulk")]
[Authorize(Roles = RoleNames.Admin)]
[ProducesResponseType(typeof(List<UserProfileListItemDto>), StatusCodes.Status200OK)]
public async Task<AizenApiResponse<List<UserProfileListItemDto>>> GetUserProfilesByUserIds(
    [FromQuery] long[] userIds, CancellationToken ct)
```

**Query Handler:** `GetUserProfilesByUserIdsQueryHandler` — projects `UserProfileListItemDto` with `UserId`, `FirstName`, `LastName`, `ProfilePhotoUrl` from `UserProfileEntity`.

---

## BFF Remote Call Interface

**File:** `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/Common/RemoteClients/IIdentityAdminBffRemoteCall.cs`

```csharp
[AizenRemoteCallGet("/api/v1/identity/admin/users/profiles/bulk")]
Task<AizenApiResponse<List<UserProfileListItemDto>>> GetUserProfilesByUserIds(
    [Refit.Query] long[] userIds,
    [AizenRemoteCallHeader("Authorization")] string authorization,
    [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);
```

Route matches Identity controller. `[Refit.Query]` generates repeated `?userIds=X&userIds=Y` query string. ✅

---

## BFF Enrichment Logic

**File:** `GetAdminVesselListBffQueryHandler.cs` — `TryEnrichOwnerNamesAsync`

Flow:
1. Collect distinct `OwnerUserId` values from current page
2. Skip Identity call if `ownerUserIds.Length == 0`
3. Log count: `[VesselListBff] ownerUserIds collected: N`
4. Call Identity bulk endpoint once per page
5. Log returned count: `[VesselListBff] identity profiles returned: N`
6. Build `Dictionary<long, UserProfileListItemDto>` keyed by `UserId`
7. For each vessel row, look up profile by `OwnerUserId` and set `OwnerName` / `OwnerAvatarUrl`

**OwnerName resolution:** `$"{profile.FirstName} {profile.LastName}".Trim()` — correct per spec.
**OwnerAvatarUrl resolution:** `profile.ProfilePhotoUrl` — correct per spec.

---

## Failure Handling

If Identity call throws: `response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Identity"))` — vessel rows are preserved with `ownerName = null`.

---

## Auth Headers Forwarded

- `Authorization: Bearer <admin-panel-bff-keycloak-service-token>` — obtained from `IAdminPanelBffKeycloakServiceTokenProvider`
- `X-Aizen-User-Token: Bearer <incoming identity user token>` — from `request.UserToken`

No browser-provided Keycloak token forwarded. ✅

---

## Runtime Note

The Identity endpoint uses `[Authorize(Roles = RoleNames.Admin)]`. The `admin-panel-bff` Keycloak service account must have the Admin role mapped for this endpoint to return 200. If the service account lacks the role, the BFF catches the exception and returns a warning.

---

## Remaining Gaps

- `OwnerAvatarUrl` will be null for all seeded profiles (no `ProfilePhotoUrl` set in Identity seed data). This is expected — frontend uses a fallback avatar.
- Runtime validation requires Identity service, Vessel service, BFF, PostgreSQL, Keycloak, Redis all running.
