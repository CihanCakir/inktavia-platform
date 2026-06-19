# ServiceRequest BFF Contract Audit Report

## Scope

Audit of the existing ServiceRequest module contracts, BFF remote call interface, and BFF response DTOs to identify gaps and alignment with the target admin UI contract.

## Existing Module Contracts

### Admin Endpoints (ServiceRequest module)
| Endpoint | Route | Status |
|----------|-------|--------|
| GET list | `GET /api/v1/admin/service-requests` | ✅ Exists |
| GET disputes | `GET /api/v1/admin/service-requests/disputes` | ✅ Exists |
| GET detail | `GET /api/v1/admin/service-requests/{id}` | ✅ Exists |

### ServiceRequestSummaryDto — Field Audit (BEFORE changes)
| Field | Present | Notes |
|-------|---------|-------|
| Id | ✅ | |
| RequestCode | ✅ | |
| Title | ✅ | |
| ServiceCategoryCode | ✅ | |
| ServiceTypeCode | ❌ | **Gap — added** |
| Status | ✅ | |
| Priority | ✅ | |
| VesselId | ✅ | |
| OwnerUserId | ❌ | **Gap — added** |
| ProviderProfileId | ❌ | **Gap — added** |
| LocationMarinaName | ❌ | **Gap — added** |
| LocationCityCode | ❌ | **Gap — added** |
| LocationCountryCode | ❌ | **Gap — added** |
| OwnerNotes | ❌ | **Gap — added** |
| RequestedStartDate | ✅ | |
| LastActivityAt | ❌ | **Gap — added** (mapped from ModifyDate) |
| OfferCount | ✅ | |
| HasActiveAssignment | ✅ | |
| CreatedAt | ✅ | |
| UpdatedAt | ✅ | |

### GetAdminServiceRequestListQueryHandler — Filter Support Audit (BEFORE)
| Filter | Supported | Notes |
|--------|-----------|-------|
| OwnerUserId | ✅ (only this) | Handler returned empty for all other cases |
| VesselId | ❌ | **Gap — fixed** |
| Status | ❌ | **Gap — fixed** |
| Priority | ❌ | **Gap — fixed** |
| ProviderProfileId | ❌ | **Gap — fixed** |
| SearchTerm | ❌ | **Gap — fixed** |
| HasDispute | ❌ | **Gap — fixed** |
| ServiceCategoryCode | ❌ | **Gap — fixed** |
| Date range | ❌ | **Gap — fixed** |

## BFF Remote Call Audit

### IServiceRequestAdminBffRemoteCall — Parameters Passed to Module
- `vesselId` ✅ — already forwarded in query param
- `status` ✅ — already forwarded
- `pageIndex` / `pageSize` ✅ — already forwarded

### AdminServiceRequestListResponse — Shape (BEFORE)
- Wrapped `GetAdminServiceRequestListResponse` from module abstraction directly → **no pagination metadata**
- **Fixed**: now uses BFF-owned `ServiceRequestPageBffDto` with From/Index/Size/Count/Pages/HasPrevious/HasNext

## P0 Gaps Resolved

| Gap | Resolution |
|-----|-----------|
| ServiceRequest admin list only supported OwnerUserId filter | Added `GetAdminListAsync` / `CountAdminAsync` with full filter support to repo |
| ServiceRequestSummaryDto missing location, notes, serviceType, provider fields | Extended DTO with 8 new fields |
| BFF list response wrapped module DTO directly (no pagination metadata) | Created BFF-owned `ServiceRequestListItemBffDto` and `ServiceRequestPageBffDto` |
| Vessel detail service history missing provider/location/notes | Updated `ServiceHistoryItemBffDto` and `GetAdminVesselDetailBffQueryHandler` mapping |
| No dedicated by-vessel history endpoint in BFF | Added `GET /api/v1/admin-panel/service-requests/by-vessel/{vesselId}/history` |

## Remaining Gaps (documented, not blocking)

| Gap | Reason | Status |
|-----|--------|--------|
| ProviderName (display name) | Requires Identity/Profile integration — module not active | P1 gap |
| VesselName in list | Requires Vessel cross-join from SR module | P1 gap |
| OwnerName in list | Requires Identity user profile lookup | P1 gap |
