# AdminPanel BFF ServiceRequest Remote Call Report

## Scope

Audit and update of the `IServiceRequestAdminBffRemoteCall` contract and BFF-owned DTOs for ServiceRequest integration.

## RemoteCall Interface

**Path:** `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/Common/RemoteClients/IServiceRequestAdminBffRemoteCall.cs`

### Methods and Token Forwarding

| Method | Route | Auth Header | User Token |
|--------|-------|-------------|------------|
| `GetAdminServiceRequestList` | `GET /api/v1/admin/service-requests` | ✅ `Authorization` | ✅ `X-Aizen-User-Token` |
| `GetAdminDisputeList` | `GET /api/v1/admin/service-requests/disputes` | ✅ | ✅ |
| `GetAdminServiceRequestDetail` | `GET /api/v1/admin/service-requests/{id}` | ✅ | ✅ |
| `CancelServiceRequest` | `PATCH /api/v1/service-requests/{id}/cancel` | ✅ | ✅ |
| `ApproveCompletion` | `PATCH /api/v1/service-requests/{id}/completion/approve` | ✅ | ✅ |
| `RejectCompletion` | `PATCH /api/v1/service-requests/{id}/completion/reject` | ✅ | ✅ |
| `ChangeDisputeStatus` | `PATCH /api/v1/service-requests/{id}/dispute/{disputeId}/status` | ✅ | ✅ |
| `ResolveDispute` | `PATCH /api/v1/service-requests/{id}/dispute/{disputeId}/resolve` | ✅ | ✅ |

### RemoteCall Contract Notes

- All read endpoints accept `Authorization` and `X-Aizen-User-Token` headers
- All command endpoints accept `Authorization` and `X-Aizen-User-Token` headers
- Query parameters for `vesselId`, `status`, `pageIndex`, `pageSize` are bound with `[Refit.Query]`
- No `IPaginate<T>` in remote call response types — responses use concrete types from SR abstraction

### No Interface Changes Required

The existing `IServiceRequestAdminBffRemoteCall` already supports all P0 query parameters needed. No changes to the remote call interface were required.

## BFF-Owned DTOs Created

| File | Purpose |
|------|---------|
| `ServiceRequestListItemBffDto.cs` | Flat list item DTO — maps from `ServiceRequestSummaryDto` |
| `ServiceRequestPageBffDto.cs` | Pagination wrapper with From/Index/Size/Count/Pages/HasPrevious/HasNext |
| `ServiceRequestVesselHistoryBffResponse.cs` | Response for by-vessel history endpoint |

## Pagination Mapping Logic

`GetAdminServiceRequestListQueryHandler` builds `ServiceRequestPageBffDto` from module response:

```csharp
var pages = request.PageSize > 0 ? (int)Math.Ceiling((double)total / request.PageSize) : 0;
ServiceRequestPageBffDto {
    From = request.PageIndex * request.PageSize,
    Index = request.PageIndex,
    Size = request.PageSize,
    Count = total,
    Pages = pages,
    HasPrevious = request.PageIndex > 0,
    HasNext = request.PageIndex < pages - 1,
    Items = items
}
```

## Field Mapping (Summary → BFF List Item)

| BFF Field | Source |
|-----------|--------|
| `Id` | `ServiceRequestSummaryDto.Id` |
| `RequestCode` | `ServiceRequestSummaryDto.RequestCode` |
| `VesselId` | `ServiceRequestSummaryDto.VesselId` |
| `ServiceType` | `ServiceTypeCode ?? ServiceCategoryCode` |
| `ServiceCategoryCode` | `ServiceRequestSummaryDto.ServiceCategoryCode` |
| `Title` | `ServiceRequestSummaryDto.Title` |
| `Status` | `Status.ToString().ToLowerInvariant()` |
| `Priority` | `Priority.ToString().ToLowerInvariant()` |
| `Location` | `LocationMarinaName` |
| `Notes` | `OwnerNotes ?? Title` |
| `OwnerUserId` | `ServiceRequestSummaryDto.OwnerUserId` |
| `ProviderProfileId` | `ServiceRequestSummaryDto.ProviderProfileId` |
| `OfferCount` | `ServiceRequestSummaryDto.OfferCount` |
| `HasActiveAssignment` | `ServiceRequestSummaryDto.HasActiveAssignment` |
| `RequestedDate` | `ServiceRequestSummaryDto.RequestedStartDate` |
| `LastActivityAt` | `ServiceRequestSummaryDto.LastActivityAt` |
| `CreatedAt` | `ServiceRequestSummaryDto.CreatedAt` |

## Remaining Gaps

| Gap | Reason |
|-----|--------|
| `VesselName` in list items | Would require Vessel module cross-call per list item (expensive) |
| `OwnerName` / `ProviderName` | Requires Identity/Profile module integration |
