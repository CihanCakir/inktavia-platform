# 10 - Cross-Module Admin Command Scenarios

## Goal

Extend the Admin Panel BFF with explicit cross-module command/orchestration endpoints.

These endpoints must support AdminPanel operational actions that may require validation/enrichment across Identity, ReferenceData, Vessel, FileStorage and ServiceRequest modules.

## Critical rule

The BFF is not a domain owner.

The BFF may:

- validate that required input exists
- orchestrate multiple active internal API calls
- forward auth headers
- shape request/response models for AdminPanel
- return warnings for partial optional failures
- provide idempotency/correlation information where supported

The BFF must not:

- bypass internal API business rules
- directly update module databases
- invent domain state transitions
- create Payment/Profile flows while those modules are inactive
- call inactive modules except to document future placeholders

## Auth forwarding

Every internal request must forward:

```text
Authorization: Bearer {current Keycloak token}
X-Aizen-User-Token: {current Identity user token}
X-Correlation-Id: {current correlation id if available}
```

Use the existing Aizen info/accessor and `AizenRemoteCall` conventions discovered in the repository.

## Discovery-first rule

For every command scenario below:

1. Discover whether the required internal endpoint exists by reading:

```text
Modules/**/docs/postman/endpoint-inventory.md
Modules/**/docs/postman/postman-validation-report.md
Modules/**/src/**/Controller/**/*.cs
Modules/**/src/**/Controllers/**/*.cs
```

2. If all required active internal endpoints exist, implement the BFF command endpoint.
3. If a required internal endpoint does not exist, do not fake the action. Document it in the blocked scenarios section and create only TODO documentation.
4. If an optional enrichment endpoint is missing, implement the core command and return warnings.

## Application organization

Create or extend this structure:

```text
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminUsers/Command
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminVessels/Command
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminServiceRequests/Command
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminFiles/Command
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminOperations/Command
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/Common/RemoteClients
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/Common/Idempotency
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/Common/Correlation
```

Every command must return a typed response DTO. Do not use `object`.

---

# Command Scenario Group A - Identity/Admin User Operations

## A1 - Approve organizer profile through AdminPanel BFF

Candidate endpoint:

```http
POST /api/v1/admin-panel/users/{userId}/organizer-profiles/{profileId}/approve
```

Internal orchestration:

1. Identity API: validate admin context if required by existing API.
2. Identity API: approve organizer profile.
3. Identity API: fetch updated user/profile summary if endpoint exists.
4. Return typed approval response.

Response sections:

```text
success
userId
profileId
newStatus
updatedProfile
warnings
```

## A2 - Reject organizer profile with reason

Candidate endpoint:

```http
POST /api/v1/admin-panel/users/{userId}/organizer-profiles/{profileId}/reject
```

Request fields:

```text
reason
adminNote
```

Internal orchestration:

- Identity API reject organizer profile
- optional updated profile fetch

## A3 - Approve venue profile through AdminPanel BFF

Candidate endpoint:

```http
POST /api/v1/admin-panel/users/{userId}/venue-profiles/{profileId}/approve
```

Internal orchestration:

- Identity API approve venue profile
- optional updated profile fetch

## A4 - Reject venue profile with reason

Candidate endpoint:

```http
POST /api/v1/admin-panel/users/{userId}/venue-profiles/{profileId}/reject
```

## A5 - Admin refresh user overview after profile action

If the AdminPanel UI needs a command-like operation that returns refreshed aggregate data after A1-A4, create typed command responses that include a minimal updated user overview section.

Do not duplicate `GET /users/{userId}/overview` logic in every handler. Reuse an Application query/service if available.

---

# Command Scenario Group B - Vessel Admin Operations

## B1 - Admin update vessel status

Candidate endpoint:

```http
POST /api/v1/admin-panel/vessels/{vesselId}/status
```

Request fields:

```text
status
reason
adminNote
```

Internal orchestration:

1. Vessel API: get vessel detail or validate vessel exists.
2. Vessel API: call existing update/change status endpoint.
3. ServiceRequest API: optionally fetch active requests for the vessel to return warnings when needed.
4. Return updated vessel status.

Warnings:

- vessel has active service requests
- ReferenceData status enrichment failed

