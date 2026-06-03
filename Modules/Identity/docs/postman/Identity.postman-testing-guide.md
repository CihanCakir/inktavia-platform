# Identity Module — Postman Testing Guide

## Prerequisites

### Environment Variables Required
| Variable | Value | Notes |
|---|---|---|
| keycloak_base_url | http://localhost:8080 | Keycloak must be running |
| keycloak_realm | inktavia | |
| keycloak_token_url | {{keycloak_base_url}}/realms/{{keycloak_realm}}/protocol/openid-connect/token | |
| identity_api_base_url | http://localhost:7101/api/v1 | Identity service must be running |
| mobile_client_id | inktavia-mobile | |
| mobile_username | test_mobile_user | |
| default_password | Password123! | |
| admin_panel_client_id | admin-panel | |
| admin_username | test_admin_user | |

### Services Required
- Keycloak (port 8080)
- Identity API (port 7101)
- PostgreSQL or configured database
- RabbitMQ (for async operations)

---

## Auth Setup

### Step 1: Get Keycloak Token
Run `00 - Auth Setup > Get Mobile Token` or `Get Admin Token`.

This will set `active_access_token` which is used in the `Authorization: Bearer {{active_access_token}}` header.

### Step 2: Identity Login (Optional)
For endpoints requiring `X-Aizen-User-Token`, run:
`00 - Auth Setup > Identity Login - Phone`

This sets:
- `identityAccessToken` — Identity module JWT
- `X-Aizen-User-Token` — User context header
- `identityRefreshToken` — For refresh

### Step 3: Switch Context
- **Mobile user context:** Run `Set Active - Mobile`
- **Admin context:** Run `Set Active - Admin` (then run `Get Admin Token` first)

---

## Endpoint Test Sequence

### Registration Flow
1. Run `02 - Registration > Register Participant` → sets `participantProfileId`
2. (Optional) Run `02 - Registration > Register Organizer` → sets `organizerProfileId`
3. (Optional) Run `02 - Registration > Register Venue` → sets `venueProfileId`

### Authentication Flow
1. Run `01 - Auth - Authorization > Send OTP` → sets `otpValidationGuid`
2. (Optional) Run `01 - Auth - Authorization > Check OTP`
3. Run `01 - Auth - Authorization > Login with OTP` → sets `identityAccessToken`, `X-Aizen-User-Token`
4. OR Run `01 - Auth - Authorization > Login with Phone` (simpler)

### Profile Query Flow
1. Run `05 - Query - Profiles > Get My Profile`
2. Run `05 - Query - Profiles > Get My Participant Profile`

### Admin Operations Flow
1. First run `Set Active - Admin`
2. Run `04 - Admin - Profile Management > Approve Organizer Profile` (set `profileUserId` and `organizerProfileId` first)

---

## Expected Responses

### Login with Phone - Success
```json
HTTP 200 OK
{
  "identityAccessToken": "eyJ...",
  "refreshToken": "eyJ...",
  "expiresIn": 3600,
  "xAizenUserToken": "..."
}
```

### Register Participant - Success
```json
HTTP 200 OK or 201 Created
{
  "profileId": "3fa85f64-...",
  "userId": "...",
  "status": "PendingVerification"
}
```

### Get My Profile - Success
```json
HTTP 200 OK
{
  "id": "3fa85f64-...",
  "email": "test@test.com",
  "firstName": "Test",
  "lastName": "User",
  "roles": ["Participant"],
  "approvalStatus": "Approved"
}
```

---

## Common Errors

| Error | Cause | Fix |
|---|---|---|
| 401 Unauthorized | Missing or expired Keycloak token | Re-run `Get Mobile/Admin Token` |
| 403 Forbidden | Insufficient role | Use admin token for admin endpoints |
| 400 Bad Request | Invalid request body | Check required fields, phone format (+90...) |
| 409 Conflict | Email/phone already registered | Use different email/phone |
| 422 Unprocessable | Password doesn't meet policy | Use Password123! format |

---

## Phone Number Format
All phone numbers must include country code: `+905XXXXXXXXX` (Turkey example)

## OTP Testing
In development, use OTP bypass value `000000` if configured, or check SMS logs.
