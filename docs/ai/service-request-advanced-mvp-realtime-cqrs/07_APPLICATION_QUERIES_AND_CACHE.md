# Inktavia Marine OS - ServiceRequest Advanced MVP Realtime CQRS

Use this prompt with GitHub Copilot Agent inside the repository.

All generated implementation must be in English and must follow the existing Aizen/Inktavia architecture.

## Step 07 - Application queries, projections, pagination, and cache

Implement CQRS queries and handlers for owner, provider, admin, and shared flows.

## General rules

- All collection queries must support pagination.
- Use existing pagination request/response contracts.
- Use typed DTOs only.
- Do not return domain entities.
- Use `IAizenQueryHandlerCacheable` or equivalent where appropriate.
- Enforce authorization and visibility rules in query handlers or repository services.
- Admin can see all records according to admin authorization policy.
- Non-admin users must be scoped by owner/provider role and access validation.

## Owner queries

Implement:

```text
GetCurrentUserServiceRequestListQuery
GetCurrentUserServiceRequestDetailQuery
GetCurrentUserVesselServiceRequestsQuery
GetServiceRequestOfferListForOwnerQuery
GetServiceRequestTimelineQuery
```

## Provider queries

Implement:

```text
GetProviderAvailableServiceRequestListQuery
GetProviderAssignedServiceRequestListQuery
GetProviderServiceRequestDetailQuery
GetProviderOfferListQuery
GetProviderWorkLogListQuery
```

## Admin queries

Implement:

```text
GetAdminServiceRequestListQuery
GetAdminServiceRequestDetailQuery
GetAdminServiceRequestDashboardQuery
GetAdminDisputeListQuery
GetAdminDelayedServiceRequestListQuery
GetAdminPendingCompletionApprovalListQuery
```

## Shared queries

Implement:

```text
GetServiceRequestMessageListQuery
GetServiceRequestAttachmentListQuery
GetServiceRequestStatusHistoryQuery
GetServiceRequestCompletionDetailQuery
GetServiceRequestDisputeDetailQuery
```

## Filters

Support relevant filters:

```text
ServiceRequestId
VesselId
OwnerUserId
OwnerProfileId
ProviderProfileId
AssignedTeamMemberId
ServiceCategoryId
ServiceTypeId
Status
Priority
Date range
Created date range
Requested date range
Completion status
Dispute status
Text search
HasUnreadMessages
HasDispute
IsDelayed
```

Only implement filters that make sense for the specific query.

## Cache

Cache stable/read-heavy queries. Suggested cache keys:

```text
servicerequest:owner:{userId}:list:{hash}
servicerequest:detail:{serviceRequestId}:user:{userId}
servicerequest:vessel:{vesselId}:list:{hash}
servicerequest:provider:{providerProfileId}:assigned:{hash}
servicerequest:admin:dashboard:{hash}
servicerequest:timeline:{serviceRequestId}
```

Invalidate cache after relevant write commands.

## Output required

Create/update:

```text
ai/service-request-advanced-mvp-realtime-cqrs/reports/07_APPLICATION_QUERIES_REPORT.md
```
