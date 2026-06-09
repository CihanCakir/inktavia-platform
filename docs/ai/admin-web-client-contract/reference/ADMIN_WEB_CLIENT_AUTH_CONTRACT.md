# Admin Web Client Authentication Contract

## Token layers

The Admin Web client must handle two token layers:

1. Keycloak access token
   - Source: Keycloak admin-panel client
   - Browser flow: Authorization Code + PKCE
   - Usage: API bearer authorization
   - Header: `Authorization: Bearer <keycloakAccessToken>`

2. Identity access token
   - Source: Identity login through AdminPanel BFF or Identity-auth forwarding endpoint exposed by AdminPanel BFF
   - Usage: Inktavia user context
   - Header: `X-Aizen-User-Token: Bearer <identityAccessToken>`

## Request header standard

Every AdminPanel BFF request must include:

```text
Authorization: Bearer <keycloakAccessToken>
X-Aizen-User-Token: Bearer <identityAccessToken>
```

## Storage guidance

Prefer in-memory storage for tokens. If persistence is required, use session storage with strict expiration handling. Avoid long-lived localStorage token persistence unless explicitly approved.

## Refresh guidance

- Refresh Keycloak token using the OIDC client/keycloak-js refresh mechanism.
- Refresh Identity token using the BFF/Identity refresh endpoint if available.
- If the Identity refresh endpoint is not exposed through AdminPanel BFF, document it as a missing BFF endpoint.

## Failure handling

- 401 from BFF: attempt token refresh once, then redirect to login.
- 403 from BFF: show access-denied page.
- Missing `X-Aizen-User-Token`: redirect to identity-context initialization flow.
