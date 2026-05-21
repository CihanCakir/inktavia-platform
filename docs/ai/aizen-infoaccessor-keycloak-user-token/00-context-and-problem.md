# 00 - Context and Problem

## Context

The project already uses Keycloak successfully. A valid Keycloak token is obtained and sent to protected APIs. The API protection problem was solved earlier by centralizing authorization policy behavior.

## Current Problem

The current user info accessor flow under:

```text
Core/InfoAccessor/src/Aizen.Core.InfoAccessor
```

contains a middleware similar to:

```text
Middlewares/AizenUserInfoMiddleware
```

This middleware tries to parse identity/user claims from the request token.

When the request token is a Keycloak API/client token, the middleware tries to parse it like an application user token and fails. This failure is expected because the Keycloak API/client token and the application user token are different concepts.

## Required Concept

After login, the system will have two tokens:

### Keycloak API/Client Token

Used for API protection.

```http
Authorization: Bearer {keycloak_api_client_token}
```

This token proves API access permission and must not be treated as application user identity.

### Application User Token

Used for application user context. This token represents the logged-in user and should be parsed by InfoAccessor.

It may include user claims such as:

```text
userId
sub
username
phoneNumber
email
profileId
role
version
refreshTokenExpire
```

The exact claim names must be discovered from the repository.

## Objective

Analyze the existing InfoAccessor architecture and implement a clean separation between:

1. API/client authentication token
2. Application user identity token

## Do Not

Do not:

- Break existing Keycloak API protection.
- Parse Keycloak client tokens as application user tokens.
- Hard-code claim names without checking existing conventions.
- Randomly modify all controllers.
- Create a one-off solution only for one API.
- Break existing InfoAccessor consumers.

## Expected Result

The middleware must become token-type aware.

If only the Keycloak API/client token exists, the request should remain authenticated at the API level, but user context should either remain empty/anonymous or be represented as a client context depending on existing framework patterns.

If a valid application user token exists, InfoAccessor should populate the current user context correctly.
