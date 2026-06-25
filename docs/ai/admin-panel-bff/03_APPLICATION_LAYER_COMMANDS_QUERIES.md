# 03 - Application Layer Commands, Queries, and Services

## Goal

Create the Admin Panel BFF Application layer using service-based and sub-service-based CQRS organization.

## Rules

- Controllers must be thin.
- Application layer contains command/query handlers and remote orchestration services.
- Use `AizenRemoteCall` for internal API calls.
- Use active modules' Abstraction class libraries where possible.
- Create Admin BFF-specific DTOs only when aggregation, response shaping, or screen-specific contracts are needed.
- Do not place domain business logic in BFF.

## Required organization

Use this structure as a guide and adapt to existing repository conventions:

```text
Aizen.Bff.AdminPanel.Application/
  Dashboard/
    Query/
    Dto/
    Service/
  Identity/
    Users/
      Query/
      Command/
      Service/
    Profiles/
      Query/
      Command/
      Service/
  ReferenceData/
    Lookups/
      Query/
      Service/
    Currencies/
      Query/
      Service/
  Vessel/
    Vessels/
      Query/
      Command/
      Service/
    Documents/
      Query/
      Command/
      Service/
    Media/
      Query/
      Command/
      Service/
  FileStorage/
    Files/
      Query/
      Command/
      Service/
  ServiceRequest/
    Requests/
      Query/
      Command/
      Service/
    Offers/
      Query/
      Service/
    Assignments/
      Query/
      Command/
      Service/
    WorkLogs/
      Query/
      Service/
    Completions/
      Query/
      Command/
      Service/
    Disputes/
      Query/
      Command/
      Service/
```

## Commands and queries

Generate only commands/queries that are supported by discovered internal endpoints.

Candidate queries:

```text
GetAdminDashboardSummaryQuery
GetAdminServiceRequestListQuery
GetAdminServiceRequestDetailQuery
GetAdminPendingCompletionApprovalListQuery
GetAdminDisputeListQuery
GetAdminDelayedServiceRequestListQuery
GetAdminVesselListQuery
GetAdminVesselDetailQuery
GetAdminFileReviewListQuery
GetAdminLookupTreeQuery
GetAdminUserProfileListQuery
```

Candidate commands:

```text
ChangeAdminServiceRequestStatusCommand
ResolveAdminServiceRequestDisputeCommand
ChangeAdminDisputeStatusCommand
ApproveAdminCompletionCommand
RejectAdminCompletionCommand
ArchiveAdminVesselCommand
GenerateAdminFileReadUrlCommand
```

If an internal endpoint does not exist, do not fake it. Document it as missing/blocked.

## DTO strategy

1. For direct proxy-like responses, reuse internal module Abstraction DTOs.
2. For admin screen aggregation, create BFF DTOs, for example:

```text
AdminDashboardSummaryResponse
AdminServiceRequestListItemResponse
AdminServiceRequestDetailResponse
AdminDisputeListItemResponse
AdminPendingCompletionListItemResponse
AdminVesselListItemResponse
AdminFileReviewListItemResponse
```

3. Avoid returning raw `object`.
4. Preserve the common response wrapper style used by the repository, if applicable.

## Output

Update Application project and produce:

```text
Bff/src/AdminPanel/Aizen.Bff.AdminPanel/docs/admin-panel-bff-application-map.md
```
