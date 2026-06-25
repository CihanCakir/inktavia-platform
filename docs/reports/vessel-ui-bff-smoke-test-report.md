# Vessel UI BFF Smoke Test Report

## Build Validation

```bash
cd /Users/cihancakir/Desktop/Mine/DEV/addesso-project
dotnet restore
dotnet build
```

**Result:** ✅ Build succeeded — 0 errors, 848 warnings (all pre-existing, unrelated to vessel UI work)

## EF Migration

```bash
cd /Users/cihancakir/Desktop/Mine/DEV/addesso-project
dotnet ef migrations add AddVesselUiContractFields \
  --project Modules/Vessel/src/Aizen.Modules.Vessel.Repository \
  --startup-project Modules/Vessel/src/Aizen.Modules.Vessel
```

**Result:** ✅ Migration `20260619075529_AddVesselUiContractFields` generated successfully

**Tables affected:**
- `vessel.vessels` — added `AssetType`, `OperationalStatus`
- `vessel.vessel_specifications` — added `BuildCountry`, `SuperstructureMaterial`, `GrossTonnage`, `NetTonnage`, `PassengerCapacity`, `CrewCapacity`
- `vessel.vessel_documents` — added `DocumentCategory`, `IssuingAuthority`, `ApprovedAt`, `ApprovedByUserId`
- `vessel.vessel_media` — added `Title`, `Description`, `ThumbnailUrl`, `UploadedByUserId`
- `vessel.vessel_engines` — added `PropulsionType`, `EnginePowerKw`, `FuelCapacityL`, `MaxSpeedKnots`, `CruisingSpeedKnots`, `RangeNm`

## Apply Migration

```bash
# Ensure PostgreSQL is running and connection string is set, then:
dotnet ef database update \
  --project Modules/Vessel/src/Aizen.Modules.Vessel.Repository \
  --startup-project Modules/Vessel/src/Aizen.Modules.Vessel
```

## Smoke Test Instructions

### Prerequisites
1. All services running: Identity (7101), Vessel (7105), ServiceRequest (7107), AdminPanel BFF
2. Keycloak running (for BFF service token acquisition)
3. Valid Identity access token obtained via `/api/v1/auth/login/username`

### MVP Endpoint Tests

#### 1. Vessel List
```bash
curl -X GET "http://localhost:<BFF_PORT>/api/v1/admin-panel/vessels?pageIndex=0&pageSize=20" \
  -H "X-Aizen-User-Token: Bearer <identity_token>" \
  -H "Authorization: Bearer <keycloak_bff_token>"
```

Expected:
```json
{
  "header": { "isSuccess": true },
  "body": {
    "vessels": {
      "index": 0, "size": 20, "count": N,
      "items": [...]
    },
    "warnings": []
  }
}
```

#### 2. Vessel List with Filters
```bash
curl -X GET "http://localhost:<BFF_PORT>/api/v1/admin-panel/vessels?assetTypes=1&operationalStatuses=1" \
  -H "X-Aizen-User-Token: Bearer <identity_token>"
```

#### 3. Vessel Detail
```bash
curl -X GET "http://localhost:<BFF_PORT>/api/v1/admin-panel/vessels/1" \
  -H "X-Aizen-User-Token: Bearer <identity_token>"
```

Expected:
```json
{
  "header": { "isSuccess": true },
  "body": {
    "vessel": {
      "id": 1,
      "name": "...",
      "engine": {...},
      "cargoDryKits": [],
      "serviceHistory": [...],
      "documentSummaries": [...]
    },
    "warnings": []
  }
}
```

#### 4. Vessel Documents
```bash
curl -X GET "http://localhost:<BFF_PORT>/api/v1/admin-panel/vessels/1/documents" \
  -H "X-Aizen-User-Token: Bearer <identity_token>"
```

Expected:
```json
{
  "body": {
    "documents": [
      {
        "id": 1,
        "documentType": "...",
        "daysUntilExpiry": 270,
        "documentStatus": "valid",
        ...
      }
    ]
  }
}
```

#### 5. Vessel Media
```bash
curl -X GET "http://localhost:<BFF_PORT>/api/v1/admin-panel/vessels/1/media" \
  -H "X-Aizen-User-Token: Bearer <identity_token>"
```

Expected:
```json
{
  "body": {
    "media": [
      {
        "id": 1,
        "mediaType": "image",
        "sortOrder": 0,
        "isPrimary": false,
        ...
      }
    ]
  }
}
```

## Degraded Mode Behavior (if Vessel module is down)

All 4 MVP endpoints return warnings instead of hard failures:
```json
{ "body": { "warnings": [{ "module": "Vessel", "message": "Vessel service is currently unavailable." }] } }
```

## Remaining Manual Steps

| Step | Command |
|------|---------|
| Apply DB migration | `dotnet ef database update ...` (above) |
| Restart Vessel module | `dotnet run` in `Modules/Vessel/src/Aizen.Modules.Vessel` |
| Restart AdminPanel BFF | `dotnet run` in `Bff/src/AdminPanel/Aizen.Bff.AdminPanel` |
