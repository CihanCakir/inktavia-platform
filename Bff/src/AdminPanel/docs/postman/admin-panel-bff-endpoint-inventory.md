# Admin Panel BFF — Endpoint Inventory

Base path prefix: `api/v1/admin-panel`

---

## Auth (`AuthController`)

| Method | Route | Auth Required | Description | Request Body | Response Type |
|--------|-------|---------------|-------------|--------------|---------------|
| POST | `/auth/login/username` | ❌ Anonymous | Login with username + PIN | `LoginWithUsernameRequest` | `UserLoginResponse` |
| POST | `/auth/login/phone` | ❌ Anonymous | Login with phone + password | `LoginWithPhoneRequest` | `UserLoginResponse` |
| POST | `/auth/login/otp` | ❌ Anonymous | Login with OTP code | `LoginWithOtpRequest` | `UserLoginResponse` |
| POST | `/auth/otp/send` | ❌ Anonymous | Send OTP to phone | `SendOtpRequest` | `SendOtpDto` |
| POST | `/auth/otp/check` | ❌ Anonymous | Validate OTP code | `CheckOtpRequest` | `CheckOtpDto` |
| POST | `/auth/refresh` | ❌ Anonymous | Refresh access token | `RefreshLoginHttpRequest` | `UserLoginResponse` |
| POST | `/auth/password/change` | ✅ Authorized | Change password | `ChangePasswordRequest` | `ChangePasswordDto` |

---

## Dashboard (`AdminDashboardController`)

| Method | Route | Auth Required | Description | Request Body | Response Type |
|--------|-------|---------------|-------------|--------------|---------------|
| GET | `/dashboard/overview` | ✅ Admin | Get admin dashboard overview | — | `AdminDashboardOverviewResponse` |

---

## Identity — Profiles (`IdentityController`)

| Method | Route | Auth Required | Description | Request Body | Response Type |
|--------|-------|---------------|-------------|--------------|---------------|
| GET | `/identity/profiles` | ✅ Admin | Search all profiles | Query: `roleContext`, `approvalStatus`, `pageIndex`, `pageSize` | `AdminUserOverviewResponse` |
| GET | `/identity/profiles/{profileId:guid}` | ✅ Admin | Get profile detail by ID | Query: `roleContext` | `ProfileDetailResult` |
| GET | `/identity/profiles/{profileId:guid}/with-roles` | ✅ Admin | Get profile with roles | — | `ProfileWithRolesResult` |
| POST | `/identity/organizers/{userId:long}/profiles/{profileId:guid}/approve` | ✅ Admin | Approve organizer profile | — | `AdminBffCommandResultDto` |
| POST | `/identity/organizers/{userId:long}/profiles/{profileId:guid}/reject` | ✅ Admin | Reject organizer profile | `AdminBffRejectRequest` | `AdminBffCommandResultDto` |
| POST | `/identity/venues/{userId:long}/profiles/{profileId:guid}/approve` | ✅ Admin | Approve venue profile | — | `AdminBffCommandResultDto` |
| POST | `/identity/venues/{userId:long}/profiles/{profileId:guid}/reject` | ✅ Admin | Reject venue profile | `AdminBffRejectRequest` | `AdminBffCommandResultDto` |

---

## Identity — Organizers (`OrganizersController`)

| Method | Route | Auth Required | Description | Request Body | Response Type |
|--------|-------|---------------|-------------|--------------|---------------|
| GET | `/identity/organizers/profiles` | ✅ Admin | List organizer profiles (paged) | Query: `pageIndex`, `pageSize` | `PagedOrganizerProfileResult` |
| GET | `/identity/organizers/profiles/{profileId:guid}` | ✅ Admin | Get organizer profile by ID | — | `OrganizerProfileResult` |
| GET | `/identity/organizers/profiles/{profileId:guid}/with-user` | ✅ Admin | Get organizer profile with user | — | `OrganizerProfileWithUserResult` |

---

## Identity — Venues (`VenuesController`)

| Method | Route | Auth Required | Description | Request Body | Response Type |
|--------|-------|---------------|-------------|--------------|---------------|
| GET | `/identity/venues/profiles` | ✅ Admin | List venue profiles (paged) | Query: `pageIndex`, `pageSize` | `PagedVenueProfileResult` |
| GET | `/identity/venues/profiles/{profileId:guid}` | ✅ Admin | Get venue profile by ID | — | `VenueProfileResult` |

---

## Identity — Participants (`ParticipantsController`)

| Method | Route | Auth Required | Description | Request Body | Response Type |
|--------|-------|---------------|-------------|--------------|---------------|
| GET | `/identity/participant/profiles` | ✅ Admin | List participant profiles (paged) | Query: `pageIndex`, `pageSize` | `PagedParticipantProfileResult` |
| GET | `/identity/participant/profiles/{profileId:guid}` | ✅ Admin | Get participant profile by ID | — | `ParticipantProfileResult` |

---

## Vessels (`VesselsController`)

