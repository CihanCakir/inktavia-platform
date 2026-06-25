# Final Token and Security Model

## Browser/Admin Web -> AdminPanel BFF

The React/Admin Web client authenticates with Identity only.

Expected request header:

```http
X-Aizen-User-Token: Bearer <identityAccessToken>
```

Do not require browser-provided Keycloak tokens.

Do not read service credentials from the browser.

Do not forward incoming browser `Authorization` headers.

## AdminPanel BFF -> Internal APIs

AdminPanel BFF uses the confidential `admin-panel-bff` Keycloak client to obtain a server-side service token.

Expected internal request headers:

```http
Authorization: Bearer <admin-panel-bff-keycloak-service-token>
X-Aizen-User-Token: Bearer <identityAccessToken>
```

## Important test-runner correction

The current local report shows requests still include both:

```http
Authorization: Bearer <identityAccessToken>
X-Aizen-User-Token: Bearer <identityAccessToken>
```

This is not the final browser contract.

Fix the BFF test runner/Postman collection so React-like BFF calls send only `X-Aizen-User-Token`.

If `Authorization` is present on incoming browser requests, AdminPanel BFF must ignore it for internal forwarding.
