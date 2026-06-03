# Inktavia Marine OS — Active Module Endpoint Inventory

## Overview

| Module | Port | Base URL | Auth Required | Endpoint Count |
|---|---|---|---|---|
| Identity | 7101 | /api/v1 | Mixed (Keycloak + Identity) | ~35 |
| ReferenceData | 7104 | /api/v1/reference-data | None (read), Admin (write) | ~38 |
| Vessel | 7105 | /api/v1/vessels | Keycloak Bearer | ~35 |
| FileStorage | 7106 | /api/v1 | Keycloak Bearer | ~12 |
| ServiceRequest | 7107 | /api/v1/service-requests | Keycloak Bearer | ~25 |

## Identity Module (port 7101)

### AuthorizationController [/api/v1/auth]

| Method | Route | Auth | Request DTO | Response Notes |
|---|---|---|---|---|
| POST | /api/v1/auth/refresh | None | RefreshLoginHttpRequest { RefreshToken, DeviceId, AccessToken } | Returns new tokens |
| POST | /api/v1/auth/otp/send | None | SendOtpRequest { PhoneNumber } | Returns ValidationGuid |
| POST | /api/v1/auth/otp/check | None | CheckOtpRequest { PhoneNumber, Otp, ValidationGuid } | Returns OTP check result |
| POST | /api/v1/auth/login/otp | None | LoginWithOtpRequest { PhoneNumber, Otp, ValidationGuid, DeviceId, NotificationToken } | Returns identityAccessToken, X-Aizen-User-Token |
| POST | /api/v1/auth/login/phone | None | LoginWithPhoneRequest { PhoneNumber, Password, DeviceId, NotificationToken } | Returns identityAccessToken, X-Aizen-User-Token |
| POST | /api/v1/auth/login/username | None | LoginWithUsernameRequest { Username, Pin, DeviceId, NotificationToken } | Returns identityAccessToken, X-Aizen-User-Token |
| POST | /api/v1/auth/password/change | Bearer | ChangePasswordRequest { OldPassword, NewPassword, NewPasswordConfirm } | 200 OK |

### RegistrationController [/api/v1/identity]

| Method | Route | Auth | Request DTO | Response Notes |
|---|---|---|---|---|
| GET | /api/v1/identity/participant/oauth/start | None | Query: provider, redirect, lang | Redirect URL |
| POST | /api/v1/identity/participant/oauth/callback/{provider} | None | Form: code, state; Headers: X-Device-Id, X-Notification-Token | Returns tokens |
| POST | /api/v1/identity/participant/register | None | RegisterConsumerRequest { Email, Phone, Password, FirstName, LastName, KvkkAccepted, DeviceId, DeviceType, NotificationToken } | Returns ProfileId |
| POST | /api/v1/identity/organizers/register | None | RegisterOrganizerRequest { Email, CompanyName, TaxNo, Phone, Password, OwnerFirstName, OwnerLastName, KvkkAccepted, DeviceId, DeviceType, NotificationToken } | Returns ProfileId |
| POST | /api/v1/identity/venues/register | None | RegisterVenueRequest { VenueName, Address, Email, Phone, Password, OwnerFirstName, OwnerLastName, KvkkAccepted, DeviceId, DeviceType, NotificationToken } | Returns ProfileId |

### ProfileController [/api/v1/identity] [Authorize]

| Method | Route | Auth | Request DTO | Response Notes |
|---|---|---|---|---|
| PUT | /api/v1/identity/participant/profile | Bearer | UpdateParticipantProfileRequest { FirstName, LastName, Gender, BirthDate, Bio, ProfilePhotoUrl, NationalityId, AllowPush, AllowSms, AllowEmail } | 200 OK |
| PUT | /api/v1/identity/organizers/profile | Bearer | UpdateOrganizerProfileRequest { OwnerFirstName, OwnerLastName, Bio, ProfilePhotoUrl, NationalityId, TaxpayerType, AllowPush, AllowSms, AllowEmail } | 200 OK |
| PUT | /api/v1/identity/venues/profile | Bearer | UpdateVenueProfileRequest | 200 OK |

### AdminController [/api/v1/identity] [Admin]

| Method | Route | Auth | Request DTO | Response Notes |
|---|---|---|---|---|
| POST | /api/v1/identity/admin/organizers/{userId}/profiles/{profileId}/approve | Admin | — | 200 OK |
| POST | /api/v1/identity/admin/organizers/{userId}/profiles/{profileId}/reject | Admin | RejectProfileRequest { Reason } | 200 OK |
| POST | /api/v1/identity/admin/venues/{userId}/profiles/{profileId}/approve | Admin | — | 200 OK |
| POST | /api/v1/identity/admin/venues/{userId}/profiles/{profileId}/reject | Admin | RejectProfileRequest { Reason } | 200 OK |

### QueryController [/api/v1/identity]

| Method | Route | Auth | Params | Response Notes |
|---|---|---|---|---|
| GET | /api/v1/identity/profile/me | Bearer | — | ProfileDto |
| GET | /api/v1/identity/profiles/{profileId} | Admin | — | ProfileDto |
| GET | /api/v1/identity/profiles/{profileId}/with-roles | Admin | — | ProfileWithRolesDto |
| GET | /api/v1/identity/profiles | Admin | firstName, lastName, roleContext, approvalStatus, pageIndex, pageSize | PagedResult<ProfileDto> |
| GET | /api/v1/identity/participant/profile/me | Bearer | — | ParticipantProfileDto |
| GET | /api/v1/identity/participant/profiles/{profileId} | Admin | — | ParticipantProfileDto |
| GET | /api/v1/identity/venues/profile/me | Bearer | — | VenueProfileDto |
| GET | /api/v1/identity/organizers/profile/me | Bearer | — | OrganizerProfileDto |

