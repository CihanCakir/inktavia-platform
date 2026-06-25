# Vessel Register Bootstrap Endpoint Report

## Scope
Implementation of `GET /api/v1/admin-panel/vessels/register` — the bootstrap/options endpoint for the React Admin Web Vessel Register page.

## Problem
The React Admin Web route `/app/vessels/register` called `GET /api/v1/admin-panel/vessels/register` and received `404 Not Found`, rendering the error message "Gemi verileri yüklenemedi."

## Files Created / Modified

| File | Action |
|------|--------|
| `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminVessels/Dto/AdminVesselRegisterBootstrapBffResponse.cs` | Created — 7 new DTOs |
| `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminVessels/Query/GetAdminVesselRegisterBootstrapQuery.cs` | Created |
| `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminVessels/Query/GetAdminVesselRegisterBootstrapQueryHandler.cs` | Created |
| `Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Controllers/V1/AdminVesselsController.cs` | Modified — added `[HttpGet("vessels/register")]` |

## Endpoint

```
GET /api/v1/admin-panel/vessels/register
Authorization: AdminPanelAccess policy
X-Aizen-User-Token: Bearer <identityAccessToken>
```

## Response Shape

```json
{
  "vesselRegister": {
    "defaults": {
      "flagCountryCode": "TR",
      "assetType": 1,
      "operationalStatus": 1,
      "ownershipStatus": 1,
      "isArchived": false
    },
    "options": {
      "vesselTypes": [ { "code": "MOTOR_YACHT", "label": "Motor Yacht" }, ... ],
      "assetTypes": [ { "value": 1, "label": "Motor Yacht" }, ... ],
      "operationalStatuses": [ { "value": 1, "label": "In Service" }, ... ],
      "ownershipStatuses": [ { "value": 1, "label": "Private" }, ... ],
      "flagCountries": [ { "code": "TR", "label": "Türkiye" }, ... ],
      "buildCountries": [ ... ],
      "hullMaterials": [ { "code": "GRP", "label": "GRP (Glass Reinforced Plastic)" }, ... ],
      "superstructureMaterials": [ ... ],
      "homePorts": [],
      "ownerCandidates": [ { "userId": 10003, "profileId": 11003, "displayName": "Ayşe Demir", ... } ]
    }
  },
  "warnings": []
}
```

## Data Sources

| Option Group | Source |
|-------------|--------|
| `vesselTypes` | Static BFF list (no ReferenceData owner) |
| `assetTypes` | Static BFF enum mapping |
| `operationalStatuses` | Static BFF enum mapping |
| `ownershipStatuses` | Static BFF enum mapping |
| `flagCountries` / `buildCountries` | `ReferenceData GET /api/v1/locations/countries` |
| `hullMaterials` | Static BFF list |
| `superstructureMaterials` | Static BFF list |
| `homePorts` | Empty — ReferenceData marina/port lookup not implemented |
| `ownerCandidates` | `Identity GET /api/v1/identity/profiles` (pageSize=100) |

## Route Ordering
The literal route `[HttpGet("vessels/register")]` is declared **before** parameterized routes like `[HttpGet("vessels/{vesselId:long}")]` to prevent route ambiguity. ASP.NET Core attribute routing resolves literal segments before parameterized ones, so ordering is safe.

## Graceful Degradation
- If ReferenceData is unavailable: `flagCountries` / `buildCountries` return empty; warning added.
- If Identity is unavailable: `ownerCandidates` returns empty; warning added.
- If Keycloak service token acquisition fails: warning added, all module-dependent lists return empty.
- Static options (vesselTypes, assetTypes, etc.) always returned regardless of module availability.

## Build Validation
```
dotnet build Aizen.sln --no-incremental
→ 0 Error(s)
```

## Remaining Gaps
- `homePorts` is always empty — needs a ReferenceData marina/port lookup endpoint.
- `ownerCandidates` loads first 100 profiles without role filter — should add `roleContext=BoatOwner` filter once Identity supports it.
