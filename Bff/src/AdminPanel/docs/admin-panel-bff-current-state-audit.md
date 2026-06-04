# AdminPanel BFF — Current State Audit

Generated: 2026-06-04

---

## Existing Controllers

| Class | File | Route Prefix | Note |
|-------|------|-------------|------|
| `AdminDashboardController` | Controllers/V1/AdminDashboardController.cs | `/api/v1/admin-panel/dashboard` | Uses CQRS ✅ |
| `AdminFilesController` | Controllers/V1/AdminFilesController.cs | `/api/v1/admin-panel/files` | Uses CQRS ✅ |
| `AdminIdentityController` | Controllers/V1/AdminIdentityController.cs | `/api/v1/admin-panel/identity` | Uses CQRS ✅ |
| `AdminReferenceDataController` | Controllers/V1/AdminReferenceDataController.cs | `/api/v1/admin-panel/reference-data` | **Calls remote call directly — bypasses CQRS ❌** |
| `AdminServiceRequestsController` | Controllers/V1/AdminServiceRequestsController.cs | `/api/v1/admin-panel/service-requests` | Uses CQRS ✅ |
| `AdminVesselsController` | Controllers/V1/AdminVesselsController.cs | `/api/v1/admin-panel/vessels` | Uses CQRS ✅ |

---

## Existing Application Commands/Queries

### AdminDashboard
| File | Issue |
|------|-------|
| `AdminDashboard/Query/GetAdminDashboardOverviewQuery.cs` | Query + Handler in same file ❌ |

### AdminFiles
| File | Issue |
|------|-------|
| `AdminFiles/Command/BulkGenerateReadUrlsCommand.cs` | Command + Handler in same file ❌ |
| `AdminFiles/Command/DeleteFileCommand.cs` | Command + Handler in same file ❌ |
| `AdminFiles/Query/GetAdminFileReviewOverviewQuery.cs` | Query + Handler in same file ❌ |

### AdminIdentity
| File | Issue |
|------|-------|
| `AdminIdentity/Command/ApproveOrganizerProfileCommand.cs` | Command + Handler in same file ❌ |
| `AdminIdentity/Command/ApproveVenueProfileCommand.cs` | Command + Handler in same file ❌ |
| `AdminIdentity/Command/RejectOrganizerProfileCommand.cs` | Command + Handler in same file ❌ |
| `AdminIdentity/Command/RejectVenueProfileCommand.cs` | Command + Handler in same file ❌ |
| `AdminIdentity/Query/GetAdminProfileDetailQuery.cs` | Query + Handler in same file ❌ |
| `AdminIdentity/Query/GetAdminProfilesQuery.cs` | Query + Handler in same file ❌ |

### AdminServiceRequests
| File | Issue |
|------|-------|
| `AdminServiceRequests/Command/ApproveCompletionCommand.cs` | Command + Handler in same file ❌ |
| `AdminServiceRequests/Command/CancelServiceRequestCommand.cs` | Command + Handler in same file ❌ |
| `AdminServiceRequests/Command/ChangeDisputeStatusCommand.cs` | Command + Handler in same file ❌ |
| `AdminServiceRequests/Command/RejectCompletionCommand.cs` | Command + Handler in same file ❌ |
| `AdminServiceRequests/Command/ResolveDisputeCommand.cs` | Command + Handler in same file ❌ |
| `AdminServiceRequests/Query/GetAdminDisputeListQuery.cs` | Query + Handler in same file ❌ |
| `AdminServiceRequests/Query/GetAdminServiceRequestFilterOptionsQuery.cs` | Query + Handler in same file ❌ |
| `AdminServiceRequests/Query/GetAdminServiceRequestListQuery.cs` | Query + Handler in same file ❌ |
| `AdminServiceRequests/Query/GetAdminServiceRequestOperationDetailQuery.cs` | Query + Handler in same file ❌ — also **defined but not wired in controller** ❌ |
| `AdminServiceRequests/Query/GetAdminServiceRequestTimelineQuery.cs` | Query + Handler in same file ❌ — also **not wired in controller** ❌ |

