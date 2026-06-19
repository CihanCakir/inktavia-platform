# 05 — Update ServiceRequest Admin Module Controller Endpoints

Update the ServiceRequest module admin controller to expose required read endpoints.

## Required module routes

Use existing route conventions. Preferred conceptual endpoints:

```http
GET /api/v1/admin/service-requests
GET /api/v1/admin/service-requests/{id}
GET /api/v1/admin/service-requests/by-vessel/{vesselId}/history
GET /api/v1/admin/service-requests/{id}/timeline
GET /api/v1/admin/service-requests/{id}/offers
GET /api/v1/admin/service-requests/{id}/assignments
GET /api/v1/admin/service-requests/{id}/worklogs
GET /api/v1/admin/service-requests/{id}/completion-dispute
```

## Filtering

The list endpoint must support at least:
- `vesselId`
- `statuses[]`
- `serviceTypes[]` if available
- `providerId` if available
- `search`
- pagination

## Auth

Preserve existing module auth pattern. Do not weaken authorization.

## Output

Controller/action implementation plus query/request wiring.
