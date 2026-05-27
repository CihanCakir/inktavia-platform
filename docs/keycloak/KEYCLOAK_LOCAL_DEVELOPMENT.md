# Keycloak Local Development Guide

## Overview

This project uses Keycloak 25 as the central authentication and token provider.
The realm `inktavia-realm` is imported automatically on first start via `infrastructure/keycloak/inktavia-realm-realm.json`.

---

## How to Run

```bash
docker compose up -d
```

Keycloak and its database start automatically. The realm import runs on first startup.

---

## Keycloak Admin Console

| URL | http://localhost:8080 |
|---|---|
| Username | `admin` |
| Password | `admin` |

After login, switch to the **inktavia-realm** realm using the dropdown in the top-left corner.

---

## Realm Import

The realm import file is:

```
infrastructure/keycloak/inktavia-realm-realm.json
```

It is mounted as a read-only volume into the Keycloak container:

```
/opt/keycloak/data/import/inktavia-realm-realm.json
```

Keycloak imports this file automatically on startup when it does not detect an existing realm with the same name.

> **Warning:** If you change the realm import file and the changes are not reflected, reset local volumes:
>
> ```bash
> docker compose down -v
> docker compose up -d
> ```
>
> `docker compose down -v` removes local volumes. **Use only in local development.**

---

## Clients

### API Clients (Resource Servers — not for login)

| Client ID | Purpose |
|---|---|
| `identity-api` | Audience for Identity API JWT validation |
| `payment-api` | Audience for Payment API JWT validation |
| `profile-api` | Audience for Profile API JWT validation |

### Application Clients (Login Clients)

| Client ID | Purpose | Allowed Audiences |
|---|---|---|
| `inktavia-mobile` | Mobile app login (PKCE) | identity-api, profile-api |
| `customer-panel` | Customer panel login (PKCE) | identity-api, profile-api, payment-api |
| `admin-panel` | Admin panel login (PKCE) | identity-api, profile-api, payment-api |

---

## Realm Roles

| Role | Description |
|---|---|
| `mobile_user` | Standard mobile user |
| `customer_user` | Customer panel user |
| `admin_user` | Administrator with full access |
| `identity_read` | Read access to Identity API |
| `identity_write` | Write access to Identity API |
| `profile_read` | Read access to Profile API |
| `profile_write` | Write access to Profile API |
| `payment_read` | Read access to Payment API |
| `payment_write` | Write access to Payment API |

---

## Test Users

| Email | Password | Roles |
|---|---|---|
| `mobile.user@inktavia.com` | `Password123!` | mobile_user, profile_read, profile_write |
| `customer.user@inktavia.com` | `Password123!` | customer_user, profile_read, payment_read |
| `admin.user@inktavia.com` | `Password123!` | admin_user, identity_read, identity_write, profile_read, profile_write, payment_read, payment_write |

---

## How to Get a Token

Use the **Resource Owner Password Credentials** flow (direct access grants) for testing via `curl`.

> **Note:** The application clients in this realm have `directAccessGrantsEnabled: false` for security.
> For local testing, use the Keycloak admin console to temporarily enable direct access grants on a client,
> or use the Authorization Code + PKCE flow.

### Option 1 — Keycloak Admin Console Token

1. Log in to http://localhost:8080
2. Switch to `inktavia-realm`
3. Go to **Clients** → select a client (e.g. `admin-panel`)
4. Set `directAccessGrantsEnabled: true` temporarily for testing
5. Save

Then request a token:

```bash
curl -s -X POST http://localhost:8080/realms/inktavia-realm/protocol/openid-connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password" \
  -d "client_id=admin-panel" \
  -d "username=admin.user@inktavia.com" \
  -d "password=Password123!" \
  | jq .access_token
```

### Option 2 — Authorization Code + PKCE Flow

