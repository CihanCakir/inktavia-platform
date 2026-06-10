# AdminPanel BFF Auth Boundary Audit Report

## Audit Date
2026-06-10

## Target Projects
- `Bff/src/AdminPanel/Aizen.Bff.AdminPanel`
- `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application`

---

## Pre-Refactor State

### Inbound browser auth
- Controllers extracted both `Authorization: Bearer <keycloak-token>` and `X-Aizen-User-Token: Bearer <identityToken>` from every request.
- Controllers carried `[Authorize(Roles = "Admin")]` requiring a Keycloak JWT with the `Admin` role.
- Browser was forced to manage Keycloak tokens.

### Header forwarding to internal APIs
- Both `Authorization` (Keycloak) and `X-Aizen-User-Token` (Identity) were passed through every command/query object.
- Every handler forwarded both to RemoteCall interfaces.
- Every RemoteCall interface forwarded both as HTTP headers to downstream module APIs.

### Controllers affected (pre-refactor)
| Controller | Auth policy | Extracted Authorization |
|---|---|---|
| `AdminDashboardController` | `[Authorize(Roles = "Admin")]` | Yes |
| `AdminFilesController` | `[Authorize(Roles = "Admin")]` | Yes |
| `AdminIdentityController` (IdentityController) | `[Authorize(Roles = "Admin")]` | Yes |
| `AdminReferenceDataController` | `[Authorize(Roles = "Admin")]` | Yes |
| `AdminServiceRequestsController` | `[Authorize(Roles = "Admin")]` | Yes |
| `AdminVesselsController` (VesselsController) | `[Authorize(Roles = "Admin")]` | Yes |
| `AuthController` | Mixed (`AllowAnonymous`/`Authorize`) | Yes (ChangePassword) |
| `OrganizersController` | `[Authorize(Roles = "Admin")]` | Yes |
| `ParticipantsController` | `[Authorize(Roles = "Admin")]` | Yes |
| `VenuesController` | `[Authorize(Roles = "Admin")]` | Yes |

### Remote Call interfaces
- All 5 remote call interfaces accepted `[AizenRemoteCallHeader("Authorization")]` and `[AizenRemoteCallHeader("X-Aizen-User-Token")]` parameters.
- Auth endpoints (login/refresh/otp) correctly had no auth headers.

### Cache usage (pre-refactor)
- No Keycloak token caching existed.
- `IAizenDistributedCache` was not referenced in the BFF Application project.

### Security gap
- Browser client was required to obtain and manage Keycloak tokens.
- No server-side `admin-panel-bff` client credentials flow existed.
- The BFF had no service identity — it forwarded browser Keycloak tokens unchanged.

---

## Post-Refactor State

See accompanying reports:
- `admin-panel-bff-cache-backed-keycloak-service-token-report.md`
- `admin-panel-bff-internal-remote-call-forwarding-report.md`
- `admin-panel-bff-auth-pipeline-fix-report.md`
