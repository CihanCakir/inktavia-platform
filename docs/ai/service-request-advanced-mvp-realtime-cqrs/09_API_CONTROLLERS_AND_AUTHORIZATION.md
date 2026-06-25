# Inktavia Marine OS - ServiceRequest Advanced MVP Realtime CQRS

Use this prompt with GitHub Copilot Agent inside the repository.

All generated implementation must be in English and must follow the existing Aizen/Inktavia architecture.

## Step 09 - API controllers, routes, authorization

Implement ServiceRequest API controllers using the existing controller/versioning/response conventions.

## Controller placement

Use existing API convention, likely:

```text
Aizen.Modules.ServiceRequest.Api/Controllers/V1
```

Adapt to actual repository convention.

## Suggested controllers

Create controllers by responsibility:

```text
ServiceRequestController
ServiceRequestOfferController
ServiceRequestAssignmentController
ServiceRequestMessageController
ServiceRequestWorkLogController
ServiceRequestCompletionController
ServiceRequestDisputeController
AdminServiceRequestController
ProviderServiceRequestController
```

If existing architecture prefers fewer controllers, group endpoints accordingly.

## Owner endpoints

Implement endpoints for:

```text
POST   /service-requests
PUT    /service-requests/{id}
POST   /service-requests/{id}/cancel
POST   /service-requests/{id}/attachments
DELETE /service-requests/{id}/attachments/{attachmentId}
GET    /service-requests/my
GET    /service-requests/my/{id}
GET    /vessels/{vesselId}/service-requests
GET    /service-requests/{id}/offers
POST   /service-requests/{id}/offers/{offerId}/accept
POST   /service-requests/{id}/offers/{offerId}/reject
GET    /service-requests/{id}/timeline
```

## Provider endpoints

Implement endpoints for:

```text
GET    /provider/service-requests/available
GET    /provider/service-requests/assigned
GET    /provider/service-requests/{id}
POST   /provider/service-requests/{id}/offers
PUT    /provider/service-requests/{id}/offers/{offerId}
POST   /provider/service-requests/{id}/offers/{offerId}/withdraw
POST   /provider/service-requests/{id}/assignments/{assignmentId}/accept
POST   /provider/service-requests/{id}/assignments/{assignmentId}/reject
POST   /provider/service-requests/{id}/assignments/{assignmentId}/start
POST   /provider/service-requests/{id}/work-logs
POST   /provider/service-requests/{id}/completion
```

## Shared message endpoints

Implement endpoints for:

```text
GET    /service-requests/{id}/messages
POST   /service-requests/{id}/messages
POST   /service-requests/{id}/messages/read
```

## Completion endpoints

Implement endpoints for:

```text
GET    /service-requests/{id}/completion
POST   /service-requests/{id}/completion/approve
POST   /service-requests/{id}/completion/reject
```

## Dispute endpoints

Implement endpoints for:

```text
POST   /service-requests/{id}/disputes
GET    /service-requests/{id}/disputes/{disputeId}
POST   /service-requests/{id}/disputes/{disputeId}/messages
POST   /service-requests/{id}/disputes/{disputeId}/attachments
```

## Admin endpoints

Implement endpoints for:

```text
GET    /admin/service-requests
GET    /admin/service-requests/{id}
GET    /admin/service-requests/dashboard
GET    /admin/service-requests/delayed
GET    /admin/service-requests/pending-completion-approval
GET    /admin/service-requests/disputes
POST   /admin/service-requests/{id}/assignments
PUT    /admin/service-requests/{id}/assignments/{assignmentId}
POST   /admin/service-requests/{id}/status
POST   /admin/service-requests/{id}/disputes/{disputeId}/status
POST   /admin/service-requests/{id}/disputes/{disputeId}/resolve
```

## Authorization

Use existing authorization policies/attributes. Logical access rules:

- Owner endpoints require authenticated boat owner/participant/user with vessel access.
- Provider endpoints require provider profile/team access.
- Admin endpoints require admin/operator role.
- Shared endpoints must check request participation.

Do not rely only on route-level authorization. Handlers/services must also enforce resource-level access.

## Output required

Create/update:

```text
ai/service-request-advanced-mvp-realtime-cqrs/reports/09_API_AUTHORIZATION_REPORT.md
```
