# 09 — Realm, Postman and Security Validation

Document Keycloak realm expectations:

- Realm: `inktavia-realm`
- Confidential client: `admin-panel-bff`
- Service accounts enabled
- Standard flow disabled
- Direct access grants disabled
- Required API/resource clients and roles
- Audience mappers if internal APIs validate `aud`

Postman model:

AdminPanel BFF React-like requests:

```http
X-Aizen-User-Token: Bearer <identityAccessToken>
```

Internal API direct tests:

```http
Authorization: Bearer <service-or-test-Keycloak-token>
X-Aizen-User-Token: Bearer <identityAccessToken>
```

Do not imply browser applications should use password grant.

Generate or update:

```text
docs/reports/admin-panel-bff-keycloak-realm-and-postman-report.md
```
