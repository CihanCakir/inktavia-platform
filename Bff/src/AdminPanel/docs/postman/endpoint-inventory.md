# Admin Panel BFF — Endpoint Inventory

Base path: `http://localhost:5200/api/v1/admin-panel`

All endpoints require `Authorization: Bearer {token}` unless marked **[No Auth]**.

---

## Dashboard

### GET /dashboard/overview
- **Description:** Returns aggregated admin dashboard metrics (pending profiles, vessel counts, open disputes, recent service requests)
- **Query Params:** none
- **Request Body:** none
- **Response:** `AdminDashboardOverviewResponse`
- **Auth:** Admin role required

---

## Identity

### GET /identity/profiles
- **Description:** Search user profiles with optional filters
- **Query Params:**
  - `roleContext` (string, optional) — e.g. `Organizer`, `Venue`, `Participant`, `General`
  - `approvalStatus` (string, optional) — e.g. `Pending`, `Approved`, `Rejected`
  - `pageIndex` (int, default 0)
  - `pageSize` (int, default 20)
- **Request Body:** none
- **Response:** `AdminUserOverviewResponse` (paged)
- **Auth:** Admin role required

### GET /identity/profiles/{profileId}
- **Description:** Get full detail of a single user profile
- **Path Params:** `profileId` (Guid)
- **Query Params:**
  - `roleContext` (string, default `General`)
- **Request Body:** none
- **Response:** `ProfileDetailResult`
- **Auth:** Admin role required

### POST /identity/organizers/{userId}/profiles/{profileId}/approve
- **Description:** Approve an organizer's profile
- **Path Params:** `userId` (long), `profileId` (Guid)
- **Request Body:** none
- **Response:** `AdminBffCommandResultDto`
- **Auth:** Admin role required

### POST /identity/organizers/{userId}/profiles/{profileId}/reject
- **Description:** Reject an organizer's profile with a reason
- **Path Params:** `userId` (long), `profileId` (Guid)
- **Request Body:**
  ```json
  { "reason": "string" }
  ```
- **Response:** `AdminBffCommandResultDto`
- **Auth:** Admin role required

### POST /identity/venues/{userId}/profiles/{profileId}/approve
- **Description:** Approve a venue's profile
- **Path Params:** `userId` (long), `profileId` (Guid)
- **Request Body:** none
- **Response:** `AdminBffCommandResultDto`
- **Auth:** Admin role required

### POST /identity/venues/{userId}/profiles/{profileId}/reject
- **Description:** Reject a venue's profile with a reason
- **Path Params:** `userId` (long), `profileId` (Guid)
- **Request Body:**
  ```json
  { "reason": "string" }
  ```
- **Response:** `AdminBffCommandResultDto`
- **Auth:** Admin role required

---

## Vessels

### GET /vessels
- **Description:** List all vessels in the system (admin view)
- **Query Params:**
  - `pageIndex` (int, default 0)
  - `pageSize` (int, default 20)
  - `searchTerm` (string, optional)
  - `isArchived` (bool, optional)
- **Request Body:** none
- **Response:** `AdminVesselOverviewResponse` (paged)
- **Auth:** Admin role required

### GET /vessels/{vesselId}/detail
- **Description:** Get documents and detail for a specific vessel
- **Path Params:** `vesselId` (long)
- **Request Body:** none
- **Response:** `AdminVesselDocumentsResponse`
- **Auth:** Admin role required

### PATCH /vessels/{vesselId}/archive
- **Description:** Archive a vessel
- **Path Params:** `vesselId` (long)
- **Request Body:**
  ```json
  { "reason": "string" }
  ```
- **Response:** `ArchiveVesselResponse`
- **Auth:** Admin role required

### PATCH /vessels/{vesselId}/restore
- **Description:** Restore an archived vessel
- **Path Params:** `vesselId` (long)
- **Request Body:** none
- **Response:** `RestoreVesselResponse`
- **Auth:** Admin role required

### PATCH /vessels/{vesselId}/status
- **Description:** Update a vessel's operational status
- **Path Params:** `vesselId` (long)
- **Request Body:**
  ```json
  { "status": "string" }
  ```
- **Response:** `UpdateVesselStatusResponse`
- **Auth:** Admin role required

### DELETE /vessels/{vesselId}/documents/{documentId}
- **Description:** Remove a specific document from a vessel
- **Path Params:** `vesselId` (long), `documentId` (long)
- **Request Body:** none
- **Response:** `RemoveVesselDocumentResponse`
- **Auth:** Admin role required

