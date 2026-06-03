# Existing Postman Export Analysis

## 1. Existing Collection: inktavia-keycloak-api-tests.postman_collection.json

### Auth Scripts Found
- Folder: `00 - Keycloak Auth`
- Requests:
  - `Get Mobile Token - inktavia-mobile` → POST {{keycloak_token_url}} grant_type=password, stores `mobile_access_token` and `active_access_token`
  - `Get Customer Token - customer-panel` → stores `customer_access_token` and `active_access_token`
  - `Get Admin Token - admin-panel` → stores `admin_access_token` and `active_access_token`
  - `Set Active Token - Mobile` → sets `active_access_token = {{mobile_access_token}}`
  - `Set Active Token - Customer` → sets `active_access_token = {{customer_access_token}}`
  - `Set Active Token - Admin` → sets `active_access_token = {{admin_access_token}}`

### Token Variable Names
| Variable | Purpose |
|---|---|
| mobile_access_token | Mobile Keycloak access token |
| customer_access_token | Customer panel Keycloak access token |
| admin_access_token | Admin panel Keycloak access token |
| active_access_token | Currently active token used in Authorization header |

### Header Conventions
- Authorization: `Bearer {{active_access_token}}`
- Content-Type: `application/json`

### Base URL Variables
| Variable | Value |
|---|---|
| keycloak_base_url | http://localhost:8080 |
| keycloak_realm | inktavia |
| keycloak_token_url | {{keycloak_base_url}}/realms/{{keycloak_realm}}/protocol/openid-connect/token |
| identity_api_base_url | http://localhost:7101/api/v1 |
| profile_api_base_url | http://localhost:7102/api/v1 |
| payment_api_base_url | http://localhost:7103/api/v1 |
| active_api_base_url | http://localhost:7101/api/v1 |

### Existing Folders
- `01 - Identity API`
- `02 - Profile API`
- `03 - Payment API`
- `04 - Auto-Discovered Endpoints`
- `90 - Negative Authorization Tests`
- `99 - Diagnostics`

## 2. Existing Environment: inktavia-local.postman_environment.json

### Key Variables Present
List all variables, their current values, and type (default/secret).

## 3. What Is Missing

### Missing URL Variables
- reference_data_api_root_url (port 7104)
- vessel_api_root_url (port 7105)
- file_storage_api_root_url (port 7106)
- service_request_api_root_url (port 7107)
- reference_data_api_base_url
- vessel_api_base_url
- file_storage_api_base_url
- service_request_api_base_url

### Missing Auth Variables
- identityAccessToken — Identity module JWT (different from Keycloak)
- X-Aizen-User-Token — Identity module user context header
- identityRefreshToken — Identity module refresh token

### Missing Identity Login Requests
- POST /api/v1/auth/login/phone (stores identityAccessToken + X-Aizen-User-Token)
- POST /api/v1/auth/login/otp (stores identityAccessToken + X-Aizen-User-Token)
- POST /api/v1/auth/login/username (stores identityAccessToken + X-Aizen-User-Token)

### Missing Scenario ID Variables
- vesselId, fileId, serviceRequestId, serviceRequestOfferId
- assignmentId, messageId, workLogId, completionId, disputeId
- currencyId, lookupGroupId, lookupItemId

### Missing Module Collections
- ReferenceData (port 7104) — not present
- Vessel (port 7105) — not present
- FileStorage (port 7106) — not present
- ServiceRequest (port 7107) — not present

## 4. Recommendations
List all recommended changes to align existing collection with new architecture.
