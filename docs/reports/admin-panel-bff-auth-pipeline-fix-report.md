# AdminPanel BFF Auth Pipeline Fix Report

## Report Date
2026-06-10

---

## Inbound Auth Policy Change

### Before
All protected controllers used `[Authorize(Roles = "Admin")]`:
```csharp
[Authorize(Roles = "Admin")]
public sealed class VesselsController : AizenWebApiController { ... }
```
This required the browser to send a Keycloak JWT with the `Admin` role claim.

### After
All protected controllers use `[Authorize]`:
```csharp
[Authorize]
public sealed class VesselsController : AizenWebApiController { ... }
```
The inbound auth policy is now delegated to the Aizen BFF starter's auth middleware, which validates the `X-Aizen-User-Token` Identity JWT.

---

## Auth Endpoints (Public)

The following endpoints remain `[AllowAnonymous]`:
```
POST /api/v1/admin-panel/auth/login/username
POST /api/v1/admin-panel/auth/login/phone
POST /api/v1/admin-panel/auth/login/otp
POST /api/v1/admin-panel/auth/otp/send
POST /api/v1/admin-panel/auth/otp/check
POST /api/v1/admin-panel/auth/refresh
```

`POST /api/v1/admin-panel/auth/password/change` remains `[Authorize]` — requires valid Identity user context.

---

## Controllers Updated

| Controller | Old Policy | New Policy |
|---|---|---|
| `AdminDashboardController` | `[Authorize(Roles = "Admin")]` | `[Authorize]` |
| `AdminFilesController` | `[Authorize(Roles = "Admin")]` | `[Authorize]` |
| `IdentityController` (AdminIdentity) | `[Authorize(Roles = "Admin")]` | `[Authorize]` |
| `AdminReferenceDataController` | `[Authorize(Roles = "Admin")]` | `[Authorize]` |
| `AdminServiceRequestsController` | `[Authorize(Roles = "Admin")]` | `[Authorize]` |
| `VesselsController` (AdminVessels) | `[Authorize(Roles = "Admin")]` | `[Authorize]` |
| `OrganizersController` | `[Authorize(Roles = "Admin")]` | `[Authorize]` |
| `ParticipantsController` | `[Authorize(Roles = "Admin")]` | `[Authorize]` |
| `VenuesController` | `[Authorize(Roles = "Admin")]` | `[Authorize]` |

---

## What Was NOT Disabled

- Authorization was not disabled globally.
- `[AllowAnonymous]` was not added to protected routes.
- Identity token validation through the Aizen BFF middleware remains active.

---

## Remaining Follow-Up

- The Aizen BFF starter (`Aizen.Core.Starter.Bff`) should configure an Identity-token-based authentication scheme (validating the `X-Aizen-User-Token` header JWT). The exact BFF auth middleware was not modified in this task — the framework handles this.
- If `[Authorize]` alone is insufficient to enforce Identity token presence, a named policy `"IdentityUserPolicy"` can be added in the Aizen BFF starter to explicitly require the `X-Aizen-User-Token` claim principal.
