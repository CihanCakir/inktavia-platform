# Source Controller Endpoint Audit — AdminPanel BFF

Generated: 2026-06-04

Scanned modules: Identity, ReferenceData, Vessel, FileStorage, ServiceRequest

---

## Identity Module

### AuthorizationController (`/api/v1/auth`)

| Method | Route | DTO In | DTO Out | Auth | AdminPanel Relevant |
|--------|-------|--------|---------|------|---------------------|
| POST | `/auth/refresh` | `RefreshLoginHttpRequest` | `UserLoginResponse` | AllowAnonymous | ✅ |
| POST | `/auth/otp/send` | `SendOtpRequest` | `SendOtpDto` | AllowAnonymous | ✅ |
| POST | `/auth/otp/check` | `CheckOtpRequest` | `CheckOtpDto` | AllowAnonymous | ✅ |
| POST | `/auth/login/otp` | `LoginWithOtpRequest` | `UserLoginResponse` | AllowAnonymous | ✅ |
| POST | `/auth/login/phone` | `LoginWithPhoneRequest` | `UserLoginResponse` | AllowAnonymous | ✅ |
| POST | `/auth/login/username` | `LoginWithUsernameRequest` | `UserLoginResponse` | AllowAnonymous | ✅ — **primary admin login** |
| POST | `/auth/password/change` | `ChangePasswordRequest` | `ChangePasswordDto` | Authorize | ✅ |

### QueryController (`/api/v1/identity`)

| Method | Route | DTO In | DTO Out | Auth | AdminPanel Relevant |
|--------|-------|--------|---------|------|---------------------|
| GET | `/identity/profile/me` | — | `UserProfileDetailDto` | Authorize | ✅ |
| GET | `/identity/profiles/{profileId:long}` | — | `UserProfileDetailDto` | Admin | ✅ |
| GET | `/identity/profiles/{profileId:long}/with-roles` | — | `UserProfileWithRolesDto` | Admin | ✅ |
| GET | `/identity/profiles` | `GetUserProfilesByFilterRequest` (query) | `IPaginate<UserProfileListItemDto>` | Admin | ✅ |
| GET | `/identity/profiles/list` | `GetUserProfileListRequest` (query) | `IList<UserProfileListItemDto>` | Admin | ✅ |
| GET | `/identity/participant/profile/me` | — | `ParticipantProfileDetailDto` | Authorize | ✅ |
| GET | `/identity/participant/profiles/{profileId:long}` | — | `ParticipantProfileDetailDto` | Admin | ✅ |
| GET | `/identity/participant/profiles/{profileId:long}/with-user` | — | `ParticipantProfileWithUserDetailDto` | Admin | ✅ |
| GET | `/identity/participant/profiles` | `GetParticipantProfilesByFilterRequest` | `IPaginate<ParticipantProfileListItemDto>` | Admin | ✅ |
| GET | `/identity/participant/profiles/list` | `GetParticipantProfileListRequest` | `IList<ParticipantProfileListItemDto>` | Admin | ✅ |
| GET | `/identity/venues/profile/me` | — | `VenueProfileDetailDto` | Authorize | ✅ |
| GET | `/identity/venues/profiles/{profileId:long}` | — | `VenueProfileDetailDto` | Admin | ✅ |
| GET | `/identity/venues/profiles/{profileId:long}/with-user` | — | `VenueProfileWithUserDetailDto` | Admin | ✅ |
| GET | `/identity/venues/profiles` | `GetVenueProfilesByFilterRequest` | `IPaginate<VenueProfileListItemDto>` | Admin | ✅ |
| GET | `/identity/venues/profiles/list` | `GetVenueProfileListRequest` | `IList<VenueProfileListItemDto>` | Admin | ✅ |
| GET | `/identity/organizers/profile/me` | — | `OrganizerProfileDetailDto` | Authorize | ✅ |
| GET | `/identity/organizers/profiles/{profileId:long}` | — | `OrganizerProfileDetailDto` | Admin | ✅ |
| GET | `/identity/organizers/profiles/{profileId:long}/with-user` | — | `OrganizerProfileWithUserDetailDto` | Admin | ✅ |
| GET | `/identity/organizers/profiles` | `GetOrganizerProfilesByFilterRequest` | `IPaginate<OrganizerProfileListItemDto>` | Admin | ✅ |
| GET | `/identity/organizers/profiles/list` | `GetOrganizerProfileListRequest` | `IList<OrganizerProfileListItemDto>` | Admin | ✅ |

