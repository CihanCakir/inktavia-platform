# Admin Panel BFF — Endpoint Catalog

Base path: `api/v1/admin-panel`  
Authorization: all endpoints require `Authorization: Bearer <keycloakAccessToken>` unless marked `[AllowAnonymous]`.  
All protected endpoints also require `X-Aizen-User-Token: Bearer <identityAccessToken>`.

---

## Auth — `api/v1/admin-panel/auth`

| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| POST | `/auth/login/username` | Anonymous | Login with username + password; returns Identity tokens |
| POST | `/auth/login/phone` | Anonymous | Login with phone number + password |
| POST | `/auth/login/otp` | Anonymous | Login with phone + OTP code |
| POST | `/auth/otp/send` | Anonymous | Send OTP code to a phone number |
| POST | `/auth/otp/check` | Anonymous | Verify an OTP code (dry check, no session created) |
| POST | `/auth/refresh` | Anonymous | Refresh Identity access + refresh tokens |
| POST | `/auth/password/change` | Bearer + User Token | Change the authenticated user's password |

---

## Dashboard — `api/v1/admin-panel`

| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| GET | `/dashboard/overview` | Admin | Aggregated counts: vessels, active service requests, open disputes, pending approvals |

---

## Identity — `api/v1/admin-panel`

### General Profiles

| Method | Path | Query Params | Purpose |
|--------|------|--------------|---------|
| GET | `/identity/profiles` | `roleContext`, `approvalStatus`, `pageIndex`, `pageSize` | Paged list of all user profiles filtered by role and approval status |
| GET | `/identity/profiles/{profileId:guid}` | `roleContext` (default: `General`) | Full profile detail for a given profile GUID |
| GET | `/identity/profiles/{profileId:guid}/with-roles` | — | Profile detail including all assigned role contexts |

### Organizer Profiles

| Method | Path | Query Params | Purpose |
|--------|------|--------------|---------|
| GET | `/identity/organizers/profiles` | `pageIndex`, `pageSize` | Paged organizer profiles |
| GET | `/identity/organizers/profiles/{profileId:guid}` | — | Single organizer profile detail |
| GET | `/identity/organizers/profiles/{profileId:guid}/with-user` | — | Organizer profile + linked user detail |
| POST | `/identity/organizers/{userId:long}/profiles/{profileId:guid}/approve` | — | Approve organizer profile (triggers identity state change) |
| POST | `/identity/organizers/{userId:long}/profiles/{profileId:guid}/reject` | — | Reject organizer profile; body: `{ reason: string }` |

### Venue Profiles

| Method | Path | Query Params | Purpose |
|--------|------|--------------|---------|
| GET | `/identity/venues/profiles` | `pageIndex`, `pageSize` | Paged venue profiles |
| GET | `/identity/venues/profiles/{profileId:guid}` | — | Single venue profile detail |
| POST | `/identity/venues/{userId:long}/profiles/{profileId:guid}/approve` | — | Approve venue profile |
| POST | `/identity/venues/{userId:long}/profiles/{profileId:guid}/reject` | — | Reject venue profile; body: `{ reason: string }` |

### Participant Profiles

| Method | Path | Query Params | Purpose |
|--------|------|--------------|---------|
| GET | `/identity/participant/profiles` | `pageIndex`, `pageSize` | Paged participant profiles |
| GET | `/identity/participant/profiles/{profileId:guid}` | — | Single participant profile detail |

---

## Files — `api/v1/admin-panel`

| Method | Path | Body / Query | Purpose |
|--------|------|--------------|---------|
| GET | `/files/{fileId:long}` | — | File metadata for admin file review screen |
| POST | `/files/{fileId:long}/read-url` | `{ expiresInMinutes: int }` | Generate a single pre-signed S3 read URL |
| POST | `/files/bulk-read-urls` | `{ fileIds: long[], expiresInMinutes: int }` | Bulk generate pre-signed S3 read URLs |
| DELETE | `/files/{fileId:long}` | — | Soft-delete a file |
| PATCH | `/files/{fileId:long}/visibility` | `{ visibility: string }` | Update file public/private access policy |

---

## Vessels — `api/v1/admin-panel`

| Method | Path | Query Params / Body | Purpose |
|--------|------|---------------------|---------|
| GET | `/vessels` | `pageIndex`, `pageSize`, `searchTerm`, `isArchived` | Paged vessel list with optional search and archive filter |
| GET | `/vessels/form-options` | — | Static dropdown options: vessel types, status options |
| GET | `/vessels/{vesselId:long}` | — | Single vessel detail |
| GET | `/vessels/{vesselId:long}/detail` | — | Aggregated vessel detail + documents + owners |
| PUT | `/vessels/{vesselId:long}` | `UpdateVesselRequest` | Full update of a vessel record |
| PATCH | `/vessels/{vesselId:long}/archive` | `ArchiveVesselRequest` | Archive a vessel |
| PATCH | `/vessels/{vesselId:long}/restore` | — | Restore an archived vessel |
| PATCH | `/vessels/{vesselId:long}/status` | `UpdateVesselStatusRequest` | Update vessel operational status |
| DELETE | `/vessels/{vesselId:long}/documents/{documentId:long}` | — | Remove a vessel document |

---

## Service Requests — `api/v1/admin-panel`

| Method | Path | Query Params / Body | Purpose |
|--------|------|---------------------|---------|
| GET | `/service-requests` | `status`, `vesselId`, `pageIndex`, `pageSize` | Paged service request list with status and vessel filters |
| GET | `/service-requests/filter-options` | — | Static filter option lists (status, dispute status) |
| GET | `/service-requests/disputes` | `status`, `pageIndex`, `pageSize` | Paged dispute list |
| GET | `/service-requests/{serviceRequestId:long}` | — | Full service request operation detail |
| GET | `/service-requests/{serviceRequestId:long}/timeline` | — | Service request event timeline |
| PATCH | `/service-requests/{serviceRequestId:long}/cancel` | `CancelServiceRequestRequest` | Admin-cancel a service request |
| PATCH | `/service-requests/{serviceRequestId:long}/completion/approve` | `ApproveServiceRequestCompletionRequest` | Approve a completion request |
| PATCH | `/service-requests/{serviceRequestId:long}/completion/reject` | `RejectServiceRequestCompletionRequest` | Reject a completion request |
| PATCH | `/service-requests/{serviceRequestId:long}/disputes/{disputeId:long}/status` | `ChangeServiceRequestDisputeStatusRequest` | Update dispute status |
| PATCH | `/service-requests/{serviceRequestId:long}/disputes/{disputeId:long}/resolve` | `ResolveServiceRequestDisputeRequest` | Resolve a dispute |

---

## Reference Data — `api/v1/admin-panel`

| Method | Path | Query Params | Purpose |
|--------|------|--------------|---------|
| GET | `/reference-data/lookup-groups` | — | All lookup group definitions |
| GET | `/reference-data/lookup-tree` | — | Hierarchical lookup group tree |
| GET | `/reference-data/lookup/{groupCode}/items` | — | Lookup items for a specific group code |
| GET | `/reference-data/currencies` | — | All supported currencies |
| GET | `/reference-data/locations/countries` | — | All countries |
| GET | `/reference-data/locations/cities` | `countryId` (optional) | All cities, optionally filtered by country |
| GET | `/reference-data/measurement-units` | `type` (optional) | Measurement units, optionally filtered by type |
| GET | `/reference-data/system-parameters` | — | Global system parameters |
