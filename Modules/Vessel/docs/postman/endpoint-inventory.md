# Vessel Module — Endpoint Inventory

**Module:** Vessel  
**Port:** 7105  
**Base URL:** `{{vessel_api_base_url}}` = `http://localhost:7105/api/v1`  
**Auth:** `Authorization: Bearer {{active_access_token}}`  

---

## VesselController

**Route prefix:** `/api/v1/vessels`  
**Tag:** Vessel  
**Auth:** Bearer (most endpoints), AllowAnonymous (GET detail, GET by code)

| # | Method | Route | Auth | Request Body / Params | Response |
|---|---|---|---|---|---|
| 1 | POST | /api/v1/vessels | Bearer | CreateVesselRequest | Returns VesselId |
| 2 | PUT | /api/v1/vessels/{vesselId} | Bearer | UpdateVesselRequest | 200 OK |
| 3 | GET | /api/v1/vessels/{vesselId} | None | — | VesselDto |
| 4 | GET | /api/v1/vessels/code/{vesselCode} | None | — | VesselDto |
| 5 | GET | /api/v1/vessels/current-user | Bearer | pageIndex, pageSize | PagedResult<VesselDto> |
| 6 | PATCH | /api/v1/vessels/{vesselId}/archive | Bearer | ArchiveVesselRequest | 200 OK |
| 7 | PATCH | /api/v1/vessels/{vesselId}/restore | Bearer | — | 200 OK |
| 8 | PATCH | /api/v1/vessels/{vesselId}/status | Bearer | UpdateVesselStatusRequest | 200 OK |
| 9 | PATCH | /api/v1/vessels/{vesselId}/visibility | Bearer | VesselVisibility (enum) | 200 OK |

**Sample CreateVesselRequest:**
```json
{
  "name": "MV Test Vessel",
  "vesselType": "CargoShip",
  "flag": "TR",
  "imo": "IMO9876543",
  "mmsi": "271001234",
  "callSign": "TCTEST",
  "buildYear": 2015
}
```

**Sample UpdateVesselRequest:**
```json
{
  "name": "MV Test Vessel Updated",
  "flag": "TR",
  "callSign": "TCTEST2"
}
```

**Sample ArchiveVesselRequest:**
```json
{
  "reason": "Vessel decommissioned for maintenance"
}
```

**Sample UpdateVesselStatusRequest:**
```json
{
  "status": "InService"
}
```

**VesselVisibility enum values:** `Public`, `Private`, `PlatformOnly`

---

## VesselLocationController

**Route prefix:** `/api/v1/vessels/{vesselId}/location`  
**Tag:** Vessel - Location

| # | Method | Route | Auth | Request Body | Response |
|---|---|---|---|---|---|
| 1 | GET | /api/v1/vessels/{vesselId}/location/current | None | — | VesselLocationDto |
| 2 | PUT | /api/v1/vessels/{vesselId}/location | Bearer | UpdateVesselLocationSnapshotRequest | 200 OK |

**Sample UpdateVesselLocationSnapshotRequest:**
```json
{
  "latitude": 41.0082,
  "longitude": 28.9784,
  "heading": 245.5,
  "speedKnots": 12.3,
  "portId": null
}
```

---

## VesselEngineController

**Route prefix:** `/api/v1/vessels/{vesselId}/engines`  
**Tag:** Vessel - Engines

| # | Method | Route | Auth | Request Body | Response |
|---|---|---|---|---|---|
| 1 | GET | /api/v1/vessels/{vesselId}/engines | None | pageIndex, pageSize | PagedResult<VesselEngineDto> |
| 2 | POST | /api/v1/vessels/{vesselId}/engines | Bearer | AddVesselEngineRequest | Returns engineId |
| 3 | PUT | /api/v1/vessels/{vesselId}/engines/{engineId} | Bearer | UpdateVesselEngineRequest | 200 OK |
| 4 | DELETE | /api/v1/vessels/{vesselId}/engines/{engineId} | Bearer | — | 200 OK |
| 5 | PATCH | /api/v1/vessels/{vesselId}/engines/{engineId}/set-primary | Bearer | — | 200 OK |

**Sample AddVesselEngineRequest:**
```json
{
  "engineType": "MainEngine",
  "makeModel": "MAN B&W 6S60MC-C",
  "powerKw": 8580,
  "fuelType": "HFO",
  "manufacturedYear": 2015
}
```

---

## VesselMediaController

**Route prefix:** `/api/v1/vessels/{vesselId}/media`  
**Tag:** Vessel - Media

| # | Method | Route | Auth | Request Body / Params | Response |
|---|---|---|---|---|---|
| 1 | GET | /api/v1/vessels/{vesselId}/media | None | pageIndex, pageSize, includeAccessUrls, accessUrlExpiresInMinutes | PagedResult<VesselMediaDto> |
| 2 | POST | /api/v1/vessels/{vesselId}/media | Bearer | AddVesselMediaRequest | Returns mediaId |
| 3 | PUT | /api/v1/vessels/{vesselId}/media/{mediaId} | Bearer | UpdateVesselMediaRequest | 200 OK |
| 4 | DELETE | /api/v1/vessels/{vesselId}/media/{mediaId} | Bearer | — | 200 OK |
| 5 | PATCH | /api/v1/vessels/{vesselId}/media/{mediaId}/set-cover | Bearer | — | 200 OK |
| 6 | PATCH | /api/v1/vessels/{vesselId}/media/{mediaId}/sort-order | Bearer | int (sortOrder) | 200 OK |

