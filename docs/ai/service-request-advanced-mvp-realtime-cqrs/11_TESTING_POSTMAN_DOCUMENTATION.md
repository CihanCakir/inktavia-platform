# Inktavia Marine OS - ServiceRequest Advanced MVP Realtime CQRS

Use this prompt with GitHub Copilot Agent inside the repository.

All generated implementation must be in English and must follow the existing Aizen/Inktavia architecture.

## Step 11 - Testing, Postman, Swagger documentation, DocumentationInfo

Add validation, tests, documentation metadata, and API examples according to existing standards.

## DocumentationInfo

Ensure every public type has `DocumentationInfo` if this project uses it:

```text
public classes
public interfaces
commands
queries
handlers
validators
request models
DTOs
controllers
services
repositories
realtime publishers
```

The description must clearly explain the purpose and business responsibility of the type.

## Tests

If the repository has a test pattern, add first-phase tests for:

### Command tests

```text
CreateServiceRequestCommand
CreateServiceRequestOfferCommand
AcceptServiceRequestOfferCommand
CreateServiceRequestAssignmentCommand
StartServiceRequestAssignmentCommand
AddServiceRequestWorkLogCommand
SubmitServiceRequestCompletionCommand
ApproveServiceRequestCompletionCommand
OpenServiceRequestDisputeCommand
ResolveServiceRequestDisputeCommand
SendServiceRequestMessageCommand
```

### Query tests

```text
GetCurrentUserServiceRequestListQuery
GetCurrentUserServiceRequestDetailQuery
GetProviderAssignedServiceRequestListQuery
GetAdminServiceRequestDashboardQuery
GetServiceRequestTimelineQuery
```

### Realtime tests if supported

Validate that command handlers call the realtime publisher after successful state changes.

Do not use mocks if the repository standard avoids mocks. Follow existing test style.

## Postman collection

If the repository contains Postman generation scripts or collections, add/update ServiceRequest endpoints.

Include environment variables for:

```text
baseUrl
accessToken
serviceRequestId
vesselId
providerProfileId
offerId
assignmentId
completionId
disputeId
fileId
```

## Swagger/OpenAPI

Add examples and response models according to existing Swagger conventions.

## Output required

Create/update:

```text
ai/service-request-advanced-mvp-realtime-cqrs/reports/11_TESTING_DOCUMENTATION_REPORT.md
```
