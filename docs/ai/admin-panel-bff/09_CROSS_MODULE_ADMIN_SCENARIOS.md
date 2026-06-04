# 09 - Cross-Module Admin Query Scenarios

## Goal

Extend the Admin Panel BFF with explicit cross-module query/orchestration endpoints.

The Admin Panel BFF must not behave as a simple proxy. It must expose AdminPanel-specific aggregate read models by orchestrating active internal APIs through `AizenRemoteCall`.

## Target projects

```text
Bff/src/AdminPanel/Aizen.Bff.AdminPanel
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application
```

## Active modules

Use only active modules:

- Identity
- ReferenceData
- Vessel
- FileStorage
- ServiceRequest

Skip inactive modules:

- Payment
- Profile

Payment/Profile-related fields may be returned only as future placeholders when they already exist in DTOs or UI contracts.

## Required discovery sources

Before implementing, inspect:

```text
Modules/Identity/docs/postman/endpoint-inventory.md
Modules/Identity/docs/postman/postman-validation-report.md
Modules/ReferenceData/docs/postman/endpoint-inventory.md
Modules/ReferenceData/docs/postman/postman-validation-report.md
Modules/Vessel/docs/postman/endpoint-inventory.md
Modules/Vessel/docs/postman/postman-validation-report.md
Modules/FileStorage/docs/postman/endpoint-inventory.md
Modules/FileStorage/docs/postman/postman-validation-report.md
Modules/ServiceRequest/docs/postman/endpoint-inventory.md
Modules/ServiceRequest/docs/postman/postman-validation-report.md
```

Also inspect controller source files when docs are incomplete:

```text
Modules/**/src/**/Controller/**/*.cs
Modules/**/src/**/Controllers/**/*.cs
```

## Non-negotiable BFF rules

- Do not implement domain business rules in the BFF.
- Do not duplicate internal module business logic.
- Do not access internal module databases directly.
- Use `AizenRemoteCall` for all module calls.
- Forward both authentication contexts to every internal API call:
  - `Authorization: Bearer {current Keycloak token}`
  - `X-Aizen-User-Token: {current Identity user token}`
- Use active modules' Abstraction class libraries where available.
- Every public class, interface, query, handler, controller and DTO must include `DocumentationInfo` according to the repository convention.
- All BFF responses must be typed DTOs. Do not return `object`.

## Application organization

Create or extend this structure:

```text
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminDashboard/Query
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminUsers/Query
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminVessels/Query
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminServiceRequests/Query
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminReferenceOptions/Query
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/Common/RemoteClients
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/Common/Mapping
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/Common/Warnings
```

## Controllers

Create or extend these BFF controllers:

```text
AdminDashboardController
AdminUsersController
AdminVesselsController
AdminServiceRequestsController
AdminReferenceOptionsController
```

Controllers must call Application queries only. They must not call `AizenRemoteCall` directly.

---

# Scenario 1 - Admin Dashboard Overview

## Endpoint

```http
GET /api/v1/admin-panel/dashboard/overview
```

## Purpose

Provide one aggregated dashboard contract for the AdminPanel home screen.

## Aggregate from

Identity:
- total users if available
- active users if available
- pending organizer/venue/provider approvals if available

Vessel:
- total vessels
- active vessels
- archived vessels
- vessels missing required documents if available

ServiceRequest:
- open service requests
- emergency/urgent requests
- waiting offer count
- pending assignment count
- in-progress count
- waiting owner approval count
- open dispute count
- delayed requests
- recent activity

FileStorage:
- pending file review if available
- failed file processing count if available

ReferenceData:
- optional display names for statuses/categories when needed

## Response sections

```text
identitySummary
vesselSummary
serviceRequestSummary
fileStorageSummary
recentActivity
warnings
```

## Failure handling

- If one optional source fails, return partial result with `warnings`.
- If all critical sources fail, return a proper BFF error response.

---

# Scenario 2 - Admin User Overview

## Endpoint

```http
GET /api/v1/admin-panel/users/{userId}/overview
```

## Purpose

Show the AdminPanel user detail screen with the user's profile, roles, vessels, ownerships and active operations.

## Aggregate from

Identity:
- user profile detail
- user profiles by role/context if available
- user roles

Vessel:
- vessels owned/managed by the user
- ownership role per vessel
- current vessel status

ServiceRequest:
- active service requests created by the user
- service requests where the user is an actor if supported
- open disputes related to the user's requests

ReferenceData:
- vessel type/status display names
- service request status/category display names when available

## Response sections

```text
user
profiles
roles
vessels
serviceRequests
summary
warnings
```

---

# Scenario 3 - Admin Vessel Overview

## Endpoint

```http
GET /api/v1/admin-panel/vessels/{vesselId}/overview
```

## Purpose

Show the AdminPanel vessel detail screen with enriched ReferenceData values and owner/user context.

## Aggregate from

Vessel:
- vessel detail
- ownership list
- specification
- engines
- location snapshot
- status history if available

Identity:
- owner/manager/captain user summaries

ReferenceData:
- vessel type
- vessel usage type
- hull material
- engine type
- fuel type
- vessel status
- document type labels
- location labels if needed

ServiceRequest:
- active service requests for the vessel
- last service request summary
- open dispute count for vessel-related requests

## Response sections

```text
vessel
owners
specification
engines
location
activeServiceRequests
statusHistory
summary
warnings
```

---

# Scenario 4 - Admin Vessel Documents

