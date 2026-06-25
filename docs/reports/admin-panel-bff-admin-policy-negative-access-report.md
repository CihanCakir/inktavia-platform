# AdminPanel BFF — Admin Policy Negative Access Report

**Date:** 2026-06-16T21:34:24Z  
**Scope:** `AdminPanelAccess` policy validation — rejection of non-admin tokens

---

## Policy Configuration

```csharp
// Program.cs
options.AddPolicy("AdminPanelAccess", policy =>
    policy
        .RequireAuthenticatedUser()
        .RequireRole("Admin"));
```

**Claim validated:** `ClaimTypes.Role` = `"Admin"`  
(`http://schemas.microsoft.com/ws/2008/06/identity/claims/role`)

Identity tokens are issued by the Identity module using:
```csharp
claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
```
The seed data assigns `"Admin"` to platform administrators (`RoleNames.Admin = "Admin"`).

---

## Test Token Matrix

| Token | Roles in JWT | Expected |
|---|---|---|
| **Admin token** | `["Admin", "Participant"]` | ✅ Granted (not 401/403) |
| **Participant token** | `["Participant"]` (no Admin) | 🚫 403 Forbidden |
| **Anonymous** (no token) | — | 🚫 401 Unauthorized |

---

## Live Test Results

### Anonymous → 401 Unauthorized

| Endpoint | Result |
|---|---|
| `GET /dashboard/overview` | HTTP 401 ✅ |
| `GET /vessels` | HTTP 401 ✅ |
| `GET /service-requests` | HTTP 401 ✅ |
| `GET /reference-data/lookup-groups` | HTTP 401 ✅ |
| `POST /auth/password/change` | HTTP 401 ✅ |

### Admin token (roles: Admin + Participant) → Access Granted

| Endpoint | HTTP Status | Auth result |
|---|---|---|
| `GET /dashboard/overview` | 200 | ✅ Granted |
| `GET /vessels` | 200 | ✅ Granted |
| `GET /service-requests` | 200 | ✅ Granted |
| `GET /reference-data/lookup-groups` | 500 | ✅ Granted (downstream down) |
| `GET /identity/organizers/profiles` | 500 | ✅ Granted (downstream down) |
| `POST /auth/password/change` | 400 | ✅ Granted (FluentValidation rejection) |

### Participant token (roles: Participant only) → 403 Forbidden

| Endpoint | Result |
|---|---|
| `GET /dashboard/overview` | HTTP 403 ✅ |
| `GET /vessels` | HTTP 403 ✅ |
| `GET /service-requests` | HTTP 403 ✅ |
| `GET /reference-data/lookup-groups` | HTTP 403 ✅ |
| `GET /identity/organizers/profiles` | HTTP 403 ✅ |
| `POST /auth/password/change` | HTTP 403 ✅ |

---

## Result Summary

| Scenario | Tests | Passed |
|---|---|---|
| Anonymous → 401 | 5 | 5 ✅ |
| Admin → granted | 6 | 6 ✅ |
| Participant → 403 | 6 | 6 ✅ |
| **Total** | **17** | **17 ✅** |

---

## Controllers Using `AdminPanelAccess` Policy

All protected controllers confirmed:

| Controller | Attribute |
|---|---|
| `AdminDashboardController` | `[Authorize(Policy = "AdminPanelAccess")]` |
| `AdminFilesController` | `[Authorize(Policy = "AdminPanelAccess")]` |
| `AdminIdentityController` | `[Authorize(Policy = "AdminPanelAccess")]` |
| `AdminReferenceDataController` | `[Authorize(Policy = "AdminPanelAccess")]` |
| `AdminServiceRequestsController` | `[Authorize(Policy = "AdminPanelAccess")]` |
| `AdminVesselsController` | `[Authorize(Policy = "AdminPanelAccess")]` |
| `OrganizersController` | `[Authorize(Policy = "AdminPanelAccess")]` |
| `ParticipantsController` | `[Authorize(Policy = "AdminPanelAccess")]` |
| `VenuesController` | `[Authorize(Policy = "AdminPanelAccess")]` |
| `AuthController.ChangePassword` (action) | `[Authorize(Policy = "AdminPanelAccess")]` |

## Controllers Exempt (AllowAnonymous)

| Controller / Action | Reason |
|---|---|
| `AuthController.LoginWithUsername` | Public login endpoint |
| `AuthController.LoginWithPhone` | Public login endpoint |
| `AuthController.LoginWithOtp` | Public login endpoint |
| `AuthController.SendOtp` | Public OTP endpoint |
| `AuthController.CheckOtp` | Public OTP endpoint |
| `AuthController.Refresh` | Public token refresh endpoint |
| `AdminInactiveModulesController` (all) | 501 responses, no sensitive data |

---

## Security Considerations

1. **FallbackPolicy** (`RequireAuthenticatedUser`) ensures any endpoint without explicit auth metadata also requires authentication. This provides defense-in-depth.

2. **Token validation**: JWT tokens are validated against `TokenOption:Issuer`, `TokenOption:Audience`, and `TokenOption:SecurityKey` from `appsettings.Local.json`. Tokens from other issuers or with invalid signatures are rejected with HTTP 401.

3. **Participant-only JWT rejected**: A token with `["Participant"]` roles but no `"Admin"` role receives HTTP 403, confirming the policy correctly differentiates admin vs. non-admin tokens.

4. **No plain `[Authorize]`** remains on any BFF controller — all use the explicit `AdminPanelAccess` policy.