**Sample AddVesselMediaRequest:**
```json
{
  "fileId": "{{fileId}}",
  "caption": "Vessel starboard view",
  "sortOrder": 1
}
```

---

## VesselOwnershipController

**Route prefix:** `/api/v1/vessels/{vesselId}/owners`  
**Tag:** Vessel - Ownership

| # | Method | Route | Auth | Request Body | Response |
|---|---|---|---|---|---|
| 1 | GET | /api/v1/vessels/{vesselId}/owners | Bearer | pageIndex, pageSize | PagedResult<VesselOwnerDto> |
| 2 | POST | /api/v1/vessels/{vesselId}/owners | Bearer | AddVesselOwnerRequest | Returns ownerId |
| 3 | PUT | /api/v1/vessels/{vesselId}/owners/{ownerId}/role | Bearer | UpdateVesselOwnerRoleRequest | 200 OK |
| 4 | DELETE | /api/v1/vessels/{vesselId}/owners/{ownerId} | Bearer | — | 200 OK |
| 5 | PATCH | /api/v1/vessels/{vesselId}/owners/{ownerId}/set-primary | Bearer | — | 200 OK |
| 6 | PATCH | /api/v1/vessels/{vesselId}/owners/accept-invitation | Bearer | — | 200 OK |
| 7 | PATCH | /api/v1/vessels/{vesselId}/owners/reject-invitation | Bearer | — | 200 OK |

**Sample AddVesselOwnerRequest:**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "role": "Captain"
}
```

---

## VesselStatusController

**Route prefix:** `/api/v1/vessels/{vesselId}/status-history`  
**Tag:** Vessel - Status

| # | Method | Route | Auth | Params | Response |
|---|---|---|---|---|---|
| 1 | GET | /api/v1/vessels/{vesselId}/status-history | Bearer | pageIndex, pageSize | PagedResult<VesselStatusHistoryDto> |

---

## VesselSpecificationController

**Route prefix:** `/api/v1/vessels/{vesselId}/specification`  
**Tag:** Vessel - Specification

| # | Method | Route | Auth | Request Body | Response |
|---|---|---|---|---|---|
| 1 | GET | /api/v1/vessels/{vesselId}/specification | None | — | VesselSpecificationDto |
| 2 | PUT | /api/v1/vessels/{vesselId}/specification | Bearer | UpsertVesselSpecificationRequest | 200 OK |
| 3 | DELETE | /api/v1/vessels/{vesselId}/specification | Bearer | — | 200 OK |

**Sample UpsertVesselSpecificationRequest:**
```json
{
  "lengthOverall": 185.5,
  "beam": 28.4,
  "draft": 11.2,
  "grossTonnage": 21450,
  "netTonnage": 9870,
  "deadweightTonnage": 28000,
  "hullMaterial": "Steel",
  "propulsionType": "MotorVessel"
}
```

---

## VesselDocumentController

**Route prefix:** `/api/v1/vessels/{vesselId}/documents`  
**Tag:** Vessel - Documents

| # | Method | Route | Auth | Request Body / Params | Response |
|---|---|---|---|---|---|
| 1 | GET | /api/v1/vessels/{vesselId}/documents | Bearer | pageIndex, pageSize, includeAccessUrls, accessUrlExpiresInMinutes | PagedResult<VesselDocumentDto> |
| 2 | POST | /api/v1/vessels/{vesselId}/documents | Bearer | AddVesselDocumentRequest | Returns documentId |
| 3 | PUT | /api/v1/vessels/{vesselId}/documents/{documentId} | Bearer | UpdateVesselDocumentRequest | 200 OK |
| 4 | DELETE | /api/v1/vessels/{vesselId}/documents/{documentId} | Bearer | — | 200 OK |
| 5 | PATCH | /api/v1/vessels/{vesselId}/documents/{documentId}/status | Bearer | VesselDocumentStatus (enum) | 200 OK |

**Sample AddVesselDocumentRequest:**
```json
{
  "fileId": "{{fileId}}",
  "documentType": "SafetyManagementCertificate",
  "title": "ISM Safety Management Certificate",
  "issuedAt": "2023-06-15T00:00:00Z",
  "expiresAt": "2028-06-14T00:00:00Z"
}
```

---

## VesselAdminController

**Route prefix:** `/api/v1/admin/vessels`  
**Tag:** Admin - Vessel  
**Auth:** Bearer (Admin)

| # | Method | Route | Params | Response |
|---|---|---|---|---|
| 1 | GET | /api/v1/admin/vessels | pageIndex, pageSize, searchTerm, isArchived | PagedResult<VesselAdminDto> |