Use a tool like [Insomnia](https://insomnia.rest/) or [Postman](https://www.postman.com/) with the following settings:

- Grant Type: Authorization Code (PKCE)
- Auth URL: `http://localhost:8080/realms/inktavia-realm/protocol/openid-connect/auth`
- Token URL: `http://localhost:8080/realms/inktavia-realm/protocol/openid-connect/token`
- Client ID: `admin-panel` (or `customer-panel` / `inktavia-mobile`)
- Redirect URI: `http://localhost:3001/*` (for admin-panel)
- Code Challenge Method: S256

---

## How to Decode a Token

```bash
# Decode the JWT payload (base64)
echo "YOUR_ACCESS_TOKEN" | cut -d. -f2 | base64 -d | jq .
```

Expected claims in the token:
- `sub` — Keycloak user subject id
- `preferred_username` — username (email)
- `email` — user email
- `realm_access.roles` — array of realm roles
- `aud` — audience array (e.g. `["identity-api", "profile-api"]`)
- `iss` — issuer (`http://localhost:8080/realms/inktavia-realm`)

---

## How to Validate Clients

In the Keycloak admin console:
1. Go to **inktavia-realm** → **Clients**
2. Verify that all 6 clients exist: `identity-api`, `payment-api`, `profile-api`, `inktavia-mobile`, `customer-panel`, `admin-panel`
3. For each application client, check **Client scopes** → **Evaluate** tab to preview the token payload for a test user

---

## How to Test APIs

### API Ports

| API | Local Port |
|---|---|
| Identity API | http://localhost:7101 |
| Profile API | http://localhost:7102 |
| Payment API | http://localhost:7103 |

### Test Identity API

Get a token for `admin.user@inktavia.com` and call a protected endpoint:

```bash
TOKEN="your_access_token_here"

curl -H "Authorization: Bearer $TOKEN" http://localhost:7101/swagger
```

### Test Profile API

```bash
curl -H "Authorization: Bearer $TOKEN" http://localhost:7102/swagger
```

### Test Payment API

```bash
curl -H "Authorization: Bearer $TOKEN" http://localhost:7103/swagger
```

### Test Forbidden Cases

1. Get a token for `mobile.user@inktavia.com` (via `inktavia-mobile` client — no `payment-api` audience)
2. Call a Payment API protected endpoint
3. Expected: `401 Unauthorized` (audience `payment-api` missing from token)

Or:
1. Get a token for `customer.user@inktavia.com` (has `payment_read`, no `payment_write`)
2. Call a payment write endpoint protected by `PaymentWrite` policy
3. Expected: `403 Forbidden` (missing `payment_write` role)

---

## Swagger Bearer Authentication

Each API Swagger UI has an **Authorize** button.

1. Open the Swagger UI (e.g. http://localhost:7101/swagger)
2. Click **Authorize**
3. Enter: `Bearer YOUR_ACCESS_TOKEN`
4. Click **Authorize** then **Close**
5. Protected endpoints now send the Bearer token automatically

---

## Authorization Policies

| Policy | Required Role |
|---|---|
| `IdentityRead` | `identity_read` or `admin_user` |
| `IdentityWrite` | `identity_write` or `admin_user` |
| `ProfileRead` | `profile_read` or `admin_user` |
| `ProfileWrite` | `profile_write` or `admin_user` |
| `PaymentRead` | `payment_read` or `admin_user` |
| `PaymentWrite` | `payment_write` or `admin_user` |
| `AdminOnly` | `admin_user` |

---

## Identity Module — Local User Mapping

Keycloak users are mapped to local `UserEntity` via:

```
access_token.sub -> UserEntity.KeycloakSubjectId
```

The Identity API reads the `sub`, `email`, and `preferred_username` claims from the JWT token and can provision a local user on first login (controlled provisioning).

---

## Reset Local Keycloak Data

If you change the realm import file and the changes are not reflected after restart:

```bash
docker compose down -v
docker compose up -d
```

> **Warning:** This deletes all local Docker volumes including the Keycloak database. Use only in local development.
