# Identity Module — Endpoint Inventory

**Module:** Identity  
**Port:** 7101  
**Base URL:** `{{identity_api_base_url}}` = `http://localhost:7101/api/v1`  

---

## AuthorizationController

**Route prefix:** `/api/v1/auth`  
**Tag:** Auth  

| # | Action | Method | Route | Auth | Request Body | Sample Request | Sample Response |
|---|---|---|---|---|---|---|---|
| 1 | Refresh Token | POST | /api/v1/auth/refresh | None | RefreshLoginHttpRequest | See below | New token pair |
| 2 | Send OTP | POST | /api/v1/auth/otp/send | None | SendOtpRequest | See below | ValidationGuid |
| 3 | Check OTP | POST | /api/v1/auth/otp/check | None | CheckOtpRequest | See below | OTP validation result |
| 4 | Login with OTP | POST | /api/v1/auth/login/otp | None | LoginWithOtpRequest | See below | identityAccessToken |
| 5 | Login with Phone | POST | /api/v1/auth/login/phone | None | LoginWithPhoneRequest | See below | identityAccessToken |
| 6 | Login with Username | POST | /api/v1/auth/login/username | None | LoginWithUsernameRequest | See below | identityAccessToken |
| 7 | Change Password | POST | /api/v1/auth/password/change | Bearer | ChangePasswordRequest | See below | 200 OK |

### Sample Requests

**Refresh Token**
```json
{
  "refreshToken": "{{identityRefreshToken}}",
  "deviceId": "test-device-001",
  "accessToken": "{{identityAccessToken}}"
}
```

**Send OTP**
```json
{
  "phoneNumber": "+905301234567"
}
```

**Check OTP**
```json
{
  "phoneNumber": "+905301234567",
  "otp": "123456",
  "validationGuid": "{{otpValidationGuid}}"
}
```

**Login with OTP**
```json
{
  "phoneNumber": "+905301234567",
  "otp": "123456",
  "validationGuid": "{{otpValidationGuid}}",
  "deviceId": "test-device-001",
  "notificationToken": "fcm-test-token"
}
```

**Login with Phone**
```json
{
  "phoneNumber": "+905301234567",
  "password": "Password123!",
  "deviceId": "test-device-001",
  "notificationToken": "fcm-test-token"
}
```

**Login with Username**
```json
{
  "username": "admin_user",
  "pin": "1234",
  "deviceId": "test-device-001",
  "notificationToken": "fcm-test-token"
}
```

**Change Password**
```json
{
  "oldPassword": "Password123!",
  "newPassword": "NewPassword456!",
  "newPasswordConfirm": "NewPassword456!"
}
```

### Sample Responses

**Login Response (phone/otp/username)**
```json
{
  "identityAccessToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4...",
  "expiresIn": 3600,
  "xAizenUserToken": "aizen-user-ctx-token-string"
}
```

---

## RegistrationController

**Route prefix:** `/api/v1/identity`  
**Tag:** Identity - Registration  

| # | Action | Method | Route | Auth | Request Body | Notes |
|---|---|---|---|---|---|---|
| 1 | OAuth Start | GET | /api/v1/identity/participant/oauth/start | None | Query: provider, redirect, lang | Returns redirect URL |
| 2 | OAuth Callback | POST | /api/v1/identity/participant/oauth/callback/{provider} | None | Form: code, state; Headers: X-Device-Id, X-Notification-Token | Returns tokens |
| 3 | Register Participant | POST | /api/v1/identity/participant/register | None | RegisterConsumerRequest | Returns ProfileId |
| 4 | Register Organizer | POST | /api/v1/identity/organizers/register | None | RegisterOrganizerRequest | Returns ProfileId |
| 5 | Register Venue | POST | /api/v1/identity/venues/register | None | RegisterVenueRequest | Returns ProfileId |

### Sample Requests

**Register Participant**
```json
{
  "email": "participant@test.com",
  "phone": "+905301234567",
  "password": "Password123!",
  "firstName": "Test",
  "lastName": "User",
  "kvkkAccepted": true,
  "deviceId": "test-device-001",
  "deviceType": "Android",
  "notificationToken": "fcm-test-token"
}
```

**Register Organizer**
```json
{
  "email": "organizer@company.com",
  "companyName": "Test Company Ltd.",
  "taxNo": "1234567890",
  "phone": "+905301234568",
  "password": "Password123!",
  "ownerFirstName": "John",
  "ownerLastName": "Doe",
  "kvkkAccepted": true,
  "deviceId": "test-device-002",
  "deviceType": "iOS",
  "notificationToken": "apns-test-token"
}
```

**Register Venue**
```json
{
  "venueName": "Harbor Test Marina",
  "address": "Test Port District, Istanbul",
  "email": "venue@harbor.com",
  "phone": "+905301234569",
  "password": "Password123!",
  "ownerFirstName": "Jane",
  "ownerLastName": "Smith",
  "kvkkAccepted": true,
  "deviceId": "test-device-003",
  "deviceType": "Android",
  "notificationToken": "fcm-test-token-2"
}
```

---

## ProfileController

**Route prefix:** `/api/v1/identity`  
**Tag:** Identity - Profile  
**Auth:** Bearer (Keycloak + X-Aizen-User-Token)