### AdminController (`/api/v1/identity`) [Admin role]

| Method | Route | DTO In | DTO Out | AdminPanel Relevant |
|--------|-------|--------|---------|---------------------|
| POST | `/identity/admin/organizers/{userId}/profiles/{profileId}/approve` | — | `VenueOrganizationRegistrationResponse` | ✅ already in BFF |
| POST | `/identity/admin/organizers/{userId}/profiles/{profileId}/reject` | `RejectProfileRequest` | `VenueOrganizationRegistrationResponse` | ✅ already in BFF |
| POST | `/identity/admin/venues/{userId}/profiles/{profileId}/approve` | — | `VenueOrganizationRegistrationResponse` | ✅ already in BFF |
| POST | `/identity/admin/venues/{userId}/profiles/{profileId}/reject` | `RejectProfileRequest` | `VenueOrganizationRegistrationResponse` | ✅ already in BFF |

### ProfileController (`/api/v1/identity`) [Authorize]

| Method | Route | DTO In | DTO Out | AdminPanel Relevant |
|--------|-------|--------|---------|---------------------|
| PUT | `/identity/participant/profile` | `UpdateParticipantProfileRequest` | `ProfileUpdateResult` | ⚠️ admin can update participant profile |
| PUT | `/identity/organizers/profile` | `UpdateOrganizerProfileRequest` | `ProfileUpdateResult` | ⚠️ admin can update organizer profile |
| PUT | `/identity/venues/profile` | `UpdateVenueProfileRequest` | `ProfileUpdateResult` | ⚠️ admin can update venue profile |

### RegistrationController
Not relevant to AdminPanel (self-service registration endpoints).

---

## ReferenceData Module

### Public Read Controllers

| Controller | Method | Route | AdminPanel Relevant |
|-----------|--------|-------|---------------------|
| LookupController | GET | `/api/v1/lookup-groups` | ✅ already in BFF |
| LookupController | GET | `/api/v1/lookup-groups/tree` | ✅ already in BFF |
| LookupController | GET | `/api/v1/lookup-groups/{groupCode}/items` | ✅ already in BFF |
| CurrencyController | GET | `/api/v1/currencies` | ✅ already in BFF |
| CurrencyController | GET | `/api/v1/currencies/{id}` | ✅ remote call exists |
| ExchangeRateController | GET | `/api/v1/reference-data/exchange-rates` | ❌ missing |
| ExchangeRateController | GET | `/api/v1/reference-data/exchange-rates/by-currency/{code}` | ❌ missing |
| ExchangeRateController | GET | `/api/v1/reference-data/exchange-rates/history` | ❌ missing |
| LocationController | GET | `/api/v1/locations/countries` | ✅ already in BFF |
| LocationController | GET | `/api/v1/locations/cities` | ✅ already in BFF |
| MeasurementController | GET | `/api/v1/measurement-units` | ✅ already in BFF |
| SystemParameterController | GET | `/api/v1/system-parameters` | ✅ already in BFF |

### Admin CRUD Controllers

