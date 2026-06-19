# AdminPanel BFF ServiceRequest Endpoint Implementation Report

## Scope

Files created or modified in the AdminPanel BFF for ServiceRequest integration.

## Files Modified

### `AdminServiceRequestListResponse.cs`
**Path:** `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminServiceRequests/Dto/AdminServiceRequestListResponse.cs`

**Change:** `ServiceRequests` property type changed from `GetAdminServiceRequestListResponse?` (module type) to `ServiceRequestPageBffDto?` (BFF-owned)

### `GetAdminServiceRequestListQueryHandler.cs`
**Path:** `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminServiceRequests/Query/GetAdminServiceRequestListQueryHandler.cs`

**Changes:**
- Maps module `GetAdminServiceRequestListResponse` items to `ServiceRequestListItemBffDto` list
- Builds `ServiceRequestPageBffDto` with full pagination metadata
- Maintains null-safe try/catch — returns `warnings` array on SR module failure

### `ServiceHistoryItemBffDto.cs`
**Path:** `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminVessels/Dto/ServiceHistoryItemBffDto.cs`

**Fields added:**
- `Provider` (string?) — provider display name; currently `null` (ProviderName requires Identity/Profile integration)
- `Location` (string?) — mapped from `LocationMarinaName`

### `GetAdminVesselDetailBffQueryHandler.cs`
**Path:** `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminVessels/Query/GetAdminVesselDetailBffQueryHandler.cs`

**Changes to service history mapping:**
- `Date`: now uses `RequestedStartDate ?? CreatedAt` (fallback to CreatedAt)
- `ServiceType`: now uses `ServiceTypeCode ?? ServiceCategoryCode` (prefers specific type code)
- `Provider`: `null` (gap — documented)
- `Location`: mapped from `LocationMarinaName`
- `Notes`: mapped from `OwnerNotes ?? Title` (prefers owner notes over title)
- `Status`: normalized to lower-case string

## Files Created

### `ServiceRequestListItemBffDto.cs`
**Path:** `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminServiceRequests/Dto/ServiceRequestListItemBffDto.cs`

Flat list item DTO for admin service request list screen.

### `ServiceRequestPageBffDto.cs`
**Path:** `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminServiceRequests/Dto/ServiceRequestPageBffDto.cs`

Concrete pagination wrapper with From/Index/Size/Count/Pages/HasPrevious/HasNext.

### `ServiceRequestVesselHistoryBffResponse.cs`
**Path:** `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminServiceRequests/Dto/ServiceRequestVesselHistoryBffResponse.cs`

Response DTO for by-vessel history endpoint.

### `GetServiceRequestsByVesselHistoryQuery.cs`
**Path:** `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminServiceRequests/Query/GetServiceRequestsByVesselHistoryQuery.cs`

New BFF query for fetching service history by vessel ID.

### `GetServiceRequestsByVesselHistoryQueryHandler.cs`
**Path:** `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminServiceRequests/Query/GetServiceRequestsByVesselHistoryQueryHandler.cs`

Handler — calls `GetAdminServiceRequestList` with `vesselId` filter. Null-safe: returns empty array + warning on failure.

## Controller Endpoint Summary

**Controller:** `ServiceRequestsController` at `api/v1/admin-panel`
**Auth policy:** `[Authorize(Policy = "AdminPanelAccess")]`

| Method | Route | Response Type | New |
|--------|-------|---------------|-----|
| GET | `service-requests` | `AdminServiceRequestListResponse` | Updated shape |
| GET | `service-requests/by-vessel/{vesselId}/history` | `ServiceRequestVesselHistoryBffResponse` | ✅ NEW |
| GET | `service-requests/disputes` | `GetAdminDisputeListResponse` | Unchanged |
| GET | `service-requests/{id}` | `AdminServiceRequestOperationDetailResponse` | Unchanged |
| GET | `service-requests/filter-options` | `AdminServiceRequestFilterOptionsResponse` | Unchanged |
| GET | `service-requests/{id}/timeline` | `AdminServiceRequestTimelineResponse` | Unchanged |
| PATCH | `service-requests/{id}/cancel` | `CancelServiceRequestResponse` | Unchanged |
| PATCH | `service-requests/{id}/completion/approve` | `ApproveServiceRequestCompletionResponse` | Unchanged |
| PATCH | `service-requests/{id}/completion/reject` | `RejectServiceRequestCompletionResponse` | Unchanged |
| PATCH | `service-requests/{id}/disputes/{disputeId}/status` | `AdminBffCommandResultDto` | Unchanged |
| PATCH | `service-requests/{id}/disputes/{disputeId}/resolve` | `ResolveServiceRequestDisputeResponse` | Unchanged |

## Build Validation

```
dotnet build Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Aizen.Bff.AdminPanel.csproj --no-incremental
```
**Result:** ✅ Build succeeded — 0 errors