### AdminVessels
| File | Issue |
|------|-------|
| `AdminVessels/Command/ArchiveVesselCommand.cs` | Command + Handler in same file ❌ |
| `AdminVessels/Command/RemoveVesselDocumentCommand.cs` | Command + Handler in same file ❌ |
| `AdminVessels/Command/RestoreVesselCommand.cs` | Command + Handler in same file ❌ |
| `AdminVessels/Command/UpdateVesselStatusCommand.cs` | Command + Handler in same file ❌ |
| `AdminVessels/Query/GetAdminVesselDocumentsQuery.cs` | Query + Handler in same file ❌ |
| `AdminVessels/Query/GetAdminVesselFormOptionsQuery.cs` | Query + Handler in same file ❌ |
| `AdminVessels/Query/GetAdminVesselOverviewQuery.cs` | Query + Handler in same file ❌ |

Total CQRS structure violations: **27 files**

---

## Existing Remote Service Interfaces

| Interface | Location | Status |
|-----------|----------|--------|
| `IIdentityAdminBffRemoteCall` | Common/RemoteClients/ | ✅ — Missing auth methods; missing `X-Aizen-User-Token` param |
| `IReferenceDataAdminBffRemoteCall` | Common/RemoteClients/ | ✅ — Missing admin CRUD methods; missing exchange rate methods |
| `IVesselAdminBffRemoteCall` | Common/RemoteClients/ | ✅ — Missing spec/engine/location/status-history methods |
| `IFileStorageAdminBffRemoteCall` | Common/RemoteClients/ | ⚠️ — Uses `Guid` fileId but module uses `long`; missing upload session |
| `IServiceRequestAdminBffRemoteCall` | Common/RemoteClients/ | ✅ — Missing timeline/offers/work-logs/messages/assignment methods |

---

## Existing AizenRemoteCall Usage

All 5 remote call interfaces use `IAizenRemoteCall` base and `[AizenRemoteCallGet/Post/Put/Patch/Delete]` attributes. Pattern is correct ✅.

All are registered in `DependencyInjection.cs` via `RestService.For<T>()` factory ✅.

---

## Existing Appsettings

All 5 remote service base URLs correctly configured under `RemoteCalls` section in `appsettings.Local.json` ✅.

---

## Auth Header Forwarding

| Header | Forwarded? |
|--------|-----------|
| `Authorization: Bearer {Keycloak token}` | ✅ All controllers extract and pass it |
| `X-Aizen-User-Token: {Identity token}` | ❌ **Not forwarded anywhere** |

---

## Missing or Suspicious Areas

1. **No auth controller** — LoginWithUsername, LoginWithPhone, LoginWithOtp, SendOtp, CheckOtp, Refresh, ChangePassword not exposed
2. **AdminReferenceDataController** — calls `_referenceData` directly, not through `IAizenCQRSProcessor`
3. **X-Aizen-User-Token** — never extracted or forwarded
4. **All Application files** — command/query and handler in the same file (27 files)
5. **FileStorage fileId** — BFF uses `Guid`; module API uses `long`
6. **GetAdminServiceRequestOperationDetailQuery** — defined but no controller endpoint
7. **GetAdminServiceRequestTimelineQuery** — defined but no controller endpoint
8. **AdminUsers folder** — empty (no queries/commands)
9. **Missing granular Identity endpoints** — participant/venue/organizer detail/filter/list/update
10. **Missing ReferenceData admin CRUD** — currency, exchange-rate, lookup, location, measurement, system-parameter
11. **Missing Vessel read endpoints** — specification, engines, location, status history
12. **Missing FileStorage upload session** endpoints
13. **Controller naming** — all carry `Admin` prefix; spec requires removal