| Controller | Method | Route | AdminPanel Relevant |
|-----------|--------|-------|---------------------|
| CurrencyAdminController | POST | `/api/v1/admin/reference-data/currencies` | ✅ |
| CurrencyAdminController | PUT | `/api/v1/admin/reference-data/currencies/{id}` | ✅ |
| CurrencyAdminController | PUT | `/api/v1/admin/reference-data/currencies/{id}/set-base` | ✅ |
| CurrencyAdminController | PUT | `/api/v1/admin/reference-data/currencies/{id}/activate` | ✅ |
| CurrencyAdminController | PUT | `/api/v1/admin/reference-data/currencies/{id}/deactivate` | ✅ |
| ExchangeRateAdminController | PUT | `/api/v1/admin/reference-data/exchange-rates` | ✅ |
| ExchangeRateAdminController | POST | `/api/v1/admin/reference-data/exchange-rates/sync` | ✅ |
| ExchangeRateAdminController | POST | `/api/v1/admin/reference-data/exchange-rates/history` | ✅ |
| LookupAdminController | POST | `/api/v1/admin/reference-data/lookup-groups` | ✅ |
| LookupAdminController | PUT | `/api/v1/admin/reference-data/lookup-groups/{id}` | ✅ |
| LookupAdminController | PUT | `/api/v1/admin/reference-data/lookup-groups/move` | ✅ |
| LookupAdminController | PUT | `/api/v1/admin/reference-data/lookup-groups/{id}/activate` | ✅ |
| LookupAdminController | PUT | `/api/v1/admin/reference-data/lookup-groups/{id}/deactivate` | ✅ |
| LookupAdminController | POST | `/api/v1/admin/reference-data/lookup-items` | ✅ |
| LookupAdminController | PUT | `/api/v1/admin/reference-data/lookup-items/{id}` | ✅ |
| LookupAdminController | PUT | `/api/v1/admin/reference-data/lookup-items/{id}/activate` | ✅ |
| LookupAdminController | PUT | `/api/v1/admin/reference-data/lookup-items/{id}/deactivate` | ✅ |
| LocationAdminController | POST | `/api/v1/admin/reference-data/locations/countries` | ✅ |
| LocationAdminController | PUT | `/api/v1/admin/reference-data/locations/{countryCode}` | ✅ |
| LocationAdminController | POST | `/api/v1/admin/reference-data/locations/cities` | ✅ |
| LocationAdminController | PUT | `/api/v1/admin/reference-data/locations/{countryCode}/cities/{cityCode}` | ✅ |
| LocationAdminController | POST | `/api/v1/admin/reference-data/locations/districts` | ✅ |
| LocationAdminController | PUT | `/api/v1/admin/reference-data/locations/{countryCode}/cities/{cityCode}/districts/{districtCode}` | ✅ |
| LocationAdminController | POST | `/api/v1/admin/reference-data/locations/neighborhoods` | ✅ |
| LocationAdminController | PUT | `.../neighborhoods/{neighborhoodCode}` | ✅ |
| LocationAdminController | POST | `/api/v1/admin/reference-data/locations/streets` | ✅ |
| MeasurementAdminController | POST | `/api/v1/admin/reference-data/measurement-units` | ✅ |
| MeasurementAdminController | PUT | `/api/v1/admin/reference-data/measurement-units/{id}` | ✅ |
| MeasurementAdminController | PUT | `/api/v1/admin/reference-data/measurement-units/{id}/activate` | ✅ |
| MeasurementAdminController | PUT | `/api/v1/admin/reference-data/measurement-units/{id}/deactivate` | ✅ |
| SystemParameterAdminController | POST | `/api/v1/admin/reference-data/system-parameters` | ✅ |
| SystemParameterAdminController | PUT | `/api/v1/admin/reference-data/system-parameters/{key}` | ✅ |
| SystemParameterAdminController | PUT | `/api/v1/admin/reference-data/system-parameters/{key}/activate` | ✅ |
| SystemParameterAdminController | PUT | `/api/v1/admin/reference-data/system-parameters/{key}/deactivate` | ✅ |

---

## Vessel Module

### Admin Endpoints

| Method | Route | DTO Out | AdminPanel Relevant |
|--------|-------|---------|---------------------|
| GET | `/api/v1/admin/vessels` | `GetAllVesselsAdminResponse` | ✅ already in BFF |

### Regular Endpoints (admin can use)

