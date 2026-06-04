# Admin Panel BFF — Application Map

Complete mapping from BFF endpoint → CQRS Query/Command → Remote Call method → Module endpoint.

## Dashboard

| BFF Endpoint | Method | CQRS | Remote Call Interface | Module Endpoint | Auth |
|---|---|---|---|---|---|
| `GET /api/v1/admin-panel/dashboard/overview` | Query | `GetAdminDashboardOverviewQuery` | `IIdentityAdminBffRemoteCall` + `IVesselAdminBffRemoteCall` + `IServiceRequestAdminBffRemoteCall` | Aggregated | ✅ Admin |

## Identity

| BFF Endpoint | Method | CQRS | Remote Call Interface | Module Endpoint | Auth |
|---|---|---|---|---|---|
| `GET /api/v1/admin-panel/identity/profiles` | Query | `GetAdminProfilesQuery(auth, roleContext, approvalStatus, pageIndex, pageSize)` | `IIdentityAdminBffRemoteCall.SearchProfiles` | `GET /api/v1/identity/profiles` | ✅ Admin |
| `GET /api/v1/admin-panel/identity/profiles/{profileId}` | Query | `GetAdminProfileDetailQuery(profileId, auth, roleContext)` | `IIdentityAdminBffRemoteCall.GetProfileById` | `GET /api/v1/identity/profiles/{profileId}` | ✅ Admin |
| `POST /api/v1/admin-panel/identity/organizers/{userId}/profiles/{profileId}/approve` | Command | `ApproveOrganizerProfileCommand(userId, profileId, auth)` | `IIdentityAdminBffRemoteCall.ApproveOrganizerProfile` | `POST /api/v1/identity/admin/organizers/{userId}/profiles/{profileId}/approve` | ✅ Admin |
| `POST /api/v1/admin-panel/identity/organizers/{userId}/profiles/{profileId}/reject` | Command | `RejectOrganizerProfileCommand(userId, profileId, reason, auth)` | `IIdentityAdminBffRemoteCall.RejectOrganizerProfile` | `POST /api/v1/identity/admin/organizers/{userId}/profiles/{profileId}/reject` | ✅ Admin |
| `POST /api/v1/admin-panel/identity/venues/{userId}/profiles/{profileId}/approve` | Command | `ApproveVenueProfileCommand(userId, profileId, auth)` | `IIdentityAdminBffRemoteCall.ApproveVenueProfile` | `POST /api/v1/identity/admin/venues/{userId}/profiles/{profileId}/approve` | ✅ Admin |
| `POST /api/v1/admin-panel/identity/venues/{userId}/profiles/{profileId}/reject` | Command | `RejectVenueProfileCommand(userId, profileId, reason, auth)` | `IIdentityAdminBffRemoteCall.RejectVenueProfile` | `POST /api/v1/identity/admin/venues/{userId}/profiles/{profileId}/reject` | ✅ Admin |

## Vessels

| BFF Endpoint | Method | CQRS | Remote Call Interface | Module Endpoint | Auth |
|---|---|---|---|---|---|
| `GET /api/v1/admin-panel/vessels` | Query | `GetAdminVesselOverviewQuery(auth, pageIndex, pageSize, searchTerm, isArchived)` | `IVesselAdminBffRemoteCall.GetAdminVesselList` | `GET /api/v1/admin/vessels` | ✅ Admin |
| `GET /api/v1/admin-panel/vessels/{vesselId}/detail` | Query | `GetAdminVesselDocumentsQuery(vesselId, auth)` | `IVesselAdminBffRemoteCall.GetVesselDocuments` | `GET /api/v1/vessels/{vesselId}/documents` | ✅ Admin |
| `PATCH /api/v1/admin-panel/vessels/{vesselId}/archive` | Command | `ArchiveVesselCommand(vesselId, request, auth)` | `IVesselAdminBffRemoteCall.ArchiveVessel` | `PATCH /api/v1/vessels/{vesselId}/archive` | ✅ Admin |
| `PATCH /api/v1/admin-panel/vessels/{vesselId}/restore` | Command | `RestoreVesselCommand(vesselId, auth)` | `IVesselAdminBffRemoteCall.RestoreVessel` | `PATCH /api/v1/vessels/{vesselId}/restore` | ✅ Admin |
| `PATCH /api/v1/admin-panel/vessels/{vesselId}/status` | Command | `UpdateVesselStatusCommand(vesselId, request, auth)` | `IVesselAdminBffRemoteCall.UpdateVesselStatus` | `PATCH /api/v1/vessels/{vesselId}/status` | ✅ Admin |
| `DELETE /api/v1/admin-panel/vessels/{vesselId}/documents/{documentId}` | Command | `RemoveVesselDocumentCommand(vesselId, documentId, auth)` | `IVesselAdminBffRemoteCall.RemoveVesselDocument` | `DELETE /api/v1/vessels/{vesselId}/documents/{documentId}` | ✅ Admin |

## Service Requests

