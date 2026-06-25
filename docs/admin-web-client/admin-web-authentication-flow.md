# Admin Web Client — Authentication Flow

## Overview

The Admin Web client uses **two independent token layers** that must both be present on every protected BFF request.

```
Browser                Keycloak               AdminPanel BFF         Identity Module
  |                       |                        |                       |
  |-- Auth Code + PKCE -->|                        |                       |
  |<-- Keycloak Token ----|                        |                       |
  |                       |                        |                       |
  |-- POST /auth/login/* ---------------------------------->               |
  |<-- UserLoginResponse (accessToken, refreshToken) ------|               |
  |                       |                        |<-- Identity login --->|
  |                       |                        |                       |
  |-- API call (Authorization + X-Aizen-User-Token) ------>               |
```

---

## Token Layer 1: Keycloak Access Token

| Property | Value |
|----------|-------|
| Provider | Keycloak `admin-panel` client |
| Flow | Authorization Code + PKCE |
| Storage | In-memory (preferred); session storage if persistence required |
| Header | `Authorization: Bearer <keycloakAccessToken>` |
| Refresh | Via `keycloak-js` or `@react-keycloak/web` silent refresh / `updateToken()` |

### PKCE Flow Steps

1. User navigates to admin UI.
2. Client redirects to Keycloak `/auth` with `code_challenge` and `code_challenge_method=S256`.
3. User authenticates in Keycloak.
4. Keycloak redirects back with `code`.
5. Client exchanges `code` + `code_verifier` for tokens at Keycloak `/token`.
6. `access_token` and `refresh_token` are stored in memory.

---

## Token Layer 2: Identity Access Token

| Property | Value |
|----------|-------|
| Provider | Inktavia Identity module via AdminPanel BFF |
| Endpoint | `POST /api/v1/admin-panel/auth/login/username` (or `/login/phone`, `/login/otp`) |
| Storage | In-memory (preferred); session storage if persistence required |
| Header | `X-Aizen-User-Token: Bearer <identityAccessToken>` |
| Refresh | `POST /api/v1/admin-panel/auth/refresh` with `{ refreshToken }` |

### Identity Login Steps

1. After Keycloak login succeeds, immediately call one of the identity login endpoints.
2. Store `accessToken` and `refreshToken` from `UserLoginResponse` in memory.
3. On every BFF request, attach both headers.

---

## Request Header Standard

Every protected AdminPanel BFF request must include:

```http
Authorization: Bearer <keycloakAccessToken>
X-Aizen-User-Token: Bearer <identityAccessToken>
```

---

## Token Storage Strategy

| Token | Preferred Storage | Fallback |
|-------|-------------------|----------|
| Keycloak access token | React context / memory | Session storage |
| Keycloak refresh token | `keycloak-js` internal | Session storage |
| Identity access token | React context / memory | Session storage |
| Identity refresh token | React context / memory | Session storage |

**Do not use `localStorage` for any token unless explicitly approved.**

---

## Token Refresh Strategy

### Keycloak Token Refresh

```typescript
// Using keycloak-js
keycloak.updateToken(60).then((refreshed) => {
  if (refreshed) {
    // update stored access token
  }
});
```

Schedule `updateToken` to run 60 seconds before expiry using `keycloak.tokenParsed?.exp`.

### Identity Token Refresh

```typescript
// POST /api/v1/admin-panel/auth/refresh
const response = await authClient.refresh({ refreshToken: identityRefreshToken });
// update stored identity access token
```

Call before the Identity `accessToken` expires. Use `exp` from JWT payload if available, or refresh proactively on 401.

---

## Error Handling

| HTTP Status | BFF Source | Action |
|-------------|------------|--------|
| 401 | Any BFF endpoint | Attempt Keycloak token refresh once. If still 401, redirect to `/login`. |
| 401 | After refresh | Clear both tokens, redirect to `/login`. |
| 403 | Any BFF endpoint | Redirect to `/access-denied` page. |
| 401 with missing `X-Aizen-User-Token` | BFF validation | Redirect to identity initialization (re-login with Identity credentials). |

---

## Initialization Sequence

```
App Start
  ├── Initialize Keycloak (keycloak-js)
  │     ├── if authenticated → restore Keycloak token from session
  │     └── if not → PKCE redirect to Keycloak login
  ├── On Keycloak token ready:
  │     ├── Check session storage for Identity token
  │     │     ├── if valid → restore
  │     │     └── if missing/expired → show Identity login modal or redirect to combined login page
  └── On both tokens ready → render protected admin UI
```

---

## Missing BFF Endpoints (Gaps)

The following Identity operations are **not currently exposed** through AdminPanel BFF:

| Gap | Impact |
|-----|--------|
| No Identity `refresh` for Keycloak-originated sessions | Identity token must be refreshed via `/auth/refresh` using Identity refresh token, independent of Keycloak refresh. This is available at `POST /auth/refresh`. |
| No dedicated `logout` endpoint | Client must call Keycloak logout and discard Identity tokens locally. No server-side Identity session invalidation is exposed through BFF. |
| No `me` / current user identity endpoint | Admin user identity must be derived from the decoded JWT claims or from the login response. |
