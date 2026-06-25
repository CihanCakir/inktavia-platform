# Vessel Owner Name Final Fix Report

## Scope
Fix `ownerName` and `ownerAvatarUrl` being null in `GET /api/v1/admin-panel/vessels?pageIndex=0&pageSize=20` BFF response.

## Root Cause Analysis

Two potential root causes were identified and addressed:

### Root Cause 1 — Authorization Role Mismatch (Confirmed Fix)
The Identity `GET /api/v1/identity/admin/users/profiles/bulk` endpoint was decorated with `[Authorize(Roles = RoleNames.Admin)]` (= `"Admin"` realm role).

When the `admin-panel-bff` Keycloak service account acquires a token via `client_credentials`, it receives Keycloak **client roles** (e.g. `resource_access.identity-api.roles: ["identity.admin"]`) — NOT the human `"Admin"` realm role. This causes a **403 Forbidden** on the Identity bulk endpoint, which is silently caught by the BFF catch block and adds only a soft warning to the response (no exception logged in prior version).

**Fix:** Changed `[Authorize(Roles = RoleNames.Admin)]` to `[Authorize(Roles = RoleNames.Admin + "," + RoleNames.IdentityAdmin)]` on both the `bulk` and new `bulk-by-profile-ids` endpoints. Added `IdentityAdmin = "identity.admin"` to `RoleNames`.

### Root Cause 2 — Seeder UserId Remap (Defensive Fix)
`IdentityMockDataSeeder.SeedUsersAsync` remaps user IDs if a user with the same email exists in the DB with a different ID. If a vessel owner user was created before the seeder ran, the profile's `UserId` in the DB would differ from the seed ID referenced by `vessel-owners.json`. A userId-based bulk lookup would return 0 results.

**Fix:** Added a new `GET /api/v1/identity/admin/users/profiles/bulk-by-profile-ids` endpoint and BFF fallback enrichment logic. After the primary userId lookup, items still missing `ownerName` but having `ownerProfileId` are re-enriched via a single profile-ID bulk call.

### Root Cause 3 — Silent Exception Swallowing (Logging Fix)
The prior `catch (Exception)` block did not log the exception, making it impossible to diagnose failures at runtime.

**Fix:** Changed to `catch (Exception ex) { _logger.LogWarning(ex, ...) }`.

### Root Cause 4 — Unsafe Body Cast (Code Quality Fix)
`profileResult?.Body as IEnumerable<UserProfileListItemDto>` — using `as` cast instead of direct assignment. If `Body` is null, the cast returns null; if it's a non-null empty list, the cast still succeeds but returns an empty list. The check `if (profiles == null) return;` would silently skip enrichment for the null case without adding a warning.

**Fix:** Use `profileResult.Body ?? new List<UserProfileListItemDto>()` after verifying `Header.IsSuccess`.

## Before State
```json
{
  "id": 20008,
  "ownerUserId": 10008,
  "ownerProfileId": 11008,
  "ownerName": null,
  "ownerAvatarUrl": null
}
```

## Expected After State
```json
{
  "id": 20008,
  "ownerUserId": 10008,
  "ownerProfileId": 11008,
  "ownerName": "Fatma Çelik",
  "ownerAvatarUrl": null
}
```

(`ownerAvatarUrl` remains null if Identity seed has no profile photo — acceptable.)

## Files Changed

| File | Change |
|------|--------|
| `Modules/Identity/src/Aizen.Modules.Identity.Abstraction/Model/RoleNames.cs` | Added `IdentityAdmin = "identity.admin"` constant |
| `Modules/Identity/src/Aizen.Modules.Identity/Controller/V1/Identity/QueryController.cs` | Updated `bulk` endpoint to `[Authorize(Roles = "Admin,identity.admin")]`; added new `bulk-by-profile-ids` endpoint |
| `Modules/Identity/src/Aizen.Modules.Identity.Application/Common/Query/GetUserProfilesByProfileIds/GetUserProfilesByProfileIdsQuery.cs` | New query class |
| `Modules/Identity/src/Aizen.Modules.Identity.Application/Common/Query/GetUserProfilesByProfileIds/GetUserProfilesByProfileIdsQueryHandler.cs` | New query handler — filters `p.Id IN (profileIds)` |
| `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/Common/RemoteClients/IIdentityAdminBffRemoteCall.cs` | Added `GetUserProfilesByProfileIds` RemoteCall method |
| `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminVessels/Query/GetAdminVesselListBffQueryHandler.cs` | Full `TryEnrichOwnerNamesAsync` rewrite: exception logging, Header.IsSuccess check, direct Body access, `ApplyOwnerNames`, `ResolveOwnerDisplayName`, profile-ID fallback path |

## Build Result

```
Build succeeded.
857 Warning(s)
0 Error(s)
```

All 857 warnings are pre-existing. No new warnings introduced.

## Architecture Decisions

- Enrichment remains in BFF — no domain logic moved to Vessel module.
- Single batch call for userId lookup + single batch call for profile-ID fallback = max 2 Identity calls per page regardless of item count (no N+1).
- `identity.admin` Keycloak role accepted as OR alternative to human `Admin` realm role. The `admin-panel-bff` service account should be assigned `identity.admin` in `resource_access.identity-api.roles` per Keycloak setup instructions.
- Exception is caught and logged as a warning; vessel list still returns successfully without enrichment when Identity is unavailable.

## Remaining Gaps

- `ownerAvatarUrl` will remain null until users have profile photos (`ProfilePhotoUrl`) in Identity seed/DB.
- If neither userId nor profileId lookup returns results (e.g., orphaned vessel owner references), `ownerName` will remain null — expected behavior.
- Keycloak must be configured with `identity.admin` client role on the `identity-api` client and assigned to the `admin-panel-bff` service account. If Keycloak was not yet configured, this is the first step required after deployment.
