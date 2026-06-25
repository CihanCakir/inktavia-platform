# 07 — Implement AdminPanel BFF ServiceRequest Handlers and Controller

Implement BFF query handlers/controller endpoints using existing AdminPanel BFF patterns.

## Expected BFF endpoints

```http
GET /api/v1/admin-panel/service-requests
GET /api/v1/admin-panel/service-requests/{id}
GET /api/v1/admin-panel/service-requests/by-vessel/{vesselId}/history
GET /api/v1/admin-panel/service-requests/{id}/timeline
GET /api/v1/admin-panel/service-requests/{id}/offers
GET /api/v1/admin-panel/service-requests/{id}/assignments
GET /api/v1/admin-panel/service-requests/{id}/worklogs
GET /api/v1/admin-panel/service-requests/{id}/completion-dispute
```

Use actual route conventions from existing AdminPanel BFF. Do not create duplicate conflicting routes.

## Behavior

- Return existing Aizen BFF envelope.
- Use `[Authorize(Policy = "AdminPanelAccess")]` or actual existing policy.
- Forward `X-Aizen-User-Token` and service token through remote client infrastructure.
- Map module DTOs into BFF UI-ready DTOs.
- If ServiceRequest module is unavailable, list/detail endpoints may return error; Vessel aggregate must degrade gracefully.

## Output

BFF controller + query/handler/DTO implementation report.
