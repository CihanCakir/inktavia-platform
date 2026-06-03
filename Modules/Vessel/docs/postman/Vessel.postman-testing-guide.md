# Vessel Module — Postman Testing Guide

## Overview
Vessel module manages vessel records, locations, engines, media, documents, ownership and specifications. Port: 7105.

## Prerequisites

### Environment Variables Required
| Variable | Value | Notes |
|---|---|---|
| vessel_api_base_url | http://localhost:7105/api/v1 | |
| vessel_api_root_url | http://localhost:7105 | |
| active_access_token | (set by auth) | Required for most endpoints |
| vesselId | (set by Create Vessel) | Auto-set after creation |
| fileId | (manual) | Required for media/document endpoints |

### Services Required
- Keycloak (port 8080)
- Vessel API (port 7105)
- FileStorage API (port 7106) — needed for file references
- PostgreSQL

## Auth Setup
1. Run `00 - Auth Setup > Get Mobile Token` for user operations
2. Run `00 - Auth Setup > Get Admin Token` for admin operations
3. `vesselId` is automatically set when you run `Create Vessel`

## Test Sequence

### Basic Vessel CRUD
1. `01 - Vessel - CRUD > Create Vessel` → auto-sets `vesselId`
2. `01 - Vessel - CRUD > Get Vessel By ID` → verify creation
3. `01 - Vessel - CRUD > Update Vessel` → update name/callsign
4. `01 - Vessel - CRUD > Update Vessel Status` → set to InService
5. `01 - Vessel - CRUD > Get My Vessels` → list your vessels

### Location Management
1. `02 - Vessel - Location > Update Vessel Location`
2. `02 - Vessel - Location > Get Current Location`

### Engine Management
1. `03 - Vessel - Engines > Add Engine` → auto-sets `engineId`
2. `03 - Vessel - Engines > Get Engines` → verify
3. `03 - Vessel - Engines > Set Primary Engine`
4. `03 - Vessel - Engines > Update Engine`

### Media Management
Prerequisites: Upload a file first using FileStorage module to get `fileId`.
1. `04 - Vessel - Media > Add Media` (requires `fileId`) → auto-sets `mediaId`
2. `04 - Vessel - Media > Get Media`
3. `04 - Vessel - Media > Set Cover Media`

### Document Management
Prerequisites: Upload a file first to get `fileId`.
1. `08 - Vessel - Documents > Add Document` (requires `fileId`) → auto-sets `vesselDocumentId`
2. `08 - Vessel - Documents > Get Documents`
3. `08 - Vessel - Documents > Update Document Status`

### Specification
1. `07 - Vessel - Specification > Upsert Specification`
2. `07 - Vessel - Specification > Get Specification`

## Expected Responses

### Create Vessel
```json
HTTP 200 OK or 201 Created
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "MV Test Vessel",
  "vesselCode": "TCTEST",
  "status": "Active"
}
```

### Get Vessel By ID
```json
HTTP 200 OK
{
  "id": "...",
  "name": "MV Test Vessel",
  "vesselType": "CargoShip",
  "flag": "TR",
  "imo": "IMO9876543"
}
```

## Common Errors

| Error | Cause | Fix |
|---|---|---|
| 401 Unauthorized | Missing token | Run Get Mobile/Admin Token |
| 404 Not Found | Vessel doesn't exist | Check vesselId variable |
| 400 Bad Request | Invalid vessel data | Check IMO/MMSI format |
| 409 Conflict | IMO already registered | Use unique IMO number |

## Notes
- IMO format: IMO followed by 7 digits
- MMSI: 9 digit number
- Media/Document endpoints require valid `fileId` from FileStorage module
- Visibility options: Public, Private, PlatformOnly
