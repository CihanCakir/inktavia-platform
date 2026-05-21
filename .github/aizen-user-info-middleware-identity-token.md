# Aizen User Info Middleware - Identity Token Header Reading

## Purpose

The system now uses two different tokens:

1. Keycloak API/client token
2. Identity/Application user token

The Keycloak token is sent with:

```http
Authorization: Bearer {keycloak_api_client_token}
```

The Identity/Application user token must be sent with:

```http
X-Aizen-User-Token: Bearer {identity_access_token}
```

This task updates the current `AizenUserInfoMiddleware` so it reads the Identity token from the dedicated user token header and maps its claims into the existing `AizenUserInfo` structure.

## Main Objective

Update the InfoAccessor flow without breaking the existing architecture.

The coding agent must:

- Inspect `Core/InfoAccessor/src/Aizen.Core.InfoAccessor`.
- Inspect `Middlewares/AizenUserInfoMiddleware`.
- Inspect the `AizenUserInfo` model and its flags/properties.
- Add missing fields/flags only if required and consistent with current architecture.
- Inspect `CreateLoginToken` method.
- Inspect `_tokenHelper` and the methods used to create access token and claims.
- Determine exactly which claims are written into the Identity token.
- Update `AizenUserInfoMiddleware` to read those claims from `X-Aizen-User-Token`.
- Set `AizenUserInfo` values safely.
- Do not parse the Keycloak token as user identity.
- Do not break existing middleware registration, DI, or starter architecture.

## Expected Header Contract

```http
Authorization: Bearer {keycloak_api_client_token}
X-Aizen-User-Token: Bearer {identity_access_token}
```

## Example Identity Token Claims

Based on a current login response, the Identity token may contain claims similar to:

```json
{
  "sub": "admin@inktavia.local",
  "UserId": "1",
  "jti": "d1e62677-d157-467e-84e6-54c55e3f5c23",
  "Version": "V1",
  "RefreshTokenExpire": "28.05.2026 10:54:41",
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "Participant",
  "exp": 1781988881,
  "iss": "app.inktavia.com",
  "aud": "www.inktavia.com"
}
```

The actual claim names must be discovered from `CreateLoginToken` and `_tokenHelper`.

## Required Safety

The middleware must not throw just because:

- `X-Aizen-User-Token` is missing.
- The request only contains a Keycloak token.
- A claim is optional.
- Claim casing differs, for example `UserId` vs `userId`.

It should only fail hard if the existing framework explicitly requires that behavior or the new options say so.
