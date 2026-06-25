# Postman Auth Contract

## Token layers

### Layer 1: Keycloak API access token

Used for API-level authorization.

Postman variable:

```text
active_access_token
```

Request auth:

```text
Authorization: Bearer {{active_access_token}}
```

### Layer 2: Identity application user token

Used for Aizen user context middleware.

Postman variables:

```text
identityAccessToken
X-Aizen-User-Token
```

Header:

```text
X-Aizen-User-Token: {{X-Aizen-User-Token}}
```

Default value of `X-Aizen-User-Token` should be set by script as:

```javascript
pm.environment.set("X-Aizen-User-Token", `Bearer ${identityAccessToken}`);
```

If the current middleware expects only the raw token, detect and document it. Do not guess silently.

## Required auth requests

- Get Mobile Token - inktavia-mobile
- Get Customer Token - customer-panel
- Get Admin Token - admin-panel
- Identity Login - Owner
- Identity Login - Provider if available
- Identity Login - Admin
- Set Active Token - Mobile/Owner
- Set Active Token - Provider
- Set Active Token - Admin
