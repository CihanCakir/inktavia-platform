# AdminPanel BFF Active Module Remote Call Debug Report

## Scope

Debug and verify active module remote calls for fixed endpoints.

## Auth Header Flow (Verified)

All active module remote call handlers follow this pattern:

```csharp
var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(ct);
var authHeader = $"Bearer {serviceToken}";
// → Authorization: Bearer <admin-panel-bff-keycloak-service-token>
// → X-Aizen-User-Token: Bearer <identityAccessToken> (from request)
```

## Fixed Endpoints Trace

### Identity: /identity/organizers/profiles, /identity/venues/profiles, /identity/participant/profiles

- BFF Controller: `OrganizersController`, `VenuesController`, `ParticipantsController`
- Application: `SearchOrganizerProfilesQuery/Handler`, `SearchVenueProfilesQuery/Handler`, `SearchParticipantProfilesQuery/Handler`
- Remote Call: `IIdentityAdminBffRemoteCall.SearchOrganizerProfiles/SearchVenueProfiles/SearchParticipantProfiles`
- Internal: `GET /api/v1/identity/organizers/profiles`, `/api/v1/identity/venues/profiles`, `/api/v1/identity/participant/profiles`
- **Failure was**: AUTH_PIPELINE_DEFAULT_SCHEME_500 — not a remote call issue; fixed by auth registration

### Dashboard: /dashboard/overview

- BFF Controller: `DashboardController`
- Remote Call: Dashboard aggregates from multiple modules via `GetAdminDashboardOverviewQueryHandler`
- **Failure was**: AUTH_PIPELINE_DEFAULT_SCHEME_500 — fixed by auth registration

### ServiceRequests: /service-requests

- BFF Controller: `ServiceRequestsController`
- Remote Call: `IServiceRequestAdminBffRemoteCall.GetAdminServiceRequests`
- **Failure was**: AUTH_PIPELINE_DEFAULT_SCHEME_500 — fixed by auth registration

### Vessels: /vessels

- BFF Controller: `VesselsController`
- Remote Call: `IVesselAdminBffRemoteCall.GetAdminVesselList`
- **Failure was**: AUTH_PIPELINE_DEFAULT_SCHEME_500 — fixed by auth registration

### Vessels: /vessels/:id/media (new)

- BFF Controller: `VesselsController.GetVesselMedia`
- Application: `GetAdminVesselMediaQuery/Handler`
- Remote Call: `IVesselAdminBffRemoteCall.GetVesselMedia → GET /api/v1/vessels/{vesselId}/media`
- Internal Module: `VesselMediaController.GetAll` — `AllowAnonymous` so no extra auth needed

### Vessels: /vessels/:id/status-history (new)

- BFF Controller: `VesselsController.GetVesselStatusHistory`
- Application: `GetAdminVesselStatusHistoryQuery/Handler`
- Remote Call: `IVesselAdminBffRemoteCall.GetVesselStatusHistory → GET /api/v1/vessels/{vesselId}/status-history`

### ReferenceData: /reference-data/lookup/{groupCode}/items

- Route matched correctly (string groupCode accepts GUID string)
- **Failure was**: AUTH_PIPELINE_DEFAULT_SCHEME_500 — fixed by auth registration

### ReferenceData: POST /reference-data/lookup (new)

- BFF Controller: `ReferenceDataController.CreateLookupGroup`
- Application: `CreateLookupGroupCommand/Handler`
- Remote Call: `IReferenceDataAdminBffRemoteCall.CreateLookupGroup → POST /api/v1/admin/reference-data/lookup-groups`
- Internal Module: `LookupAdminController.CreateGroup` — requires Admin role; BFF Keycloak service token carries admin-panel-bff roles

### ReferenceData: POST /reference-data/lookup/{groupCode}/items (new)

- BFF Controller: `ReferenceDataController.CreateLookupItem`
- Application: `CreateLookupItemCommand/Handler`
- Remote Call: `IReferenceDataAdminBffRemoteCall.CreateLookupItem → POST /api/v1/admin/reference-data/lookup-items`

## Known Issues

- Keycloak service token unavailable locally (Keycloak not running) → active remote calls will fail with "token unavailable" or timeout. This is expected in local-only dev without Keycloak; the auth pipeline fix resolves the 500→401 path.
- The `IReferenceDataAdminBffRemoteCall` remote call base URL points to `/api/v1/lookup-groups` for reads (correct for the public ReferenceData API) and `/api/v1/admin/reference-data/lookup-groups` for writes (correct for the admin API). Verify base URL config if both paths resolve correctly.
