# AdminPanel BFF Keycloak Realm and Postman Report

## Report Date
2026-06-10

---

## Keycloak Realm Requirements

### Realm
```
inktavia-realm
```

### Confidential BFF Client
```
Client ID:              admin-panel-bff
Client authentication:  ON
Service accounts:       ON
Standard flow:          OFF
Direct access grants:   OFF
Implicit flow:          OFF
```

### API / Resource Clients
```
identity-api
reference-data-api
vessel-api
file-storage-api
service-request-api
```

### Service Account Roles (assign to admin-panel-bff)
```
identity-api:
  identity.auth, identity.read, identity.write, identity.admin
  identity.profile.read, identity.profile.approve, identity.profile.reject

reference-data-api:
  reference-data.read, reference-data.write
  reference-data.lookup.manage, reference-data.location.read, reference-data.currency.manage

vessel-api:
  vessel.read, vessel.write, vessel.admin
  vessel.document.manage, vessel.ownership.manage

file-storage-api:
  file.read, file.write, file.delete
  file.read-url.create, file.upload-url.create, file.visibility.manage

service-request-api:
  service-request.read, service-request.write, service-request.admin
  service-request.assignment.manage, service-request.dispute.manage, service-request.completion.manage
```

### Audience Mappers
If internal APIs validate the `aud` claim, configure audience mappers so `admin-panel-bff` service tokens include:
```
identity-api
reference-data-api
vessel-api
file-storage-api
service-request-api
```

---

## Testing Model

### React Admin Web — BFF request (simulated via Postman)

```
POST /api/v1/admin-panel/auth/login/username
Content-Type: application/json
(no Authorization header)

{"username": "admin", "pin": "1234"}
```

After login, React stores the Identity access token. Subsequent requests:
```
GET /api/v1/admin-panel/vessels
X-Aizen-User-Token: Bearer <identityAccessToken>
(no Authorization header from browser)
```

### Internal API direct test (bypass BFF)

```
GET /api/v1/admin/vessels
Authorization: Bearer <admin-panel-bff-keycloak-token-from-client-credentials>
X-Aizen-User-Token: Bearer <identityAccessToken>
```

To obtain the `admin-panel-bff` Keycloak token for testing:
```
POST http://localhost:8080/realms/inktavia-realm/protocol/openid-connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials
client_id=admin-panel-bff
client_secret=<secret>
```

---

## Security Notes
- Browser applications must NOT use `client_credentials` grant.
- Browser applications must NOT use `password` grant for Keycloak.
- Do not give `admin-panel-bff` service account broad `realm-admin` permissions.
- Assign only the minimum required API client roles to the `admin-panel-bff` service account.
