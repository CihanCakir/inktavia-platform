# Admin Web Client Contract — Final Report

## Scope

This document summarizes the findings from the AdminPanel BFF analysis and the generated client contract package.

Source projects analyzed:

- `Bff/src/AdminPanel/Aizen.Bff.AdminPanel`
- `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application`

---

## BFF Summary

### Controllers Found

| Tag | Controller | Route Base |
|-----|-----------|------------|
| Auth | `AuthController` | `api/v1/admin-panel/auth` |
| Dashboard | `DashboardController` | `api/v1/admin-panel` |
| Identity | `IdentityController` | `api/v1/admin-panel` |
| Organizers | `OrganizersController` | `api/v1/admin-panel` |
| Venues | `VenuesController` | `api/v1/admin-panel` |
| Participants | `ParticipantsController` | `api/v1/admin-panel` |
| Files | `FilesController` | `api/v1/admin-panel` |
| Vessels | `VesselsController` | `api/v1/admin-panel` |
| Service Requests | `ServiceRequestsController` | `api/v1/admin-panel` |
| Reference Data | `ReferenceDataController` | `api/v1/admin-panel` |

### Total Endpoints: 42

| Domain | Endpoint Count |
|--------|---------------|
| Auth | 7 |
| Dashboard | 1 |
| Identity (general) | 3 |
| Identity (organizers) | 5 |
| Identity (venues) | 4 |
| Identity (participants) | 2 |
| Files | 5 |
| Vessels | 9 |
| Service Requests | 10 |
| Reference Data | 8 |

---

## Architecture Pattern Confirmed

- All controllers delegate to `IAizenCQRSProcessor`.
- Every controller reads `Authorization` and `X-Aizen-User-Token` from incoming request headers and passes them downstream to application-layer queries/commands.
- Application layer uses `IAizenRemoteCall`-based interfaces (`IFileStorageAdminBffRemoteCall`, `IIdentityAdminBffRemoteCall`, `IVesselAdminBffRemoteCall`, `IServiceRequestAdminBffRemoteCall`, `IReferenceDataAdminBffRemoteCall`) for synchronous module calls.
- All aggregated responses carry a `List<AdminBffWarning>` for non-fatal downstream failures.

---

## Token Architecture Confirmed

- **Layer 1**: Keycloak access token → `Authorization: Bearer` header (validated by BFF OIDC middleware).
- **Layer 2**: Identity access token → `X-Aizen-User-Token: Bearer` header (forwarded to Identity module on each call).
- Auth endpoints (`/auth/*`) are `[AllowAnonymous]` and return Identity tokens directly.
- `POST /auth/refresh` is available and **covers the Identity token refresh gap** identified in authentication analysis.

---

## Generated Documents

| File | Description |
|------|-------------|
| `admin-panel-bff-endpoint-catalog.md` | Complete table of all 42 BFF endpoints with method, path, auth, and purpose |
| `admin-panel-bff-response-types.md` | Full TypeScript type definitions extracted from BFF DTOs and RemoteCall contracts |
| `admin-web-authentication-flow.md` | Dual-token auth flow, PKCE sequence, storage strategy, refresh strategy, error handling, and identified gaps |
| `admin-web-client-architecture.md` | React project structure, Axios client setup, auth store, TanStack Query pattern, environment variables, module boundaries |
| `admin-web-api-client-contract.md` | Typed API function contracts for all 7 API modules + `unwrap` helper |
| `admin-web-implementation-roadmap.md` | 10-phase implementation roadmap with dependency map |
| `admin-web-contract-final-report.md` | This document |

---

## Identified Gaps

| Gap | Severity | Notes |
|-----|----------|-------|
| No server-side logout endpoint | Low | Client discards tokens locally; Keycloak logout is handled by `keycloak-js`. No Identity session invalidation endpoint is exposed. |
| No `/me` endpoint | Low | Admin user identity must be decoded from Keycloak JWT claims (`preferred_username`, `email`, `roles`). |
| `AdminUsers` controller not found | Info | Covered by `IdentityController` + `OrganizersController` + `VenuesController` + `ParticipantsController`. |
| No dedicated filter-options for Identity | Info | Identity profile list uses inline `roleContext` and `approvalStatus` string params; no enum/options endpoint. The client should hardcode or derive these from known values. |

---

## Out-of-Scope Confirmation

- Payment domain: no controller, no RemoteCall, no DTO found in AdminPanel BFF.
- Profile domain (standalone): not present as a dedicated module in BFF; profile operations are handled through Identity endpoints.
- Azure Blob Storage: not referenced.
- MinIO: not referenced.

---

## React Admin Web Project Status

No existing React Admin Web project was found in the repository. The generated documents provide a **recommended architecture only**. Project scaffolding should not be performed unless explicitly instructed.

---

## Next Steps

1. Validate TypeScript types in `admin-panel-bff-response-types.md` against actual Identity, Vessel, ServiceRequest, and FileStorage Abstraction DTOs.
2. Scaffold the React project using the structure in `admin-web-client-architecture.md`.
3. Implement in the order defined in `admin-web-implementation-roadmap.md` starting with Phase 1 (Auth).
4. Confirm Keycloak realm and client ID for the admin-panel OIDC client.
5. Confirm BFF base URL for each deployment environment.