| Method | Route | DTO Out | AdminPanel Relevant |
|--------|-------|---------|---------------------|
| GET | `/api/v1/vessels/{vesselId}` | `GetVesselDetailResponse` | ✅ |
| GET | `/api/v1/vessels/code/{vesselCode}` | `GetVesselByCodeResponse` | ⚠️ optional |
| PUT | `/api/v1/vessels/{vesselId}` | `UpdateVesselResponse` | ✅ |
| PATCH | `/api/v1/vessels/{vesselId}/archive` | `ArchiveVesselResponse` | ✅ already in BFF |
| PATCH | `/api/v1/vessels/{vesselId}/restore` | `RestoreVesselResponse` | ✅ already in BFF |
| PATCH | `/api/v1/vessels/{vesselId}/status` | `UpdateVesselStatusResponse` | ✅ already in BFF |
| GET | `/api/v1/vessels/{vesselId}/owners` | `GetVesselOwnersResponse` | ✅ (in remote call, not in controller) |
| GET | `/api/v1/vessels/{vesselId}/documents` | `GetVesselDocumentsResponse` | ✅ already in BFF |
| DELETE | `/api/v1/vessels/{vesselId}/documents/{documentId}` | — | ✅ already in BFF |
| GET | `/api/v1/vessels/{vesselId}/spec` | `GetVesselSpecificationResponse` | ❌ missing |
| GET | `/api/v1/vessels/{vesselId}/engines` | `GetVesselEnginesResponse` | ❌ missing |
| GET | `/api/v1/vessels/{vesselId}/location` | `GetVesselLocationResponse` | ❌ missing |
| GET | `/api/v1/vessels/{vesselId}/status-history` | `GetVesselStatusHistoryResponse` | ❌ missing |
| GET | `/api/v1/vessels/{vesselId}/media` | `GetVesselMediaResponse` | ❌ missing |

---

## FileStorage Module

| Method | Route | DTO Out | AdminPanel Relevant |
|--------|-------|---------|---------------------|
| GET | `/api/v1/files/{fileId:long}` | `FileDto` | ✅ — BFF incorrectly uses Guid |
| GET | `/api/v1/files/{fileId:long}/metadata` | `FileMetadataDto` | ✅ — BFF incorrectly uses Guid |
| DELETE | `/api/v1/files/{fileId:long}` | — | ✅ already in BFF (Guid mismatch) |
| PUT | `/api/v1/files/{fileId:long}/visibility` | `FileDto` | ✅ |
| POST | `/api/v1/files/{fileId:long}/owners` | `FileOwnerReferenceDto` | ⚠️ optional |
| POST | `/api/v1/upload-sessions` | `FileUploadSessionDto` | ✅ |
| POST | `/api/v1/upload-sessions/{code}/complete` | `FileDto` | ✅ |

---

## ServiceRequest Module

### Admin Endpoints

| Method | Route | DTO Out | AdminPanel Relevant |
|--------|-------|---------|---------------------|
| GET | `/api/v1/admin/service-requests` | `GetAdminServiceRequestListResponse` | ✅ already in BFF |
| GET | `/api/v1/admin/service-requests/disputes` | `GetAdminDisputeListResponse` | ✅ already in BFF |
| GET | `/api/v1/admin/service-requests/{id}` | `GetServiceRequestDetailResponse` | ✅ already in BFF |

### Regular Endpoints

| Method | Route | DTO Out | AdminPanel Relevant |
|--------|-------|---------|---------------------|
| PATCH | `/api/v1/service-requests/{id}/cancel` | `CancelServiceRequestResponse` | ✅ already in BFF |
| PATCH | `/api/v1/service-requests/{id}/completion/approve` | `ApproveServiceRequestCompletionResponse` | ✅ already in BFF |
| PATCH | `/api/v1/service-requests/{id}/completion/reject` | `RejectServiceRequestCompletionResponse` | ✅ already in BFF |
| PATCH | `/api/v1/service-requests/{id}/dispute/{disputeId}/status` | — | ✅ already in BFF |
| PATCH | `/api/v1/service-requests/{id}/dispute/{disputeId}/resolve` | `ResolveServiceRequestDisputeResponse` | ✅ already in BFF |
| GET | `/api/v1/service-requests/{id}/offers` | `GetServiceRequestOffersResponse` | ❌ missing |
| GET | `/api/v1/service-requests/{id}/work-logs` | `GetServiceRequestWorkLogsResponse` | ❌ missing |
| GET | `/api/v1/service-requests/{id}/messages` | `GetServiceRequestMessagesResponse` | ❌ missing |
| GET | `/api/v1/service-requests/{id}/assignment` | `GetServiceRequestAssignmentResponse` | ❌ missing |
| GET | `/api/v1/service-requests/{id}/completion` | `GetServiceRequestCompletionResponse` | ❌ missing |
