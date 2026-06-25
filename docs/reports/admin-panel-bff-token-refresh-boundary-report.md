# AdminPanel BFF Token Refresh Boundary Report

## Report Date
2026-06-10

---

## Keycloak Service Token Refresh

The BFF acquires a Keycloak service token via `client_credentials` grant. Refresh is automatic:

1. Every request calls `IAdminPanelBffKeycloakServiceTokenProvider.GetAccessTokenAsync()`.
2. The provider checks the cache: if the token exists and `RefreshAfterUtc > UtcNow`, the cached token is returned.
3. If the cache miss or the token is within `CacheSecondsBeforeExpiry` of expiry, a new token is fetched from Keycloak and cached with TTL = `expires_in - CacheSecondsBeforeExpiry`.
4. The browser never sees or manages the Keycloak service token.

**Keycloak refresh token:** Not applicable. `client_credentials` grant does not use refresh tokens. A new access token is fetched when the current one nears expiry.

---

## Identity Token Refresh

Identity access tokens are managed by the React client. The refresh boundary is:

| Scenario | Behavior |
|---|---|
| Identity access token still valid | React sends `X-Aizen-User-Token: Bearer <token>` per request; BFF forwards it unchanged |
| Identity access token expired | BFF returns 401 (or Identity module rejects with 401); React calls `POST /api/v1/admin-panel/auth/refresh` |
| Refresh request | BFF proxies to Identity `POST /api/v1/auth/refresh`; returns new `UserLoginResponse` to React |

The BFF does not hold the Identity refresh token server-side. Identity refresh is entirely client-initiated.

---

## Auth Endpoints That Touch Tokens

| Endpoint | Token behaviour |
|---|---|
| `POST /auth/login/username` | Returns Identity access + refresh token to React (via Identity response) |
| `POST /auth/login/phone` | Same |
| `POST /auth/login/otp` | Same |
| `POST /auth/refresh` | Accepts Identity refresh token from React; proxies to Identity; returns new tokens |
| `POST /auth/password/change` | Forwards `X-Aizen-User-Token` + BFF service token to Identity |

---

## Rules Observed
- BFF does not silently extend Identity tokens.
- BFF does not fabricate Identity tokens.
- BFF does not store Identity refresh tokens.
- BFF does not call Keycloak with a refresh token (not applicable for client_credentials).
- Identity token refresh uses the real Identity refresh contract.
