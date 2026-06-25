# AdminPanel BFF — Keycloak Token Validation Guide

This guide describes how to manually obtain and validate the `admin-panel-bff` service token from a locally running Keycloak instance.

---

## Prerequisites

- Keycloak running at `http://localhost:8080`
- Realm `inktavia-realm` imported from `infrastructure/keycloak/inktavia-realm-realm.json`
- `admin-panel-bff` client secret known (local dev: `local-dev-only-change-me`)

---

## Step 1 — Obtain a Service Token

### cURL

```bash
curl -s -X POST \
  http://localhost:8080/realms/inktavia-realm/protocol/openid-connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials" \
  -d "client_id=admin-panel-bff" \
  -d "client_secret=local-dev-only-change-me"
```

### Postman

```
POST http://localhost:8080/realms/inktavia-realm/protocol/openid-connect/token
Content-Type: application/x-www-form-urlencoded

Body (x-www-form-urlencoded):
  grant_type    = client_credentials
  client_id     = admin-panel-bff
  client_secret = <secret>
```

Replace `<secret>` with `local-dev-only-change-me` for local dev, or the real secret in other environments (never commit the real value).

### Expected successful response

```json
{
  "access_token": "<jwt>",
  "expires_in": 3600,
  "token_type": "Bearer",
  "not-before-policy": 0,
  "scope": "..."
}
```

---

## Step 2 — Decode the Access Token

Copy the `access_token` value and decode it at [https://jwt.io](https://jwt.io) or use the CLI:

```bash
# Requires jq
TOKEN=$(curl -s -X POST \
  http://localhost:8080/realms/inktavia-realm/protocol/openid-connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials" \
  -d "client_id=admin-panel-bff" \
  -d "client_secret=local-dev-only-change-me" \
  | jq -r '.access_token')

echo $TOKEN | cut -d. -f2 | base64 -d 2>/dev/null | jq .
```

---

## Step 3 — Validate Required Claims

### 3.1 `azp` (Authorized Party)

```
azp = "admin-panel-bff"
```

Confirms the token was issued to the BFF client.

### 3.2 `aud` (Audience)

The `aud` claim must contain **all five** internal API audience values:

```
"aud": [
  "identity-api",
  "reference-data-api",
  "vessel-api",
  "file-storage-api",
  "service-request-api"
]
```

> ⚠️ If `aud` is missing any entry, the corresponding API will reject the token with `401 Unauthorized`.  
> Root cause: the audience mapper for that API is missing or misconfigured on the `admin-panel-bff` client.

### 3.3 `resource_access` (Client Roles)

The `resource_access` claim must contain all five client sections with their assigned roles:

```json
{
  "resource_access": {
    "identity-api": {
      "roles": [
        "identity.auth",
        "identity.read",
        "identity.write",
        "identity.admin",
        "identity.profile.read",
        "identity.profile.approve",
        "identity.profile.reject"
      ]
    },
    "reference-data-api": {
      "roles": [
        "reference-data.read",
        "reference-data.write",
        "reference-data.lookup.manage",
        "reference-data.location.read",
        "reference-data.currency.manage"
      ]
    },
    "vessel-api": {
      "roles": [
        "vessel.read",
        "vessel.write",
        "vessel.admin",
        "vessel.document.manage",
        "vessel.ownership.manage"
      ]
    },
    "file-storage-api": {
      "roles": [
        "file.read",
        "file.write",
        "file.delete",
        "file.read-url.create",
        "file.upload-url.create",
        "file.visibility.manage"
      ]
    },
    "service-request-api": {
      "roles": [
        "service-request.read",
        "service-request.write",
        "service-request.admin",
        "service-request.assignment.manage",
        "service-request.dispute.manage",
        "service-request.completion.manage"
      ]
    }
  }
}
```

#### Minimum spot-checks

| Check | Expected value |
|---|---|
| `resource_access.identity-api.roles` contains | `identity.admin` |
| `resource_access.vessel-api.roles` contains | `vessel.admin` |
| `resource_access.file-storage-api.roles` contains | `file.read-url.create` |
| `resource_access.service-request-api.roles` contains | `service-request.admin` |

### 3.4 `sub` — Service Account Subject

The `sub` claim will be the internal Keycloak UUID of the service account user. Confirm it is **not** a real human user UUID.

The `preferred_username` claim should equal `service-account-admin-panel-bff`.

---

## Step 4 — Verify No `realm-admin` Permissions

Confirm the decoded token does **not** contain any of the following in `resource_access`:

```
realm-management
account
```

The `admin-panel-bff` service account must not hold realm-admin or realm-management roles.

---

## Step 5 — Simulate BFF → Internal API Call

Use the obtained token as the `Authorization` header and pass a fake user identity token as `X-Aizen-User-Token`:

```bash
curl -s http://localhost:<api-port>/v1/some-endpoint \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Aizen-User-Token: Bearer <identityAccessToken>"
```

The internal API should:
1. Validate `Authorization` bearer token against Keycloak (`aud` claim matches the API's own `clientId`)
2. Extract `X-Aizen-User-Token` for user-context authorization

---

## Troubleshooting

| Symptom | Likely Cause |
|---|---|
| `401` from token endpoint | Wrong `client_secret` or `client_id` |
| `aud` missing an API | Audience mapper not present or `access.token.claim` is `false` |
| `resource_access` empty | Service account role mappings not imported correctly; re-import realm or add manually in Keycloak Admin UI |
| Token contains `realm-admin` roles | Service account was incorrectly assigned realm-management roles; remove them |
| `grant_type=client_credentials` rejected | `serviceAccountsEnabled` is `false` on the client |

---

## Secret Management Reminder

| Environment | Source |
|---|---|
| Local dev | `local-dev-only-change-me` (hardcoded in realm import for convenience) |
| All other environments | Inject via `KeycloakServiceToken__ClientSecret` environment variable |

Never commit the real production secret to source control.
