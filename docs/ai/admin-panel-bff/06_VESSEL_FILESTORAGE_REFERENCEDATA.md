# 06 - Vessel, FileStorage, and ReferenceData Admin BFF Endpoints

## Goal

Create Admin Panel BFF endpoints for Vessel, FileStorage, and ReferenceData support screens.

## Vessel Admin BFF

Inspect:

```text
Modules/Vessel/docs/postman/endpoint-inventory.md
Modules/Vessel/docs/postman/postman-validation-report.md
Aizen.Modules.Vessel.Abstraction
```

Candidate endpoints:

```text
GET /api/v1/admin-panel/vessels
GET /api/v1/admin-panel/vessels/{vesselId}
GET /api/v1/admin-panel/vessels/{vesselId}/documents
GET /api/v1/admin-panel/vessels/{vesselId}/media
GET /api/v1/admin-panel/vessels/{vesselId}/service-requests
POST /api/v1/admin-panel/vessels/{vesselId}/status/change
POST /api/v1/admin-panel/vessels/{vesselId}/archive
```

Generate only if supported.

## FileStorage Admin BFF

Inspect:

```text
Modules/FileStorage/docs/postman/endpoint-inventory.md
Modules/FileStorage/docs/postman/postman-validation-report.md
Aizen.Modules.FileStorage.Abstraction
```

Candidate endpoints:

```text
GET /api/v1/admin-panel/files
GET /api/v1/admin-panel/files/{fileId}
POST /api/v1/admin-panel/files/{fileId}/read-url
POST /api/v1/admin-panel/files/{fileId}/access-policy
POST /api/v1/admin-panel/files/{fileId}/soft-delete
```

Generate only if supported.

## ReferenceData Admin BFF

Inspect:

```text
Modules/ReferenceData/docs/postman/endpoint-inventory.md
Modules/ReferenceData/docs/postman/postman-validation-report.md
Aizen.Modules.ReferenceData.Abstraction
```

Candidate endpoints:

```text
GET /api/v1/admin-panel/reference-data/lookup-groups
GET /api/v1/admin-panel/reference-data/lookup-groups/tree
GET /api/v1/admin-panel/reference-data/lookup-items/by-group/{lookupGroupId}
GET /api/v1/admin-panel/reference-data/currencies
GET /api/v1/admin-panel/reference-data/countries
GET /api/v1/admin-panel/reference-data/cities
```

Generate only if supported.

## Requirements

- Use `AizenRemoteCall`.
- Forward auth headers.
- Use active module Abstraction DTOs where possible.
- BFF-specific DTOs are allowed for admin list/detail screens.
- Do not activate Payment/Profile.

## Output

Update docs:

```text
Bff/src/AdminPanel/Aizen.Bff.AdminPanel/docs/admin-panel-bff-vessel-filestorage-referencedata-map.md
```
