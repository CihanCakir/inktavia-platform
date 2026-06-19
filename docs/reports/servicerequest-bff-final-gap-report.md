# ServiceRequest BFF Final Gap Report

## Implemented ✅

| Feature | Status |
|---------|--------|
| ServiceRequest admin list supports full filter (VesselId, Status, Priority, OwnerUserId, SearchTerm, etc.) | ✅ |
| ServiceRequestSummaryDto exposes location, notes, serviceTypeCode, ownerUserId, providerProfileId | ✅ |
| AdminPanel BFF list response uses BFF-owned concrete DTOs (no IPaginate/interface) | ✅ |
| AdminPanel BFF list response has pagination metadata (From/Index/Size/Count/Pages/HasPrevious/HasNext) | ✅ |
| Vessel detail serviceHistory maps Location and Notes correctly | ✅ |
| Vessel detail serviceHistory uses ServiceTypeCode ?? ServiceCategoryCode | ✅ |
| Vessel detail serviceHistory status normalized to lower-case | ✅ |
| New GET /admin-panel/service-requests/by-vessel/{vesselId}/history endpoint | ✅ |
| Auth: AdminPanelAccess policy on all service request BFF endpoints | ✅ |
| Auth: BFF forwards Keycloak service token + Identity user token to SR module | ✅ |
| Null-safety: SR module unavailable → warnings array, never hard BFF failure | ✅ |
| Build passes with 0 errors | ✅ |

## Known Gaps (Documented, Not Blocking)

### P1 Gaps

| Gap | Root Cause | Resolution Path |
|-----|-----------|----------------|
| `provider` field in serviceHistory is always null | ProviderName requires Identity/Profile module cross-join | When Profile module is active, add `IProfileAdminBffRemoteCall.GetProviderDisplayName(providerProfileId)` call |
| `vesselName` not in ServiceRequest list items | Requires Vessel cross-call per list item | Add Vessel module BatchGetNames call in BFF handler or denormalize in SR module |
| `ownerName` not in ServiceRequest list items | Requires Identity user profile lookup | Add Identity enrichment step in BFF handler |
| P1 sub-detail read endpoints not BFF-exposed | SR module has the data; BFF wiring not added | Add GetAdminServiceRequestOffersQuery, WorkLogsQuery, MessagesQuery handlers |

### P2 Gaps

| Gap | Root Cause | Resolution Path |
|-----|-----------|----------------|
| `POST /admin-panel/service-requests/{id}/assign` not exposed | `CreateServiceRequestAssignmentCommand` exists; BFF wiring not added | Straightforward to add in a follow-up sprint |
| Admin status change endpoint | No dedicated admin change-status command exists | Create `AdminChangeServiceRequestStatusCommand` in SR module first |

### Infrastructure Gaps (Pre-existing)

| Gap | Notes |
|-----|-------|
| Token protection for Redis | Keycloak service tokens stored as raw values; encryption utility follow-up documented in previous reports |
| EF Migration for new fields | ServiceRequest module has no schema changes in this sprint (only DTO/query changes) |

## Build Summary

| Project | Result |
|---------|--------|
| `Aizen.Modules.ServiceRequest` | ✅ 0 errors |
| `Aizen.Bff.AdminPanel` | ✅ 0 errors |
| `Aizen.Bff.AdminPanel.Application` | ✅ 0 errors (dependency of above) |

## Files Changed Summary

| File | Change Type |
|------|------------|
| `ServiceRequestSummaryDto.cs` | Modified — 8 new fields |
| `IServiceRequestRepository.cs` | Modified — 2 new methods |
| `ServiceRequestRepository.cs` | Modified — impl of 2 new methods + `BuildAdminQuery` |
| `ServiceRequestMappingExtensions.cs` | Modified — `ToSummaryDto` maps new fields |
| `GetAdminServiceRequestListQueryHandler.cs` (SR module) | Modified — uses `GetAdminListAsync` |
| `ServiceHistoryItemBffDto.cs` | Modified — `Provider` and `Location` fields added |
| `GetAdminVesselDetailBffQueryHandler.cs` | Modified — improved mapping |
| `AdminServiceRequestListResponse.cs` | Modified — uses BFF-owned `ServiceRequestPageBffDto` |
| `GetAdminServiceRequestListQueryHandler.cs` (BFF) | Modified — maps to BFF DTOs |
| `AdminServiceRequestsController.cs` | Modified — added by-vessel history endpoint |

## Files Created

| File | Purpose |
|------|---------|
| `ServiceRequestListItemBffDto.cs` | BFF-owned list item DTO |
| `ServiceRequestPageBffDto.cs` | BFF-owned pagination wrapper |
| `ServiceRequestVesselHistoryBffResponse.cs` | By-vessel history response |
| `GetServiceRequestsByVesselHistoryQuery.cs` | BFF query for vessel history |
| `GetServiceRequestsByVesselHistoryQueryHandler.cs` | BFF handler for vessel history |
