# AdminPanel BFF — Postman Auth Model Report

**Generated:** 2026-06-11  
**Scope:** Auth model design and application in Postman collection  
**Repository:** Inktavia Marine OS (CihanCakir/inktavia-platform)

---

## Final Auth Model Summary

AdminPanel BFF uses the **Identity-only browser auth model** for all browser-to-BFF interactions.

---

## Request Flow

```
Browser / Postman
       │
       │  X-Aizen-User-Token: Bearer <identityAccessToken>
       ▼
 AdminPanel BFF  ←─ Reads user token from header
       │
       │  Authorization: Bearer <keycloak-service-token>   (BFF acquires server-side)
       │  X-Aizen-User-Token: Bearer <identityAccessToken> (forwarded)
       ▼
  Internal APIs (Identity, Vessel, FileStorage, ServiceRequest, ReferenceData)
```

---

## Browser / Postman Auth Contract

### Protected request (all BFF routes except anonymous):

```http
X-Aizen-User-Token: Bearer <identityAccessToken>
```

### Anonymous requests (no auth header required):

```http
POST /auth/login/username
POST /auth/login/phone
POST /auth/login/otp
POST /auth/otp/send
POST /auth/otp/check
POST /auth/refresh
```

### NOT required from browser / Postman:

```http
Authorization: Bearer <keycloakAccessToken>   ← This is NOT sent by browser/Postman for normal requests
```

---

## Postman Collection Auth Implementation

### Protected requests — header applied:

All protected requests in the collection have exactly one auth-related header:

```json
{
  "key": "X-Aizen-User-Token",
  "value": "{{X_Aizen_User_Token}}",
  "type": "text"
}
```

No `Authorization` header is present on any normal AdminPanel BFF Postman request.

### Collection-level auth:

The Postman collection does not set a collection-level `Authorization` policy (no `Bearer Token` collection auth). Each protected request explicitly declares the `X-Aizen-User-Token` header.

---

## Token Capture

### Identity tokens (login / refresh)

Login and refresh requests use a defensive capture script:

```javascript
const json = pm.response.json();
const data = json.data || json;
const token = data.token || data;
const accessToken = token.accessToken || data.accessToken || data.identityAccessToken;
const refreshToken = token.refreshToken || data.refreshToken || data.identityRefreshToken;
if (accessToken) {
  pm.environment.set('identityAccessToken', accessToken);
  pm.environment.set('X_Aizen_User_Token', `Bearer ${accessToken}`);
}
if (refreshToken) {
  pm.environment.set('identityRefreshToken', refreshToken);
}
```

This handles multiple Identity response envelope shapes:
- `{ data: { token: { accessToken, refreshToken } } }`
- `{ data: { accessToken, refreshToken } }`
- `{ accessToken, refreshToken }`
- `{ identityAccessToken, identityRefreshToken }`

### Keycloak BFF service token (optional folder only)

The optional Keycloak folder captures the BFF service token:

```javascript
const json = pm.response.json();
if (json.access_token) {
  pm.environment.set('adminPanelBffServiceToken', json.access_token);
}
```

**This token is stored only for inspection/debugging. It is NOT attached to any normal AdminPanel BFF request.**

---

## Keycloak BFF Service Token — Server-Side Model

The AdminPanel BFF acquires the Keycloak service token server-side:

```http
POST /realms/inktavia-realm/protocol/openid-connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials
client_id=admin-panel-bff
client_secret=<secret>
```

This token is:
- Cached in Redis via Aizen Core/Cache until near expiry
- Transparently forwarded as `Authorization: Bearer <token>` to internal APIs
- Never exposed to the browser
- Never expected as input from Postman callers on normal BFF requests

---

## Variable Isolation

| Variable | Who Sets It | Who Uses It |
|----------|-------------|-------------|
| `identityAccessToken` | Login/Refresh test scripts | Reference only |
| `X_Aizen_User_Token` | Login/Refresh test scripts | All protected request headers |
| `adminPanelBffServiceToken` | Keycloak optional folder test script | Optional folder inspection only |

`adminPanelBffServiceToken` is **never** used as an Authorization header in any normal BFF Postman request.

---

## Catalog Legacy Wording Override

The endpoint catalog at `docs/admin-web-client/admin-panel-bff-endpoint-catalog.md` contains legacy text:

> "Authorization: all endpoints require `Authorization: Bearer <keycloakAccessToken>` unless marked `[AllowAnonymous]`."

Per `RUN_THIS_FIRST_SINGLE_PROMPT.md`, this wording was **overridden** with the final Identity-only browser auth model. The Postman collection reflects the correct current model.

---

## Affected Files

| File | Change |
|------|--------|
| `docs/postman/admin-panel-bff/Inktavia_AdminPanel_BFF.postman_collection.json` | Created with Identity-only auth model |
| `docs/postman/admin-panel-bff/Inktavia_AdminPanel_BFF_Local.postman_environment.json` | Created with correct variable separation |
| `docs/postman/admin-panel-bff/README.md` | Created with auth model documentation |

---

## Security Observations

1. **`admin_panel_bff_client_secret`** is stored as `type: secret` in the Postman environment with placeholder value `local-dev-only-change-me`. Never commit a real secret.

2. **`identityAccessToken` and `X_Aizen_User_Token`** are captured into Postman environment variables. Postman environment variables are not encrypted at rest in Postman Desktop. For production testing, use short-lived tokens or rotate immediately after testing.

3. **`adminPanelBffServiceToken`** is a Keycloak access token stored in an environment variable for debugging only. It is scoped to the optional folder and not used in normal BFF flows. Treat it as a sensitive credential.

4. The `admin_panel_bff_client_secret` should be supplied via environment variable injection (`KeycloakServiceToken__ClientSecret`) in all non-local environments. The Postman collection uses a placeholder only for local development.

---

## Remaining Gaps / Follow-ups

| Item | Severity | Action |
|------|----------|--------|
| Catalog legacy auth wording not updated | Low | Consider updating the catalog to reflect Identity-only model |
| Real `admin_panel_bff_client_secret` provisioning | High | Must be provided from secret manager before running Keycloak optional folder in non-local environments |
| Identity token `type: default` in environment | Low | Consider marking as `type: secret` if Postman team settings allow; currently left as default to allow easy inspection during development |