## B2 - Admin archive vessel

Candidate endpoint:

```http
POST /api/v1/admin-panel/vessels/{vesselId}/archive
```

Request fields:

```text
reason
adminNote
```

Internal orchestration:

- Vessel API archive command if available
- optional ServiceRequest active request check

Do not implement archive if Vessel module does not expose an archive endpoint.

## B3 - Admin restore vessel

Candidate endpoint:

```http
POST /api/v1/admin-panel/vessels/{vesselId}/restore
```

Implement only if active internal endpoint exists.

## B4 - Admin assign vessel owner/role

Candidate endpoint:

```http
POST /api/v1/admin-panel/vessels/{vesselId}/ownerships
```

Request fields:

```text
userId
role
isPrimary
adminNote
```

Internal orchestration:

1. Identity API: validate user/profile summary.
2. Vessel API: add ownership/role assignment.
3. Vessel API: fetch updated ownership list.

## B5 - Admin update vessel owner/role

Candidate endpoint:

```http
PUT /api/v1/admin-panel/vessels/{vesselId}/ownerships/{ownershipId}
```

Request fields:

```text
role
isPrimary
adminNote
```

## B6 - Admin revoke vessel owner/role

Candidate endpoint:

```http
DELETE /api/v1/admin-panel/vessels/{vesselId}/ownerships/{ownershipId}
```

Request fields:

```text
reason
adminNote
```

## B7 - Admin create vessel document review request

Candidate endpoint:

```http
POST /api/v1/admin-panel/vessels/{vesselId}/documents/{documentId}/review-request
```

Purpose:

Ask the owner to re-upload or correct a document.

Internal orchestration:

- Vessel API: validate vessel document exists
- FileStorage API: validate file metadata if fileId exists
- Vessel API or Notification placeholder: add admin note/request if endpoint exists

If no active endpoint exists for document review request, document as blocked/future.

---

# Command Scenario Group C - Vessel + FileStorage Operations

## C1 - Admin generate read URL for vessel document

Candidate endpoint:

```http
POST /api/v1/admin-panel/vessels/{vesselId}/documents/{documentId}/read-url
```

Internal orchestration:

1. Vessel API: get document detail/list and find document.
2. FileStorage API: generate read signed URL for document.fileId.
3. Return file metadata + read URL.

This command is allowed because signed URLs are time-bound operational actions.

## C2 - Admin remove vessel document attachment

Candidate endpoint:

```http
DELETE /api/v1/admin-panel/vessels/{vesselId}/documents/{documentId}
```

Internal orchestration:

- Vessel API: remove document relation or soft-delete document
- FileStorage API: do not hard-delete file unless internal endpoint explicitly supports safe soft-delete and the domain rules allow it

## C3 - Admin regenerate service request attachment read URLs

Candidate endpoint:

```http
POST /api/v1/admin-panel/service-requests/{serviceRequestId}/attachments/read-urls
```

Internal orchestration:

- ServiceRequest API: get attachments
- FileStorage API: generate read signed URLs for each file

---

# Command Scenario Group D - ServiceRequest Admin Operations

## D1 - Admin change service request status

Candidate endpoint:

```http
POST /api/v1/admin-panel/service-requests/{serviceRequestId}/status
```

Request fields:

```text
status
reason
adminNote
```

Internal orchestration:

1. ServiceRequest API: get detail/validate exists.
2. ServiceRequest API: call change status command if available.
3. ServiceRequest API: get updated status history if available.
4. Return updated status response.

Do not invent state transition rules in BFF. The ServiceRequest module must validate allowed transitions.

## D2 - Admin cancel service request

Candidate endpoint:

```http
POST /api/v1/admin-panel/service-requests/{serviceRequestId}/cancel
```

Request fields:

```text
reason
adminNote
```

Internal orchestration:

- ServiceRequest API cancel command
- optional updated detail fetch

## D3 - Admin reopen service request

Candidate endpoint:

```http
POST /api/v1/admin-panel/service-requests/{serviceRequestId}/reopen
```

Implement only if ServiceRequest module exposes a reopen/reactivate endpoint.

## D4 - Admin update service request priority/urgency

Candidate endpoint:

```http
POST /api/v1/admin-panel/service-requests/{serviceRequestId}/urgency
```

Request fields:

```text
urgency
reason
adminNote
```

## D5 - Admin add internal note/system message

Candidate endpoint:

```http
POST /api/v1/admin-panel/service-requests/{serviceRequestId}/admin-notes
```

Request fields:

```text
message
visibility
attachmentFileIds
```

Internal orchestration:

- ServiceRequest API: send message/add admin note if available
- FileStorage API: validate attachments if fileIds provided

## D6 - Admin assign provider/team to service request

Candidate endpoint:

```http
POST /api/v1/admin-panel/service-requests/{serviceRequestId}/assignments
```

Request fields:

```text
providerProfileId
assignedTeamMemberId
plannedStartAt
plannedEndAt
adminNote
```

Internal orchestration:

1. ServiceRequest API: validate request/detail.
2. Identity API: validate provider user/profile only if provider identity/profile endpoint exists.
3. ServiceRequest API: create assignment.
4. ServiceRequest API: fetch updated assignment/detail.

If ProviderOperations/Profile module is inactive and provider validation is unavailable, skip provider enrichment and document limitation.

## D7 - Admin reassign service request

Candidate endpoint:

```http
POST /api/v1/admin-panel/service-requests/{serviceRequestId}/assignments/{assignmentId}/reassign
```

Request fields:

```text
newProviderProfileId
newAssignedTeamMemberId
reason
adminNote
```

Implement only if ServiceRequest module supports assignment update/reassignment.

## D8 - Admin start/force-start assignment

Candidate endpoint:

```http
POST /api/v1/admin-panel/service-requests/{serviceRequestId}/assignments/{assignmentId}/start
```

## D9 - Admin complete/force-complete assignment

Candidate endpoint:

```http
POST /api/v1/admin-panel/service-requests/{serviceRequestId}/assignments/{assignmentId}/complete
```

Use only existing ServiceRequest assignment endpoints.

---

# Command Scenario Group E - Offer Operations

## E1 - Admin withdraw invalid offer

Candidate endpoint:

```http
POST /api/v1/admin-panel/service-requests/{serviceRequestId}/offers/{offerId}/withdraw
```

Request fields:

```text
reason
adminNote
```

Internal orchestration:

- ServiceRequest API withdraw offer if available
- optional updated offer list fetch

## E2 - Admin reject suspicious offer

Candidate endpoint:

```http
POST /api/v1/admin-panel/service-requests/{serviceRequestId}/offers/{offerId}/reject
```

Implement only if internal endpoint exists.

---

# Command Scenario Group F - Completion Operations

## F1 - Admin approve completion

Candidate endpoint:

```http
POST /api/v1/admin-panel/service-requests/{serviceRequestId}/completion/{completionId}/approve
```

Request fields:

```text
adminNote
```

Internal orchestration:

- ServiceRequest API approve completion if available
- FileStorage API optional evidence URL generation for response
- return updated status/completion summary

## F2 - Admin reject completion

Candidate endpoint:

```http
POST /api/v1/admin-panel/service-requests/{serviceRequestId}/completion/{completionId}/reject
```

Request fields:

```text
reason
adminNote
requiredAction
```

## F3 - Admin request additional completion evidence

Candidate endpoint:

```http
POST /api/v1/admin-panel/service-requests/{serviceRequestId}/completion/{completionId}/request-evidence
```

Implement only if ServiceRequest message/admin note or completion update endpoint supports it. Otherwise document as blocked/future.

---

# Command Scenario Group G - Dispute Operations

## G1 - Admin change dispute status

Candidate endpoint:

```http
POST /api/v1/admin-panel/service-requests/{serviceRequestId}/disputes/{disputeId}/status
```

Request fields:

```text
status
reason
adminNote
```

Internal orchestration:

- ServiceRequest API change dispute status
- optional updated dispute detail fetch

## G2 - Admin resolve dispute

Candidate endpoint:

```http
POST /api/v1/admin-panel/service-requests/{serviceRequestId}/disputes/{disputeId}/resolve
```

Request fields:

```text
resolution
resolutionNote
resolvedInFavorOf
adminNote
```