| Method | Route | Auth Required | Description | Request Body | Response Type |
|--------|-------|---------------|-------------|--------------|---------------|
| GET | `/vessels` | ✅ Admin | List vessels (paged, filterable) | Query: `pageIndex`, `pageSize`, `searchTerm`, `isArchived` | `AdminVesselOverviewResponse` |
| GET | `/vessels/{vesselId:long}` | ✅ Admin | Get vessel by ID | — | `GetVesselDetailResponse` |
| GET | `/vessels/{vesselId:long}/detail` | ✅ Admin | Get vessel documents | — | `AdminVesselDocumentsResponse` |
| GET | `/vessels/form-options` | ✅ Admin | Get vessel form options | — | `AdminVesselFormOptionsResponse` |
| PUT | `/vessels/{vesselId:long}` | ✅ Admin | Update vessel | `UpdateVesselRequest` | `UpdateVesselResponse` |
| PATCH | `/vessels/{vesselId:long}/archive` | ✅ Admin | Archive vessel | `ArchiveVesselRequest` | `ArchiveVesselResponse` |
| PATCH | `/vessels/{vesselId:long}/restore` | ✅ Admin | Restore vessel | — | `RestoreVesselResponse` |
| PATCH | `/vessels/{vesselId:long}/status` | ✅ Admin | Update vessel status | `UpdateVesselStatusRequest` | `UpdateVesselStatusResponse` |
| DELETE | `/vessels/{vesselId:long}/documents/{documentId:long}` | ✅ Admin | Remove vessel document | — | `RemoveVesselDocumentResponse` |

---

## Files (`FilesController`)

| Method | Route | Auth Required | Description | Request Body | Response Type |
|--------|-------|---------------|-------------|--------------|---------------|
| GET | `/files/{fileId:long}` | ✅ Admin | Get file review overview | — | `AdminFileReviewOverviewResponse` |
| POST | `/files/bulk-read-urls` | ✅ Admin | Bulk generate pre-signed read URLs | `BulkGenerateReadUrlsRequest` (`FileIds[]`, `ExpiresInMinutes`) | `List<FileAccessUrlResult>` |
| POST | `/files/{fileId:long}/read-url` | ✅ Admin | Generate read URL for single file | `CreateReadUrlRequest` (`ExpiresInMinutes`) | `FileAccessUrlResult` |
| PATCH | `/files/{fileId:long}/visibility` | ✅ Admin | Update file visibility | `UpdateVisibilityRequest` (`Visibility`) | `EmptyResult` |
| DELETE | `/files/{fileId:long}` | ✅ Admin | Delete file | — | `AdminBffCommandResultDto` |

---

## Service Requests (`ServiceRequestsController`)

| Method | Route | Auth Required | Description | Request Body | Response Type |
|--------|-------|---------------|-------------|--------------|---------------|
| GET | `/service-requests` | ✅ Admin | List service requests (paged, filterable) | Query: `status`, `vesselId`, `pageIndex`, `pageSize` | `AdminServiceRequestListResponse` |
| GET | `/service-requests/disputes` | ✅ Admin | List disputes (paged, filterable) | Query: `status`, `pageIndex`, `pageSize` | `GetAdminDisputeListResponse` |
| GET | `/service-requests/filter-options` | ✅ Admin | Get filter option lists | — | `AdminServiceRequestFilterOptionsResponse` |
| GET | `/service-requests/{serviceRequestId:long}` | ✅ Admin | Get service request detail | — | `AdminServiceRequestOperationDetailResponse` |
| GET | `/service-requests/{serviceRequestId:long}/timeline` | ✅ Admin | Get service request timeline | — | `AdminServiceRequestTimelineResponse` |
| PATCH | `/service-requests/{serviceRequestId:long}/cancel` | ✅ Admin | Cancel service request | `CancelServiceRequestRequest` | `CancelServiceRequestResponse` |
| PATCH | `/service-requests/{serviceRequestId:long}/completion/approve` | ✅ Admin | Approve completion | `ApproveServiceRequestCompletionRequest` | `ApproveServiceRequestCompletionResponse` |
| PATCH | `/service-requests/{serviceRequestId:long}/completion/reject` | ✅ Admin | Reject completion | `RejectServiceRequestCompletionRequest` | `RejectServiceRequestCompletionResponse` |
| PATCH | `/service-requests/{serviceRequestId:long}/disputes/{disputeId:long}/status` | ✅ Admin | Change dispute status | `ChangeServiceRequestDisputeStatusRequest` | `AdminBffCommandResultDto` |
| PATCH | `/service-requests/{serviceRequestId:long}/disputes/{disputeId:long}/resolve` | ✅ Admin | Resolve dispute | `ResolveServiceRequestDisputeRequest` | `ResolveServiceRequestDisputeResponse` |

---

## Reference Data (`ReferenceDataController`)

| Method | Route | Auth Required | Description | Request Body | Response Type |
|--------|-------|---------------|-------------|--------------|---------------|
| GET | `/reference-data/lookup-groups` | ✅ Admin | Get all lookup groups | — | lookup groups list |
| GET | `/reference-data/lookup-groups/tree` | ✅ Admin | Get lookup group tree | — | lookup group tree |
| GET | `/reference-data/lookup-groups/{groupCode}/items` | ✅ Admin | Get lookup items by group code | — | lookup items |
| GET | `/reference-data/currencies` | ✅ Admin | Get currencies | — | currencies list |
| GET | `/reference-data/countries` | ✅ Admin | Get countries | — | countries list |
| GET | `/reference-data/cities` | ✅ Admin | Get cities | — | cities list |
| GET | `/reference-data/measurement-units` | ✅ Admin | Get measurement units | — | measurement units |
| GET | `/reference-data/system-parameters` | ✅ Admin | Get system parameters | — | system parameters |

---

## Notes

- All routes are prefixed with `api/v1/admin-panel/`
- All headers: `Authorization: Bearer <token>`, `X-Aizen-User-Token: <token>` (required for authorized endpoints)
- Response envelope: `AizenApiResponse<T>`
