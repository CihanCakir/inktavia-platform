# 00 - Context and Contract

## Context

The project now has working Keycloak and Identity token separation.

The API is protected by Keycloak.

The application user identity is represented by a separate Identity/Application token generated after login.

## Required Request Contract

After login, requests to user-aware protected endpoints should send both tokens:

```http
Authorization: Bearer {keycloak_api_client_token}
X-Aizen-User-Token: Bearer {identity_access_token}
```

## Token Responsibilities

### Keycloak Token

Header:

```http
Authorization
```

Purpose:

```text
API/client access protection.
```

Do not parse this as `AizenUserInfo`.

### Identity Token

Header:

```http
X-Aizen-User-Token
```

Purpose:

```text
Application user context.
```

This token must be parsed by `AizenUserInfoMiddleware`.

## Main Task

Update:

```text
Core/InfoAccessor/src/Aizen.Core.InfoAccessor
```

Especially:

```text
Middlewares/AizenUserInfoMiddleware
```

So that it:

1. Reads `X-Aizen-User-Token`.
2. Parses the Identity token.
3. Maps claims into `AizenUserInfo`.
4. Sets existing flags/properties correctly.
5. Adds missing flags/properties only if needed.
6. Does not throw when the header is missing unless configured.
7. Does not parse `Authorization` as user identity.

## Important

Before changing code, inspect:

- Existing `AizenUserInfoMiddleware`
- Existing `AizenUserInfo`
- Existing InfoAccessor interfaces
- Existing options/configuration
- Existing DI registration
- `CreateLoginToken`
- `_tokenHelper`
- Access token creation methods
- Claim names added into the Identity token

## Expected Final Behavior

When request contains:

```http
Authorization: Bearer {keycloak_token}
X-Aizen-User-Token: Bearer {identity_token}
```

`AizenUserInfo` should be populated from the Identity token.

When request contains only:

```http
Authorization: Bearer {keycloak_token}
```

request should not fail because of InfoAccessor. `AizenUserInfo` should remain empty/anonymous/client-only based on existing architecture.
