# 04 — Authentication and Token Flow

Create:

```text
docs/admin-web-client/admin-web-authentication-flow.md
```

Document how the React.js Admin Web client authenticates and calls AdminPanel BFF.

Required model:

- Keycloak token is used for `Authorization` bearer.
- Identity token is used for `X-Aizen-User-Token`.
- Every BFF request sends both.

Use browser-safe Keycloak Authorization Code + PKCE as the primary approach.

Include:

- Login sequence
- Identity context initialization sequence
- Request header forwarding
- Token refresh
- Logout
- 401/403 handling
- Environment variables
- Local/dev/prod endpoint examples
- Security notes
- Storage recommendation

Do not propose direct browser calls to internal module APIs.