| # | Action | Method | Route | Auth | Request Body | Notes |
|---|---|---|---|---|---|---|
| 1 | Update Participant Profile | PUT | /api/v1/identity/participant/profile | Bearer | UpdateParticipantProfileRequest | 200 OK |
| 2 | Update Organizer Profile | PUT | /api/v1/identity/organizers/profile | Bearer | UpdateOrganizerProfileRequest | 200 OK |
| 3 | Update Venue Profile | PUT | /api/v1/identity/venues/profile | Bearer | UpdateVenueProfileRequest | 200 OK |

### Sample Requests

**Update Participant Profile**
```json
{
  "firstName": "Test",
  "lastName": "Updated",
  "gender": "Male",
  "birthDate": "1990-01-15T00:00:00Z",
  "bio": "Test participant bio",
  "profilePhotoUrl": "https://example.com/photo.jpg",
  "nationalityId": "{{lookupItemId}}",
  "allowPush": true,
  "allowSms": false,
  "allowEmail": true
}
```

**Update Organizer Profile**
```json
{
  "ownerFirstName": "John",
  "ownerLastName": "Updated",
  "bio": "Marine services organizer",
  "profilePhotoUrl": "https://example.com/org-photo.jpg",
  "nationalityId": "{{lookupItemId}}",
  "taxpayerType": "Individual",
  "allowPush": true,
  "allowSms": true,
  "allowEmail": true
}
```

---

## AdminController

**Route prefix:** `/api/v1/identity`  
**Tag:** Identity - Admin  
**Auth:** Bearer (Admin role)

| # | Action | Method | Route | Auth | Request Body | Notes |
|---|---|---|---|---|---|---|
| 1 | Approve Organizer Profile | POST | /api/v1/identity/admin/organizers/{userId}/profiles/{profileId}/approve | Admin | — | 200 OK |
| 2 | Reject Organizer Profile | POST | /api/v1/identity/admin/organizers/{userId}/profiles/{profileId}/reject | Admin | RejectProfileRequest | 200 OK |
| 3 | Approve Venue Profile | POST | /api/v1/identity/admin/venues/{userId}/profiles/{profileId}/approve | Admin | — | 200 OK |
| 4 | Reject Venue Profile | POST | /api/v1/identity/admin/venues/{userId}/profiles/{profileId}/reject | Admin | RejectProfileRequest | 200 OK |

**RejectProfileRequest:**
```json
{
  "reason": "Insufficient documentation provided"
}
```

---

## QueryController

**Route prefix:** `/api/v1/identity`  
**Tag:** Identity - Query

| # | Action | Method | Route | Auth | Query Params | Response |
|---|---|---|---|---|---|---|
| 1 | Get My Profile | GET | /api/v1/identity/profile/me | Bearer | — | ProfileDto |
| 2 | Get Profile By ID | GET | /api/v1/identity/profiles/{profileId} | Admin | — | ProfileDto |
| 3 | Get Profile With Roles | GET | /api/v1/identity/profiles/{profileId}/with-roles | Admin | — | ProfileWithRolesDto |
| 4 | Search Profiles | GET | /api/v1/identity/profiles | Admin | firstName, lastName, roleContext, approvalStatus, pageIndex, pageSize | PagedResult |
| 5 | List Profiles | GET | /api/v1/identity/profiles/list | Admin | firstName, lastName, roleContext, approvalStatus | List |
| 6 | Get My Participant Profile | GET | /api/v1/identity/participant/profile/me | Bearer | — | ParticipantProfileDto |
| 7 | Get Participant Profile By ID | GET | /api/v1/identity/participant/profiles/{profileId} | Admin | — | ParticipantProfileDto |
| 8 | Get Participant Profile With User | GET | /api/v1/identity/participant/profiles/{profileId}/with-user | Admin | — | ParticipantProfileWithUserDto |
| 9 | Search Participant Profiles | GET | /api/v1/identity/participant/profiles | Admin | firstName, lastName, approvalStatus, pageIndex, pageSize | PagedResult |
| 10 | List Participant Profiles | GET | /api/v1/identity/participant/profiles/list | Admin | — | List |
| 11 | Get My Venue Profile | GET | /api/v1/identity/venues/profile/me | Bearer | — | VenueProfileDto |
| 12 | Get Venue Profile By ID | GET | /api/v1/identity/venues/profiles/{profileId} | Admin | — | VenueProfileDto |
| 13 | Get Venue Profile With User | GET | /api/v1/identity/venues/profiles/{profileId}/with-user | Admin | — | VenueProfileWithUserDto |
| 14 | Search Venue Profiles | GET | /api/v1/identity/venues/profiles | Admin | pageIndex, pageSize | PagedResult |
| 15 | List Venue Profiles | GET | /api/v1/identity/venues/profiles/list | Admin | — | List |
| 16 | Get My Organizer Profile | GET | /api/v1/identity/organizers/profile/me | Bearer | — | OrganizerProfileDto |
| 17 | Get Organizer Profile By ID | GET | /api/v1/identity/organizers/profiles/{profileId} | Admin | — | OrganizerProfileDto |
| 18 | Get Organizer Profile With User | GET | /api/v1/identity/organizers/profiles/{profileId}/with-user | Admin | — | OrganizerProfileWithUserDto |
| 19 | Search Organizer Profiles | GET | /api/v1/identity/organizers/profiles | Admin | pageIndex, pageSize | PagedResult |
| 20 | List Organizer Profiles | GET | /api/v1/identity/organizers/profiles/list | Admin | — | List |
