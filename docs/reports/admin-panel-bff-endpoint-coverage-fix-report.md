# AdminPanel BFF Endpoint Coverage Fix Report

## Scope

Fix `MISSING_ROUTE_OR_INACTIVE_MODULE` and `METHOD_NOT_ALLOWED_ROUTE_INCOMPLETE` failures by adding BFF routes and correcting route naming mismatches.

## Route Decisions

### ReferenceData Route Aliases

The test runner uses shorter paths that don't match the BFF's verbose route names. Added `[HttpGet]` alias attributes to existing controller actions:

| Test Path | BFF Canonical Route | Resolution |
|-----------|---------------------|------------|
| `/reference-data/lookup` | `/reference-data/lookup-groups` | ✅ Alias added |
| `/reference-data/currency` | `/reference-data/currencies` | ✅ Alias added |
| `/reference-data/location` | `/reference-data/locations/countries` | ✅ Alias added |
| `/reference-data/measurement` | `/reference-data/measurement-units` | ✅ Alias added |
| `/reference-data/system-parameter` | `/reference-data/system-parameters` | ✅ Alias added |

Note: `/reference-data/lookup/{groupCode}/items` already matched correctly (groupCode is a string; GUID is a valid string).

### ReferenceData Write Endpoints

Active module (`ReferenceData.LookupAdminController`) has create operations. BFF endpoints added:

| Endpoint | Internal Module Route | Status |
|----------|-----------------------|--------|
| `POST /reference-data/lookup` | `POST /api/v1/admin/reference-data/lookup-groups` | ✅ Added |
| `POST /reference-data/lookup/{groupCode}/items` | `POST /api/v1/admin/reference-data/lookup-items` | ✅ Added |

New files:
- `AdminReferenceData/Command/CreateLookupGroupCommand.cs`
- `AdminReferenceData/Command/CreateLookupGroupCommandHandler.cs`
- `AdminReferenceData/Command/CreateLookupItemCommand.cs`
- `AdminReferenceData/Command/CreateLookupItemCommandHandler.cs`
- `IReferenceDataAdminBffRemoteCall.cs` — added `CreateLookupGroup` and `CreateLookupItem` methods

### Vessel Missing Endpoints

Active module (`VesselMediaController`, `VesselStatusController`) provides these endpoints. BFF endpoints added:

| Endpoint | Internal Module Route | Status |
|----------|-----------------------|--------|
| `GET /vessels/{id}/media` | `GET /api/v1/vessels/{vesselId}/media` | ✅ Added |
| `GET /vessels/{id}/status-history` | `GET /api/v1/vessels/{vesselId}/status-history` | ✅ Added |

New files:
- `AdminVessels/Query/GetAdminVesselMediaQuery.cs`
- `AdminVessels/Query/GetAdminVesselMediaQueryHandler.cs`
- `AdminVessels/Query/GetAdminVesselStatusHistoryQuery.cs`
- `AdminVessels/Query/GetAdminVesselStatusHistoryQueryHandler.cs`
- `IVesselAdminBffRemoteCall.cs` — added `GetVesselMedia` and `GetVesselStatusHistory` methods

### Inactive Module 501 Responses

New controller `AdminInactiveModulesController.cs` returns HTTP 501 with Aizen envelope for all inactive module paths:

| Path | Module | Resolution |
|------|--------|------------|
| `GET/POST /cargodry/kits*` | CargoDry | 501 |
| `GET/POST /notification-templates` | Notification | 501 |
| `GET /payments/transactions*`, `/payments/commissions` | Payment | 501 |
| `GET /reports`, `/reports/kpi`, `/analytics/dashboard` | Reporting | 501 |
| `GET /files` | FileStorage list | 501 |

## Validation

```
dotnet build Aizen.Bff.AdminPanel.csproj
Build succeeded. 0 Error(s)
```