Internal orchestration:

1. ServiceRequest API: get dispute detail.
2. ServiceRequest API: resolve dispute.
3. ServiceRequest API: get updated request/detail/timeline if available.
4. Return resolution response.

Payment/Payout side effects must be future placeholders because Payment is inactive.

## G3 - Admin add dispute message

Candidate endpoint:

```http
POST /api/v1/admin-panel/service-requests/{serviceRequestId}/disputes/{disputeId}/messages
```

Request fields:

```text
message
attachmentFileIds
visibility
```

Internal orchestration:

- FileStorage API: validate attachment fileIds if provided
- ServiceRequest API: add dispute message if supported

## G4 - Admin attach file to dispute

Candidate endpoint:

```http
POST /api/v1/admin-panel/service-requests/{serviceRequestId}/disputes/{disputeId}/attachments
```

Request fields:

```text
fileIds
note
```

---

# Command Scenario Group H - Batch/Admin Operations

## H1 - Admin bulk update service request status

Candidate endpoint:

```http
POST /api/v1/admin-panel/service-requests/bulk/status
```

Request fields:

```text
serviceRequestIds
status
reason
adminNote
```

Implement only if ServiceRequest module supports bulk or if calling individual status endpoints sequentially is acceptable by existing architecture.

If implemented sequentially:

- include per-item result
- do not hide partial failures
- do not use transaction semantics across modules

## H2 - Admin bulk regenerate attachment read URLs

Candidate endpoint:

```http
POST /api/v1/admin-panel/files/bulk/read-urls
```

Request fields:

```text
fileIds
```

Internal orchestration:

- FileStorage API generate read URL per file
- return per-file result

## H3 - Admin operational refresh command

Candidate endpoint:

```http
POST /api/v1/admin-panel/operations/refresh-cache
```

Implement only if active modules expose cache invalidation/refresh endpoints. Otherwise document as future.

---

# Required command response patterns

For each command endpoint, return a typed DTO such as:

```text
AdminBffCommandResultDto
AdminBffPartialCommandResultDto
AdminServiceRequestCommandResultDto
AdminVesselCommandResultDto
AdminDisputeCommandResultDto
```

Minimum fields:

```text
success
message
correlationId
warnings
updatedResource
```

For bulk operations:

```text
success
correlationId
totalCount
successCount
failedCount
items
warnings
```

---

# Idempotency and correlation

For command endpoints that create or mutate state, support these headers when existing repository conventions allow:

```text
X-Correlation-Id
Idempotency-Key
```

If idempotency infrastructure does not exist, document it as recommended future improvement. Do not create an unrelated idempotency framework unless the repository already has a convention.

---

# Required Postman outputs

Generate/update:

```text
Bff/src/AdminPanel/docs/postman/admin-panel-bff-cross-module-command-inventory.md
Bff/src/AdminPanel/docs/postman/AdminPanelBff.CrossModuleCommands.postman_collection.json
Bff/src/AdminPanel/docs/postman/admin-panel-bff-cross-module-command-validation-report.md
```

Each request must include:

```text
Authorization: Bearer {{active_access_token}}
X-Aizen-User-Token: {{X-Aizen-User-Token}}
Content-Type: application/json
```

Each request body must be generated from the actual BFF request DTO. Do not use `{}` for commands that require body payloads.

Each test script must validate:

- status is not 500
- JSON response when applicable
- `header.isSuccess` when wrapped
- command result section exists
- important ids/status values are captured into environment variables when useful

---

# Required documentation outputs

Generate:

```text
Bff/src/AdminPanel/docs/admin-panel-bff-cross-module-command-scenarios.md
Bff/src/AdminPanel/docs/admin-panel-bff-command-scenario-matrix.md
Bff/src/AdminPanel/docs/admin-panel-bff-cross-module-command-final-report.md
```

The matrix must include:

```text
Scenario code
BFF endpoint
Internal modules used
Internal endpoints used
Implemented / Blocked / Future
Reason if blocked
Postman request name
```

---

# Build validation

Run:

```bash
dotnet restore
dotnet build
```

Fix only AdminPanel BFF related compilation errors.

Do not refactor unrelated modules unless required for compilation and explicitly documented.