## ReferenceData Module (port 7104)

### LookupController [/api/v1/reference-data/lookup-groups]

| Method | Route | Auth | Params | Response Notes |
|---|---|---|---|---|
| GET | /api/v1/reference-data/lookup-groups | None | onlyActive | List<LookupGroupDto> |
| GET | /api/v1/reference-data/lookup-groups/tree | None | onlyActive | Tree structure |
| GET | /api/v1/reference-data/lookup-groups/{id} | None | — | LookupGroupDto |
| GET | /api/v1/reference-data/lookup-groups/lookup-items/{groupCode} | None | onlyActive | List<LookupItemDto> |

### CurrencyController [/api/v1/reference-data/currencies]

| Method | Route | Auth | Params | Response Notes |
|---|---|---|---|---|
| GET | /api/v1/reference-data/currencies | None | onlyActive | List<CurrencyDto> |
| GET | /api/v1/reference-data/currencies/base | None | — | CurrencyDto |
| GET | /api/v1/reference-data/currencies/{id} | None | — | CurrencyDto |

### LocationController [/api/v1/reference-data/locations]

| Method | Route | Auth | Params | Response Notes |
|---|---|---|---|---|
| GET | /api/v1/reference-data/locations/countries | None | onlyActive | List<CountryDto> |
| GET | /api/v1/reference-data/locations/countries/{countryCode} | None | — | CountryDto |
| GET | /api/v1/reference-data/locations/{countryCode}/cities | None | onlyActive | List<CityDto> |
| GET | /api/v1/reference-data/locations/{countryCode}/cities/{cityCode} | None | — | CityDto |
| GET | /api/v1/reference-data/locations/{countryCode}/cities/{cityCode}/districts | None | onlyActive | List<DistrictDto> |
| GET | /api/v1/reference-data/locations/{countryCode}/cities/{cityCode}/districts/{districtCode}/neighborhoods | None | onlyActive | List<NeighborhoodDto> |

## Vessel Module (port 7105)

### VesselController [/api/v1/vessels]

| Method | Route | Auth | Request DTO | Response Notes |
|---|---|---|---|---|
| POST | /api/v1/vessels | Bearer | CreateVesselRequest { Name, VesselType, Flag, Imo, Mmsi, CallSign, BuildYear } | Returns VesselId |
| PUT | /api/v1/vessels/{vesselId} | Bearer | UpdateVesselRequest { Name, Flag, CallSign } | 200 OK |
| GET | /api/v1/vessels/{vesselId} | None | — | VesselDto |
| GET | /api/v1/vessels/code/{vesselCode} | None | — | VesselDto |
| GET | /api/v1/vessels/current-user | Bearer | pageIndex, pageSize | PagedResult<VesselDto> |
| PATCH | /api/v1/vessels/{vesselId}/archive | Bearer | ArchiveVesselRequest { Reason } | 200 OK |
| PATCH | /api/v1/vessels/{vesselId}/restore | Bearer | — | 200 OK |
| PATCH | /api/v1/vessels/{vesselId}/status | Bearer | UpdateVesselStatusRequest { Status } | 200 OK |
| PATCH | /api/v1/vessels/{vesselId}/visibility | Bearer | VesselVisibility (enum) | 200 OK |

## FileStorage Module (port 7106)

### UploadSessionController [/api/v1/upload-sessions]

| Method | Route | Auth | Request DTO | Response Notes |
|---|---|---|---|---|
| POST | /api/v1/upload-sessions | Bearer | CreateUploadSessionRequest { FileName, ContentType, FileSizeBytes, BucketContext, OwnerEntityType?, OwnerEntityId? } | Returns uploadSessionCode, presignedUrl |
| POST | /api/v1/upload-sessions/{uploadSessionCode}/complete | Bearer | CompleteUploadSessionRequest { Checksum, ActualSizeBytes } | Returns FileId |

### FileController [/api/v1/files]

| Method | Route | Auth | Request DTO | Response Notes |
|---|---|---|---|---|
| GET | /api/v1/files/{fileId} | Bearer | — | FileDto |
| GET | /api/v1/files/{fileId}/metadata | Bearer | — | FileMetadataDto |
| DELETE | /api/v1/files/{fileId} | Bearer | DeleteFileRequest { Reason } | 200 OK |
| PUT | /api/v1/files/{fileId}/visibility | Bearer | FileVisibility (Public/Private) | 200 OK |
| POST | /api/v1/files/{fileId}/owners | Bearer | LinkFileToOwnerRequest { OwnerEntityType, OwnerEntityId } | 200 OK |

## ServiceRequest Module (port 7107)

### ServiceRequestController [/api/v1/service-requests]

| Method | Route | Auth | Request DTO | Response Notes |
|---|---|---|---|---|
| POST | /api/v1/service-requests | Bearer | CreateServiceRequestRequest { VesselId, Title, Description, ServiceType, RequiredPort, ScheduledAt, BudgetMin?, BudgetMax?, CurrencyId, Attachments? } | Returns serviceRequestId |
| PUT | /api/v1/service-requests/{id} | Bearer | UpdateServiceRequestRequest | 200 OK |
| GET | /api/v1/service-requests/{id} | Bearer | — | ServiceRequestDto |
| GET | /api/v1/service-requests/my | Bearer | status, pageIndex, pageSize | PagedResult<ServiceRequestDto> |
| PATCH | /api/v1/service-requests/{id}/cancel | Bearer | CancelServiceRequestRequest { Reason } | 200 OK |
| PATCH | /api/v1/service-requests/{id}/publish | Bearer | — | 200 OK |