## Endpoint

```http
GET /api/v1/admin-panel/vessels/{vesselId}/documents
```

## Purpose

Return vessel documents enriched with FileStorage metadata and signed read URLs.

## Aggregate from

Vessel:
- vessel document list

FileStorage:
- file metadata for each document
- read signed URL for each file

ReferenceData:
- vessel document type names

## Response sections

```text
vesselId
documents
warnings
```

## Failure handling

If signed URL generation fails for a file:

- keep document metadata
- set `readUrl = null`
- add a warning with fileId/documentId

---

# Scenario 5 - Admin ServiceRequest Operation Detail

## Endpoint

```http
GET /api/v1/admin-panel/service-requests/{serviceRequestId}/operation-detail
```

## Purpose

Provide the full AdminPanel operational detail screen for one ServiceRequest.

## Aggregate from

ServiceRequest:
- request detail
- status history
- offers
- offer items
- assignment
- messages preview
- work logs
- completion
- dispute
- attachments
- timeline

Vessel:
- vessel detail/summary

Identity:
- owner user summary
- provider actor/user summaries if available
- admin actor summaries if available

ReferenceData:
- service category/type names
- urgency/status display values
- dispute reason/status display values when applicable

FileStorage:
- attachment metadata
- read signed URLs

## Response sections

```text
serviceRequest
vessel
owner
provider
statusHistory
offers
assignment
messagesPreview
workLogs
completion
dispute
attachments
timeline
warnings
```

---

# Scenario 6 - Admin ServiceRequest Timeline

## Endpoint

```http
GET /api/v1/admin-panel/service-requests/{serviceRequestId}/timeline
```

## Purpose

Return a normalized timeline that merges status history, messages, work logs, completion and dispute events.

## Aggregate from

ServiceRequest:
- status history
- messages
- work logs
- completion events
- dispute events

Identity:
- actor summaries

FileStorage:
- attachment metadata for timeline items when needed

## Response sections

```text
serviceRequestId
items
warnings
```

## Timeline item types

```text
StatusChanged
MessageSent
OfferCreated
OfferAccepted
AssignmentCreated
WorkLogAdded
CompletionSubmitted
CompletionApproved
DisputeOpened
DisputeResolved
AdminAction
SystemEvent
```

---

# Scenario 7 - Admin ServiceRequest Filter Options

## Endpoint

```http
GET /api/v1/admin-panel/service-requests/filter-options
```

## Purpose

Provide all dropdown/filter values for the AdminPanel ServiceRequest list/detail screens.

## Aggregate from

ReferenceData:
- service provider categories
- maintenance service types
- cleaning service types
- emergency service types
- vessel types
- vessel statuses

ServiceRequest Abstraction/enums:
- service request statuses
- urgency values
- offer statuses
- assignment statuses
- completion statuses
- dispute reasons
- dispute statuses
- message types
- work log types

## Response sections

```text
serviceCategories
serviceTypes
urgencies
requestStatuses
offerStatuses
assignmentStatuses
completionStatuses
disputeReasons
disputeStatuses
workLogTypes
messageTypes
warnings
```

---

# Scenario 8 - Admin Vessel Form Options

## Endpoint

```http
GET /api/v1/admin-panel/vessels/form-options
```

## Purpose

Provide all dropdown/form values for AdminPanel vessel creation/edit/review screens.

## Aggregate from

ReferenceData:
- vessel types
- vessel usage types
- hull materials
- engine types
- fuel types
- vessel statuses
- vessel document types
- measurement units
- countries/cities/districts if available

## Response sections

```text
vesselTypes
vesselUsageTypes
hullMaterials
engineTypes
fuelTypes
vesselStatuses
documentTypes
measurementUnits
locations
warnings
```

---

# Scenario 9 - Admin File Review Overview

## Endpoint

```http
GET /api/v1/admin-panel/files/{fileId}/review-overview
```

## Purpose

Show an AdminPanel file review screen with owner context and related domain usage.

## Aggregate from

FileStorage:
- file metadata
- read signed URL
- file owner
- content type/size/status

Vessel:
- related vessel document/media if file is used by Vessel

ServiceRequest:
- related request attachment/worklog/completion/dispute if file is used by ServiceRequest

Identity:
- uploader/owner user summary if available

## Response sections

```text
file
readUrl
relatedVessels
relatedServiceRequests
owner
warnings
```

---

# Required Postman outputs

Generate/update:

```text
Bff/src/AdminPanel/docs/postman/admin-panel-bff-cross-module-endpoint-inventory.md
Bff/src/AdminPanel/docs/postman/AdminPanelBff.CrossModuleQueries.postman_collection.json
Bff/src/AdminPanel/docs/postman/admin-panel-bff-cross-module-validation-report.md
```

Each request must include:

```text
Authorization: Bearer {{active_access_token}}
X-Aizen-User-Token: {{X-Aizen-User-Token}}
```

Each test script must validate:

- status is not 500
- JSON response when response body exists
- `header.isSuccess` when Metropol/Aizen response wrapper is used
- `body` exists
- expected aggregate sections exist

---

# Final report

Generate:

```text
Bff/src/AdminPanel/docs/admin-panel-bff-cross-module-query-scenarios-final-report.md
```

Include:

- endpoints created
- handlers created
- DTOs created
- internal module endpoints used
- missing/blocked internal endpoints
- fallback/warning behavior
- Postman files created
- build result
