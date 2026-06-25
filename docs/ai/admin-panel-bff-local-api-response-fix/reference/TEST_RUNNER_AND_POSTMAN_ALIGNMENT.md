# Test Runner and Postman Alignment

The local report currently includes an incoming `Authorization` header with an Identity JWT.

Under the final model, React-like BFF tests must send:

```http
X-Aizen-User-Token: Bearer <identityAccessToken>
```

Do not send:

```http
Authorization: Bearer <identityAccessToken>
```

Keep a separate optional Keycloak service-token validation folder/request for testing the Keycloak token endpoint, but do not send that service token to the BFF from browser-like tests.

Update:

```text
docs/postman/admin-panel-bff/
Bff/src/AdminPanel/docs/reports test runner scripts
any local BFF API test harness
```
