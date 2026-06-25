# Keycloak Realm Audit Report — `admin-panel-bff` BFF Service-Token Security Model

## Realm File Path

```
infrastructure/keycloak/inktavia-realm-realm.json
```

A second file exists at `infrastructure/keycloak/realm-export.json/realm-export.json` — this is the legacy Aizen framework bootstrap file for the `aizen` realm and is **not** the Inktavia production realm. It was not modified.

---

## Security Model Summary

| Actor | Token Behaviour |
|---|---|
| React / Admin Web | Does **not** obtain or hold a Keycloak token. Sends only `X-Aizen-User-Token: Bearer <identityAccessToken>` |
| AdminPanel BFF | Obtains a **server-side** Keycloak service token via `client_credentials` using the confidential `admin-panel-bff` client. Calls internal APIs with `Authorization: Bearer <keycloak-service-token>` + `X-Aizen-User-Token: Bearer <identityAccessToken>` |

---

## Clients Found (pre-audit)

| Client ID | Type | Notes |
|---|---|---|
| `identity-api` | Resource / API | Present, no client roles |
| `payment-api` | Resource / API | Present, no client roles |
| `profile-api` | Resource / API | Present, no client roles |
| `vessel-api` | Resource / API | Present, no client roles |
| `file-storage-api` | Resource / API | Present, no client roles |
| `service-request-api` | Resource / API | Present, no client roles |
| `reference-data-api` | Resource / API | Present, no client roles |
| `inktavia-mobile` | Public / browser | Present — unchanged |
| `customer-panel` | Public / browser | Present — unchanged |
| `admin-panel` | Public / browser | Present — was `enabled: true`, `standardFlowEnabled: true` |

**Missing:** `admin-panel-bff` (BFF confidential client)

---

## Clients Added or Updated

### `admin-panel-bff` — **ADDED**

```json
{
  "clientId": "admin-panel-bff",
  "publicClient": false,
  "serviceAccountsEnabled": true,
  "standardFlowEnabled": false,
  "implicitFlowEnabled": false,
  "directAccessGrantsEnabled": false,
  "authorizationServicesEnabled": false,
  "secret": "local-dev-only-change-me"
}
```

Five audience protocol mappers attached directly to this client (see Audience Mappers section).

### `admin-panel` — **UPDATED**

Disabled (`enabled: false`) and `standardFlowEnabled` set to `false`. React/Admin Web no longer uses Keycloak browser-login tokens. Protocol mappers removed. Client kept in the file for historical reference only.

---

## Client Roles Found (pre-audit)

**None.** The `roles.client` section did not exist in the original file. All client-level authorization was done through coarse-grained realm roles (e.g., `vessel_read`, `file_storage_write`).

---

## Client Roles Added

All roles were added under `roles.client` in the realm export format.

### `identity-api`

| Role | Description |
|---|---|
| `identity.auth` | Authenticate / token-exchange operations |
| `identity.read` | Read identity data |
| `identity.write` | Write / mutate identity data |
| `identity.admin` | Full administrative access to Identity API |
| `identity.profile.read` | Read user profile data |
| `identity.profile.approve` | Approve pending user profiles |
| `identity.profile.reject` | Reject pending user profiles |

### `reference-data-api`

| Role | Description |
|---|---|
| `reference-data.read` | Read reference data |
| `reference-data.write` | Write reference data |
| `reference-data.lookup.manage` | Manage lookup tables |
| `reference-data.location.read` | Read location reference data |
| `reference-data.currency.manage` | Manage currency data |

### `vessel-api`

| Role | Description |
|---|---|
| `vessel.read` | Read vessel data |
| `vessel.write` | Write / mutate vessel data |
| `vessel.admin` | Full administrative access to Vessel API |
| `vessel.document.manage` | Manage vessel documents |
| `vessel.ownership.manage` | Manage vessel ownership records |

### `file-storage-api`

| Role | Description |
|---|---|
| `file.read` | Read file metadata |
| `file.write` | Upload / write files |
| `file.delete` | Delete files |
| `file.read-url.create` | Generate pre-signed read URLs |
| `file.upload-url.create` | Generate pre-signed upload URLs |
| `file.visibility.manage` | Manage public/private access policy |

### `service-request-api`

