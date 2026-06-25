# Auth Token Contract — Inktavia Marine OS

## Overview

Inktavia Marine OS uses a **two-layer authentication model**:
1. **Keycloak Token** — machine-level auth for service-to-service and API gateway access
2. **Identity Module Token** — user-level auth for participant/organizer/venue context

---

## Layer 1: Keycloak Token Flow

### Endpoint
```
POST {{keycloak_token_url}}
```
Where `keycloak_token_url = {{keycloak_base_url}}/realms/{{keycloak_realm}}/protocol/openid-connect/token`

### Request (form-urlencoded)
```
grant_type=password
client_id={{mobile_client_id}}
username={{mobile_username}}
password={{default_password}}
```

### Response
```json
{
  "access_token": "eyJhbGc...",
  "refresh_token": "eyJhbGc...",
  "expires_in": 300,
  "token_type": "Bearer"
}
```

### Postman Test Script
```javascript
const response = pm.response.json();
pm.environment.set("mobile_access_token", response.access_token);
pm.environment.set("active_access_token", response.access_token);
```

### Token Variables

| Variable | Client | Stored By |
|---|---|---|
| mobile_access_token | inktavia-mobile | Get Mobile Token request |
| customer_access_token | customer-panel | Get Customer Token request |
| admin_access_token | admin-panel | Get Admin Token request |
| active_access_token | any | Set Active Token requests |

### Usage in API Requests
All API requests use:
```
Authorization: Bearer {{active_access_token}}
```

### Switch Active Token
Send the "Set Active Token - Mobile/Customer/Admin" requests which run:
```javascript
pm.environment.set("active_access_token", pm.environment.get("mobile_access_token"));
```

---

## Layer 2: Identity Module Token Flow

### Login with Phone+Password
```
POST {{identity_api_base_url}}/auth/login/phone
Authorization: Bearer {{active_access_token}}
Content-Type: application/json
```

Request:
```json
{
  "phoneNumber": "+905XXXXXXXXX",
  "password": "Password123!",
  "deviceId": "test-device-001",
  "notificationToken": "fcm-token-placeholder"
}
```

### Login with OTP
Step 1 — Send OTP:
```
POST {{identity_api_base_url}}/auth/otp/send
Body: { "phoneNumber": "+905XXXXXXXXX" }
```

Step 2 — Check OTP (get ValidationGuid):
```
POST {{identity_api_base_url}}/auth/otp/check
Body: { "phoneNumber": "+905XXXXXXXXX", "otp": "123456", "validationGuid": "..." }
```

Step 3 — Login:
```
POST {{identity_api_base_url}}/auth/login/otp
Body: { "phoneNumber": "+905XXXXXXXXX", "otp": "123456", "validationGuid": "...", "deviceId": "test-device-001", "notificationToken": "..." }
```

### Login with Username+PIN
```
POST {{identity_api_base_url}}/auth/login/username
Body: { "username": "admin_user", "pin": "1234", "deviceId": "test-device-001", "notificationToken": "..." }
```

### Identity Login Response
```json
{
  "identityAccessToken": "eyJhbGc...",
  "refreshToken": "eyJhbGc...",
  "expiresIn": 3600,
  "xAizenUserToken": "user-context-token-string"
}
```

### Identity Login Postman Test Script
```javascript
const response = pm.response.json();
pm.environment.set("identityAccessToken", response.identityAccessToken);
pm.environment.set("X-Aizen-User-Token", response.xAizenUserToken);
pm.environment.set("identityRefreshToken", response.refreshToken);
```

---

## Combined Usage Pattern

For user-context API calls:
```
Authorization: Bearer {{active_access_token}}
X-Aizen-User-Token: {{X-Aizen-User-Token}}
Content-Type: application/json
```

### Which Headers Are Required?

| Endpoint Type | Authorization | X-Aizen-User-Token |
|---|---|---|
| Public endpoints (no auth) | Not required | Not required |
| Keycloak-secured (machine auth) | Bearer {{active_access_token}} | Not required |
| User-context endpoints | Bearer {{active_access_token}} | Required |
| Admin endpoints | Bearer {{admin_access_token}} as active | Not required |

---

## Token Refresh

### Keycloak Refresh
Re-run the "Get [Role] Token" request from `00 - Keycloak Auth` folder.

### Identity Refresh
```
POST {{identity_api_base_url}}/auth/refresh
Body: {
  "refreshToken": "{{identityRefreshToken}}",
  "deviceId": "test-device-001",
  "accessToken": "{{identityAccessToken}}"
}
```

---

## Auth Setup Order

1. Run `Get Mobile Token` OR `Get Admin Token` from `00 - Keycloak Auth`
2. (Optional) Run `Identity Login - Phone` to get user-level tokens
3. Use `Set Active Token - [Role]` to switch context
4. All subsequent API requests use `Authorization: Bearer {{active_access_token}}`
