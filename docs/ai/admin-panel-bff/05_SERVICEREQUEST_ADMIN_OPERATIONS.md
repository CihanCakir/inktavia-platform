# 05 - ServiceRequest Admin Operations

## Goal

Create Admin Panel BFF endpoints for ServiceRequest operations by orchestrating the ServiceRequest internal API through `AizenRemoteCall`.

## Source of truth

Inspect:

```text
Modules/ServiceRequest/docs/postman/endpoint-inventory.md
Modules/ServiceRequest/docs/postman/postman-validation-report.md
Modules/ServiceRequest/src/**/Controller/**/*.cs
Modules/ServiceRequest/src/**/Controllers/**/*.cs
Aizen.Modules.ServiceRequest.Abstraction
```

## Candidate BFF endpoints

Generate only endpoints supported by actual internal API contracts.

```text
GET /api/v1/admin-panel/service-requests
GET /api/v1/admin-panel/service-requests/{serviceRequestId}
GET /api/v1/admin-panel/service-requests/{serviceRequestId}/timeline
GET /api/v1/admin-panel/service-requests/{serviceRequestId}/offers
GET /api/v1/admin-panel/service-requests/{serviceRequestId}/assignments
GET /api/v1/admin-panel/service-requests/{serviceRequestId}/work-logs
GET /api/v1/admin-panel/service-requests/{serviceRequestId}/messages
GET /api/v1/admin-panel/service-requests/pending-completions
GET /api/v1/admin-panel/service-requests/disputes
GET /api/v1/admin-panel/service-requests/disputes/{disputeId}
GET /api/v1/admin-panel/service-requests/delayed
POST /api/v1/admin-panel/service-requests/{serviceRequestId}/status/change
POST /api/v1/admin-panel/service-requests/completions/{completionId}/approve
POST /api/v1/admin-panel/service-requests/completions/{completionId}/reject
POST /api/v1/admin-panel/service-requests/disputes/{disputeId}/status/change
POST /api/v1/admin-panel/service-requests/disputes/{disputeId}/resolve
```

## Aggregated detail response

For `GET /service-requests/{serviceRequestId}`, aggregate where supported:

- request detail
- vessel summary if available
- owner/user summary if available
- attachments
- offers
- assignment
- status history/timeline
- messages preview
- work logs preview
- completion state
- dispute state

Do not call missing endpoints. Document missing sections.

## Admin BFF response DTOs

Suggested BFF DTOs:

```text
AdminServiceRequestListResponse
AdminServiceRequestListItemResponse
AdminServiceRequestDetailResponse
AdminServiceRequestTimelineItemResponse
AdminPendingCompletionListResponse
AdminDisputeListResponse
AdminDisputeDetailResponse
AdminDelayedServiceRequestListResponse
```

## Requirements

- Use ServiceRequest Abstraction classes when available.
- Use BFF DTOs for screen-specific aggregation.
- Do not implement ServiceRequest domain rules inside BFF.
- Use `AizenRemoteCall` only.
- Forward `Authorization` and `X-Aizen-User-Token`.
- Add Postman tests and variable extraction for important IDs.

## Output

Update docs:

```text
Bff/src/AdminPanel/Aizen.Bff.AdminPanel/docs/admin-panel-bff-servicerequest-map.md
```