| Role | Description |
|---|---|
| `service-request.read` | Read service requests |
| `service-request.write` | Create / update service requests |
| `service-request.admin` | Full administrative access to ServiceRequest API |
| `service-request.assignment.manage` | Manage assignment of service requests |
| `service-request.dispute.manage` | Manage service request disputes |
| `service-request.completion.manage` | Manage service request completion flow |

---

## Service Account Role Mappings Added

Service account user added to the `users` array:

```json
{
  "username": "service-account-admin-panel-bff",
  "serviceAccountClientId": "admin-panel-bff",
  "clientRoles": { ... }
}
```

All 31 client roles across the five API clients are mapped to this service account. No realm-management or `realm-admin` permissions were assigned.

| Client | Roles Assigned |
|---|---|
| `identity-api` | 7 roles (auth, read, write, admin, profile.read, profile.approve, profile.reject) |
| `reference-data-api` | 5 roles (read, write, lookup.manage, location.read, currency.manage) |
| `vessel-api` | 5 roles (read, write, admin, document.manage, ownership.manage) |
| `file-storage-api` | 6 roles (read, write, delete, read-url.create, upload-url.create, visibility.manage) |
| `service-request-api` | 6 roles (read, write, admin, assignment.manage, dispute.manage, completion.manage) |

---

## Audience Mappers Added or Updated

Five direct `oidc-audience-mapper` protocol mappers added to the `admin-panel-bff` client:

| Mapper Name | Audience Included | access.token.claim | introspection.token.claim |
|---|---|---|---|
| `audience-identity-api` | `identity-api` | true | true |
| `audience-reference-data-api` | `reference-data-api` | true | true |
| `audience-vessel-api` | `vessel-api` | true | true |
| `audience-file-storage-api` | `file-storage-api` | true | true |
| `audience-service-request-api` | `service-request-api` | true | true |

`id.token.claim` and `userinfo.token.claim` are set to `false` for all mappers (service tokens only, no user-facing tokens).

---

## Secret Handling Decision

| Concern | Decision |
|---|---|
| Local development | Placeholder value `local-dev-only-change-me` stored in the realm JSON. This file is imported by the local Docker Compose Keycloak container only. |
| Production / staging | The real client secret **must not** be committed. Inject it via the environment variable `KeycloakServiceToken__ClientSecret` in the BFF application configuration. |
| Secret rotation | Rotate the Keycloak client secret in Keycloak Admin UI and update the environment variable / secret store. The realm JSON placeholder does not need to be changed. |

---

## Remaining Gaps

1. **`payment-api` and `profile-api` client roles** — No client roles were defined for these two API resource clients. They exist in the realm for audience validation only. If admin-panel-bff ever needs to call those APIs, roles must be added in a future update.

2. **`inktavia-mobile` audience mappers** — The mobile client still includes `profile-api` and `payment-api` audience mappers. These do not break anything but should be reviewed if those clients are retired.

3. **`customer-panel` audience mappers** — Same as above. Kept unchanged from original.

4. **Realm roles vs client roles** — The realm currently contains coarse-grained realm roles (e.g., `vessel_read`, `vessel_write`). These are used by the `users` array. They have not been removed because they are assigned to test users. Consider migrating to fine-grained client roles long-term.

5. **Keycloak version** — The realm export format used here is compatible with Keycloak 21+. If an older version is in use, verify that `introspection.token.claim` is a supported mapper config key.

6. **Service account email** — Keycloak auto-generates a service account user on import when `serviceAccountsEnabled: true`. The explicit `users` entry with `serviceAccountClientId` is the standard Keycloak export format for pre-seeding role mappings. Confirm Keycloak version handles this correctly on import (tested on Keycloak 22+).

---

## Manual Validation Steps

1. Import the updated `inktavia-realm-realm.json` into a local Keycloak instance (Docker Compose).
2. Navigate to **Realm → Clients → admin-panel-bff → Service Accounts** tab and verify all 31 client roles appear.
3. Navigate to **Realm → Clients → admin-panel-bff → Mappers** and confirm the 5 audience mappers are present.
4. Obtain a token using `client_credentials` (see token validation guide).
5. Decode the JWT and verify `aud`, `azp`, and `resource_access` claims as documented in `keycloak-admin-panel-bff-token-validation-guide.md`.