| BFF Endpoint | Method | CQRS | Remote Call Interface | Module Endpoint | Auth |
|---|---|---|---|---|---|
| `GET /api/v1/admin-panel/service-requests` | Query | `GetAdminServiceRequestListQuery(auth, status, vesselId, pageIndex, pageSize)` | `IServiceRequestAdminBffRemoteCall.GetAdminServiceRequestList` | `GET /api/v1/admin/service-requests` | ✅ Admin |
| `GET /api/v1/admin-panel/service-requests/disputes` | Query | `GetAdminDisputeListQuery(auth, status, pageIndex, pageSize)` | `IServiceRequestAdminBffRemoteCall.GetAdminDisputeList` | `GET /api/v1/admin/service-requests/disputes` | ✅ Admin |
| `GET /api/v1/admin-panel/service-requests/{serviceRequestId}` | Query | `GetAdminServiceRequestOperationDetailQuery(serviceRequestId, auth)` | `IServiceRequestAdminBffRemoteCall.GetAdminServiceRequestDetail` | `GET /api/v1/admin/service-requests/{serviceRequestId}` | ✅ Admin |
| `PATCH /api/v1/admin-panel/service-requests/{serviceRequestId}/cancel` | Command | `CancelServiceRequestCommand(id, request, auth)` | `IServiceRequestAdminBffRemoteCall.CancelServiceRequest` | `PATCH /api/v1/service-requests/{serviceRequestId}/cancel` | ✅ Admin |
| `PATCH /api/v1/admin-panel/service-requests/{serviceRequestId}/completion/approve` | Command | `ApproveCompletionCommand(id, request, auth)` | `IServiceRequestAdminBffRemoteCall.ApproveCompletion` | `PATCH /api/v1/service-requests/{serviceRequestId}/completion/approve` | ✅ Admin |
| `PATCH /api/v1/admin-panel/service-requests/{serviceRequestId}/completion/reject` | Command | `RejectCompletionCommand(id, request, auth)` | `IServiceRequestAdminBffRemoteCall.RejectCompletion` | `PATCH /api/v1/service-requests/{serviceRequestId}/completion/reject` | ✅ Admin |
| `PATCH /api/v1/admin-panel/service-requests/{serviceRequestId}/disputes/{disputeId}/status` | Command | `ChangeDisputeStatusCommand(srId, dId, request, auth)` | `IServiceRequestAdminBffRemoteCall.ChangeDisputeStatus` | `PATCH /api/v1/service-requests/{serviceRequestId}/dispute/{disputeId}/status` | ✅ Admin |
| `PATCH /api/v1/admin-panel/service-requests/{serviceRequestId}/disputes/{disputeId}/resolve` | Command | `ResolveDisputeCommand(srId, dId, request, auth)` | `IServiceRequestAdminBffRemoteCall.ResolveDispute` | `PATCH /api/v1/service-requests/{serviceRequestId}/dispute/{disputeId}/resolve` | ✅ Admin |

## Files

| BFF Endpoint | Method | CQRS | Remote Call Interface | Module Endpoint | Auth |
|---|---|---|---|---|---|
| `GET /api/v1/admin-panel/files/{fileId}` | Query | `GetAdminFileReviewOverviewQuery(fileId, auth)` | `IFileStorageAdminBffRemoteCall.GetFileMetadata` | `GET /api/v1/files/{fileId}` | ✅ Admin |
| `POST /api/v1/admin-panel/files/bulk-read-urls` | Command | `BulkGenerateReadUrlsCommand(fileIds, expiresInMinutes, auth)` | `IFileStorageAdminBffRemoteCall.CreateReadUrl` (per file) | `POST /api/v1/files/{fileId}/access/read-url` | ✅ Admin |
| `DELETE /api/v1/admin-panel/files/{fileId}` | Command | `DeleteFileCommand(fileId, auth)` | `IFileStorageAdminBffRemoteCall.DeleteFile` | `DELETE /api/v1/files/{fileId}` | ✅ Admin |

## Reference Data (No Auth)

| BFF Endpoint | Method | CQRS | Remote Call Interface | Module Endpoint | Auth |
|---|---|---|---|---|---|
| `GET /api/v1/admin-panel/reference-data/lookup-groups` | — | Direct remote call (no CQRS) | `IReferenceDataAdminBffRemoteCall.GetLookupGroups` | `GET /api/v1/lookup-groups` | ❌ None |
| `GET /api/v1/admin-panel/reference-data/lookup-tree` | — | Direct remote call | `IReferenceDataAdminBffRemoteCall.GetLookupGroupTree` | `GET /api/v1/lookup-groups/tree` | ❌ None |
| `GET /api/v1/admin-panel/reference-data/lookup/{groupCode}/items` | — | Direct remote call | `IReferenceDataAdminBffRemoteCall.GetLookupItemsByGroupCode` | `GET /api/v1/lookup-groups/{groupCode}/items` | ❌ None |
| `GET /api/v1/admin-panel/reference-data/currencies` | — | Direct remote call | `IReferenceDataAdminBffRemoteCall.GetCurrencies` | `GET /api/v1/currencies` | ❌ None |
| `GET /api/v1/admin-panel/reference-data/locations/countries` | — | Direct remote call | `IReferenceDataAdminBffRemoteCall.GetCountries` | `GET /api/v1/locations/countries` | ❌ None |
| `GET /api/v1/admin-panel/reference-data/locations/cities` | — | Direct remote call | `IReferenceDataAdminBffRemoteCall.GetCities` | `GET /api/v1/locations/cities` | ❌ None |
| `GET /api/v1/admin-panel/reference-data/measurement-units` | — | Direct remote call | `IReferenceDataAdminBffRemoteCall.GetMeasurementUnits` | `GET /api/v1/measurement-units` | ❌ None |
| `GET /api/v1/admin-panel/reference-data/system-parameters` | — | Direct remote call | `IReferenceDataAdminBffRemoteCall.GetSystemParameters` | `GET /api/v1/system-parameters` | ❌ None |

> **Note:** `AdminReferenceDataController` calls the remote call interface directly without going through `IAizenCQRSProcessor`, as reference data is read-only and requires no auth enrichment.
