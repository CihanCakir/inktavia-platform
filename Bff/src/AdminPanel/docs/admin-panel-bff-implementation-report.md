# Admin Panel BFF — Implementation Report

## Summary

The Admin Panel BFF is a fully implemented orchestration layer built on the Aizen Framework (`AppType.Bff`). It exposes a unified API surface at `api/v1/admin-panel/` for the admin dashboard UI.

## Projects

| Project | Purpose |
|---|---|
| `Aizen.Bff.AdminPanel` | Host: controllers, Program.cs, startup |
| `Aizen.Bff.AdminPanel.Application` | CQRS handlers, DTOs, remote call interfaces, DI |

## Controllers Implemented

### `AdminDashboardController`
- `GET /api/v1/admin-panel/dashboard/overview` → `GetAdminDashboardOverviewQuery`

### `AdminIdentityController`
- `GET /api/v1/admin-panel/identity/profiles` → `GetAdminProfilesQuery`
- `GET /api/v1/admin-panel/identity/profiles/{profileId}` → `GetAdminProfileDetailQuery`
- `POST /api/v1/admin-panel/identity/organizers/{userId}/profiles/{profileId}/approve` → `ApproveOrganizerProfileCommand`
- `POST /api/v1/admin-panel/identity/organizers/{userId}/profiles/{profileId}/reject` → `RejectOrganizerProfileCommand`
- `POST /api/v1/admin-panel/identity/venues/{userId}/profiles/{profileId}/approve` → `ApproveVenueProfileCommand`
- `POST /api/v1/admin-panel/identity/venues/{userId}/profiles/{profileId}/reject` → `RejectVenueProfileCommand`

### `AdminVesselsController`
- `GET /api/v1/admin-panel/vessels` → `GetAdminVesselOverviewQuery`
- `GET /api/v1/admin-panel/vessels/{vesselId}/detail` → `GetAdminVesselDocumentsQuery`
- `PATCH /api/v1/admin-panel/vessels/{vesselId}/archive` → `ArchiveVesselCommand`
- `PATCH /api/v1/admin-panel/vessels/{vesselId}/restore` → `RestoreVesselCommand`
- `PATCH /api/v1/admin-panel/vessels/{vesselId}/status` → `UpdateVesselStatusCommand`
- `DELETE /api/v1/admin-panel/vessels/{vesselId}/documents/{documentId}` → `RemoveVesselDocumentCommand`

### `AdminServiceRequestsController`
- `GET /api/v1/admin-panel/service-requests` → `GetAdminServiceRequestListQuery`
- `GET /api/v1/admin-panel/service-requests/disputes` → `GetAdminDisputeListQuery`
- `GET /api/v1/admin-panel/service-requests/{serviceRequestId}` → `GetAdminServiceRequestOperationDetailQuery`
- `PATCH /api/v1/admin-panel/service-requests/{serviceRequestId}/cancel` → `CancelServiceRequestCommand`
- `PATCH /api/v1/admin-panel/service-requests/{serviceRequestId}/completion/approve` → `ApproveCompletionCommand`
- `PATCH /api/v1/admin-panel/service-requests/{serviceRequestId}/completion/reject` → `RejectCompletionCommand`
- `PATCH /api/v1/admin-panel/service-requests/{serviceRequestId}/disputes/{disputeId}/status` → `ChangeDisputeStatusCommand`
- `PATCH /api/v1/admin-panel/service-requests/{serviceRequestId}/disputes/{disputeId}/resolve` → `ResolveDisputeCommand`

### `AdminFilesController`
- `GET /api/v1/admin-panel/files/{fileId}` → `GetAdminFileReviewOverviewQuery`
- `POST /api/v1/admin-panel/files/bulk-read-urls` → `BulkGenerateReadUrlsCommand`
- `DELETE /api/v1/admin-panel/files/{fileId}` → `DeleteFileCommand`

### `AdminReferenceDataController` (no CQRS — direct remote call)
- `GET /api/v1/admin-panel/reference-data/lookup-groups`
- `GET /api/v1/admin-panel/reference-data/lookup-tree`
- `GET /api/v1/admin-panel/reference-data/lookup/{groupCode}/items`
- `GET /api/v1/admin-panel/reference-data/currencies`
- `GET /api/v1/admin-panel/reference-data/locations/countries`
- `GET /api/v1/admin-panel/reference-data/locations/cities`
- `GET /api/v1/admin-panel/reference-data/measurement-units`
- `GET /api/v1/admin-panel/reference-data/system-parameters`

## Remote Call Interfaces Implemented

| Interface | Module | Port |
|---|---|---|
| `IIdentityAdminBffRemoteCall` | Identity | 7101 |
| `IReferenceDataAdminBffRemoteCall` | ReferenceData | 7104 |
| `IVesselAdminBffRemoteCall` | Vessel | 7105 |
| `IFileStorageAdminBffRemoteCall` | FileStorage | 7106 |
| `IServiceRequestAdminBffRemoteCall` | ServiceRequest | 7107 |

## DTOs Implemented

### Application Layer DTOs
- `AdminDashboardOverviewResponse` — aggregated dashboard metrics
- `AdminUserOverviewResponse` — paged user profiles
- `AdminVesselOverviewResponse` — paged vessel list with form options
- `AdminVesselDocumentsResponse` — vessel detail with documents
- `AdminVesselFormOptionsResponse` — form lookup options
- `AdminServiceRequestListResponse` — paged request list
- `AdminServiceRequestOperationDetailResponse` — enriched request detail
- `AdminServiceRequestTimelineResponse` — timeline events
- `AdminServiceRequestFilterOptionsResponse` — filter option lookups
- `AdminFileReviewOverviewResponse` — file metadata with access info
- `AdminBffCommandResultDto` — generic command result
- `AdminBffBulkCommandResultDto` — bulk operation result
- `AdminBffRejectRequest` — rejection reason wrapper

### In-Scope vs Skipped

| Area | Status | Notes |
|---|---|---|
| Dashboard overview | ✅ Implemented | Aggregates Identity + Vessel + ServiceRequest counts |
| Identity profile search + detail | ✅ Implemented | With roleContext and approvalStatus filters |
| Organizer approval/rejection | ✅ Implemented | |
| Venue approval/rejection | ✅ Implemented | |
| Vessel list + detail | ✅ Implemented | |
| Vessel archive/restore/status | ✅ Implemented | |
| Vessel document removal | ✅ Implemented | |
| Service request list + detail | ✅ Implemented | |
| Dispute list + status + resolve | ✅ Implemented | |
| Cancel / approve-completion / reject-completion | ✅ Implemented | |
| File metadata + read URLs | ✅ Implemented | Bulk URL generation fans out per file |
| File deletion | ✅ Implemented | |
| Reference data (all lookups) | ✅ Implemented | No auth required |
| Upload session management | ⬛ Out of scope | Owned by FileStorage module directly |
| Vessel CRUD (create/update) | ⬛ Out of scope | Managed by Vessel module owner |
| Payment management | ⬛ Out of scope | Separate payment module |
| Profile CRUD | ⬛ Out of scope | Owned by Identity module |
