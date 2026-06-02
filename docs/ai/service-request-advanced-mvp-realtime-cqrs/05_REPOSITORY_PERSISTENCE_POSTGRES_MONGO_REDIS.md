# Inktavia Marine OS - ServiceRequest Advanced MVP Realtime CQRS

Use this prompt with GitHub Copilot Agent inside the repository.

All generated implementation must be in English and must follow the existing Aizen/Inktavia architecture.

## Step 05 - Repository, PostgreSQL persistence, optional Mongo, Redis/cache

Implement persistence according to the existing repository layer conventions.

## PostgreSQL

Create ServiceRequest DbContext and EF Core configurations using the project's current pattern.

Suggested schema name:

```text
servicerequest
```

If existing modules use another naming convention, follow it.

## DbContext

Create something logically equivalent to:

```text
ServiceRequestDbContext
```

DbSets:

```text
ServiceRequests
ServiceRequestItems
ServiceRequestOffers
ServiceRequestOfferItems
ServiceRequestStatusHistories
ServiceRequestAttachments
ServiceRequestMessages
ServiceRequestAssignments
ServiceRequestWorkLogs
ServiceRequestCompletions
ServiceRequestDisputes
```

## EF Core configuration requirements

For each entity:

- table name
- primary key
- required fields
- max lengths
- decimal precision for monetary fields
- enum conversions according to existing standard
- indexes for query performance
- relationships
- cascade/restrict behavior consistent with current modules
- soft delete query filter if existing base supports it
- UTC-compatible date/time handling

## Suggested indexes

Add indexes where appropriate:

```text
ServiceRequest: VesselId, OwnerUserId, OwnerProfileId, Status, Priority, ServiceCategoryId, ServiceTypeId, CreatedAt
ServiceRequestOffer: ServiceRequestId, ProviderProfileId, Status, CreatedAt
ServiceRequestAssignment: ServiceRequestId, ProviderProfileId, AssignedTeamMemberId, Status, PlannedStartAt
ServiceRequestWorkLog: ServiceRequestId, AssignmentId, Type, CreatedAt
ServiceRequestMessage: ServiceRequestId, SenderUserId, CreatedAt, IsRead
ServiceRequestDispute: ServiceRequestId, Status, Reason, CreatedAt
ServiceRequestAttachment: ServiceRequestId, FileId, AttachmentType
```

## Repository contracts/services

Create repository interfaces and implementations using the project pattern.

Minimum repository/service methods:

```text
AddServiceRequestAsync
UpdateServiceRequestAsync
GetServiceRequestByIdAsync
GetServiceRequestDetailAsync
GetServiceRequestsByOwnerAsync
GetServiceRequestsByVesselAsync
GetAdminServiceRequestListAsync
GetProviderAvailableServiceRequestsAsync
GetProviderAssignedServiceRequestsAsync
AddOfferAsync
GetOfferByIdAsync
GetOffersByServiceRequestAsync
AcceptOfferAsync or domain-level state update support
AddAssignmentAsync
GetAssignmentByIdAsync
GetAssignmentsByProviderAsync
AddMessageAsync
GetMessagesAsync
AddWorkLogAsync
GetWorkLogsAsync
SubmitCompletionAsync
GetCompletionByIdAsync
OpenDisputeAsync
GetDisputesAsync
AddStatusHistoryAsync
```

Use repository service methods for complex query projections if existing architecture prefers repository services over raw DbContext in handlers.

## Mongo optional read/activity documents

Only implement Mongo if existing architecture uses it for read models/activity documents and it fits the module.

Possible documents:

```text
ServiceRequestActivityDocument
ServiceRequestTimelineDocument
ServiceRequestRealtimeEventDocument
```

Do not duplicate primary transactional state in Mongo.

## Redis/query cache

Use existing cache patterns for frequently used queries:

```text
GetCurrentUserServiceRequestListQuery
GetCurrentUserServiceRequestDetailQuery
GetCurrentUserVesselServiceRequestsQuery
GetProviderAssignedServiceRequestListQuery
GetAdminServiceRequestDashboardQuery
GetServiceRequestTimelineQuery
```

Implement cache key constants and invalidation after write commands.

## DI registration

Register DbContext, repositories, services, Mongo optional repositories, cache services, and realtime publishers according to existing module conventions.

## Migration

Create migration if the repository has migration tooling. If migration generation cannot be completed automatically, document exact commands to run.

## Output required

Create/update:

```text
ai/service-request-advanced-mvp-realtime-cqrs/reports/05_REPOSITORY_PERSISTENCE_REPORT.md
```