---

## Service Requests

### GET /service-requests
- **Description:** List all service requests (admin view)
- **Query Params:**
  - `status` (string, optional)
  - `vesselId` (long, optional)
  - `pageIndex` (int, default 0)
  - `pageSize` (int, default 20)
- **Request Body:** none
- **Response:** `AdminServiceRequestListResponse` (paged)
- **Auth:** Admin role required

### GET /service-requests/disputes
- **Description:** List all disputed service requests
- **Query Params:**
  - `status` (string, optional)
  - `pageIndex` (int, default 0)
  - `pageSize` (int, default 20)
- **Request Body:** none
- **Response:** `GetAdminDisputeListResponse` (paged)
- **Auth:** Admin role required

### GET /service-requests/{serviceRequestId}
- **Description:** Get full operation detail for a service request
- **Path Params:** `serviceRequestId` (long)
- **Request Body:** none
- **Response:** `AdminServiceRequestOperationDetailResponse`
- **Auth:** Admin role required

### PATCH /service-requests/{serviceRequestId}/cancel
- **Description:** Admin cancels a service request
- **Path Params:** `serviceRequestId` (long)
- **Request Body:**
  ```json
  { "reason": "string" }
  ```
- **Response:** `CancelServiceRequestResponse`
- **Auth:** Admin role required

### PATCH /service-requests/{serviceRequestId}/completion/approve
- **Description:** Admin approves completion of a service request
- **Path Params:** `serviceRequestId` (long)
- **Request Body:**
  ```json
  { "notes": "string" }
  ```
- **Response:** `ApproveServiceRequestCompletionResponse`
- **Auth:** Admin role required

### PATCH /service-requests/{serviceRequestId}/completion/reject
- **Description:** Admin rejects completion claim for a service request
- **Path Params:** `serviceRequestId` (long)
- **Request Body:**
  ```json
  { "reason": "string" }
  ```
- **Response:** `RejectServiceRequestCompletionResponse`
- **Auth:** Admin role required

### PATCH /service-requests/{serviceRequestId}/disputes/{disputeId}/status
- **Description:** Change the status of a dispute
- **Path Params:** `serviceRequestId` (long), `disputeId` (long)
- **Request Body:**
  ```json
  { "status": "string" }
  ```
- **Response:** `AdminBffCommandResultDto`
- **Auth:** Admin role required

### PATCH /service-requests/{serviceRequestId}/disputes/{disputeId}/resolve
- **Description:** Admin resolves a dispute with a resolution note and decision
- **Path Params:** `serviceRequestId` (long), `disputeId` (long)
- **Request Body:**
  ```json
  { "resolution": "string", "favoredParty": "string" }
  ```
- **Response:** `ResolveServiceRequestDisputeResponse`
- **Auth:** Admin role required

---

## Files

### GET /files/{fileId}
- **Description:** Get file metadata and review overview
- **Path Params:** `fileId` (Guid)
- **Request Body:** none
- **Response:** `AdminFileReviewOverviewResponse`
- **Auth:** Admin role required

### POST /files/bulk-read-urls
- **Description:** Generate pre-signed S3 read URLs for multiple files
- **Request Body:**
  ```json
  {
    "fileIds": ["guid1", "guid2"],
    "expiresInMinutes": 60
  }
  ```
- **Response:** `List<FileAccessUrlResult>`
- **Auth:** Admin role required

### DELETE /files/{fileId}
- **Description:** Permanently delete a file
- **Path Params:** `fileId` (Guid)
- **Request Body:** none
- **Response:** `AdminBffCommandResultDto`
- **Auth:** Admin role required

---

## Reference Data [No Auth]

### GET /reference-data/lookup-groups
- **Response:** `LookupGroupListResult` (flat list of groups)

### GET /reference-data/lookup-tree
- **Response:** `LookupGroupTreeResult` (hierarchical tree)

### GET /reference-data/lookup/{groupCode}/items
- **Path Params:** `groupCode` (string)
- **Response:** `LookupItemListResult`

### GET /reference-data/currencies
- **Response:** `CurrencyListResult`

### GET /reference-data/locations/countries
- **Response:** `CountryListResult`

### GET /reference-data/locations/cities
- **Query Params:** `countryId` (long, optional)
- **Response:** `CityListResult`

### GET /reference-data/measurement-units
- **Query Params:** `type` (string, optional)
- **Response:** `MeasurementUnitListResult`

### GET /reference-data/system-parameters
- **Response:** `SystemParameterListResult`
