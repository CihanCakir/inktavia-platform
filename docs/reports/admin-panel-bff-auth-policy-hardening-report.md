# AdminPanel BFF — Auth Policy Hardening Report

**Date:** 2026-06-15  
**Branch:** feature/service-request-registration  
**Scope:** AdminPanel BFF inbound authentication hardening pass

---

## Summary

This report documents the hardening of the `AdminPanelAccess` authorization policy to ensure that
only Identity tokens carrying the `Admin` role claim can access protected AdminPanel BFF endpoints.
Customer, mobile, and participant-only tokens are now explicitly rejected.

---

## Prior State (Before This Pass)

| Item | State |
|---|---|
| Auth policy name | `AdminPanelAccess` |
| Policy requirement | `RequireAuthenticatedUser()` only |
| Role/claim validation | **None** — any valid Identity token could pass |
| Customer/mobile rejection | **No** — participant tokens would succeed |
| Controller decoration | Generic `[Authorize]` on all protected controllers |
| `AuthController.ChangePassword` | Generic `[Authorize]` |

---

## Changes Applied

### 1. `AdminPanelAccess` policy — `Program.cs`

```csharp
options.AddPolicy("AdminPanelAccess", policy =>
    policy
        .RequireAuthenticatedUser()
        .RequireRole("Admin"));
```

**Claim validated:** `ClaimTypes.Role` = `"Admin"`  
Roles are issued by the Identity module via `InktaviaTokenService` using `new Claim(ClaimTypes.Role, role)`.
The seed data registers `"Admin"` as the admin role name (`RoleNames.Admin = "Admin"`).

### 2. All protected controllers updated

All class-level `[Authorize]` attributes replaced with `[Authorize(Policy = "AdminPanelAccess")]`:

| Controller | File |
|---|---|
| `AdminDashboardController` | `AdminDashboardController.cs` |
| `AdminFilesController` | `AdminFilesController.cs` |
| `AdminIdentityController` | `AdminIdentityController.cs` |
| `AdminReferenceDataController` | `AdminReferenceDataController.cs` |
| `AdminServiceRequestsController` | `AdminServiceRequestsController.cs` |
| `AdminVesselsController` | `AdminVesselsController.cs` |
| `OrganizersController` | `OrganizersController.cs` |
| `ParticipantsController` | `ParticipantsController.cs` |
| `VenuesController` | `VenuesController.cs` |
| `AuthController.ChangePassword` (action-level) | `AuthController.cs:86` |

### 3. Exempt endpoints (remain `[AllowAnonymous]`)

- `POST /auth/login/username`
- `POST /auth/login/phone`
- `POST /auth/otp/send`
- `POST /auth/otp/check`
- `POST /auth/refresh`
- All `AdminInactiveModulesController` endpoints (501 responses)

---

## Behavior After Hardening

| Token type | Has `Admin` claim | Access to protected endpoints |
|---|---|---|
| Admin Identity token | ✅ Yes | ✅ Allowed |
| Participant-only token | ❌ No | ❌ 403 Forbidden |
| Customer/mobile token | ❌ No | ❌ 403 Forbidden |
| No `X-Aizen-User-Token` header | ❌ Not authenticated | ❌ 401 Unauthorized |
| Expired Identity token | ❌ Not valid | ❌ 401 Unauthorized |

---

## Test JWT Compatibility

The local test JWT (from `identity.api.json`) contains:
```json
"roles": ["Admin", "Participant"]
```
This token carries the `Admin` role claim and passes the `AdminPanelAccess` policy. ✅

---

## Validation

```
dotnet build Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Aizen.Bff.AdminPanel.csproj
```
**Result: Build succeeded. 0 errors.**

---

## Remaining Gaps / Follow-Ups

1. **Panel context claim**: If the Identity module issues a separate `PanelContext` claim (e.g., `"AdminPanel"`) in future releases, the policy can be extended with `.RequireClaim("PanelContext", "AdminPanel")` for defense-in-depth. The `Admin` role check is sufficient for the current Identity contract.

2. **Integration test**: A negative test with a participant-only token against a protected endpoint should be added to the local BFF test suite to verify 403 rejection end-to-end.
