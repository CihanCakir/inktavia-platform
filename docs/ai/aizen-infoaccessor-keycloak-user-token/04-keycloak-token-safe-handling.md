# 04 - Keycloak Token Safe Handling

Ensure Keycloak API/client tokens are handled safely.

## Requirement

A Keycloak client/API token should not be parsed as an application user token.

## Keycloak Token Detection

Common Keycloak claims may include:

```text
iss
aud
azp
client_id
scope
typ
realm_access
resource_access
preferred_username
exp
iat
jti
```

A client credentials token may not contain application user claims.

## Safe Behavior

When the middleware sees only a Keycloak token:

- Do not throw because `userId`, `version`, `refreshTokenExpire` or similar user claims are missing.
- Optionally populate a client context if the architecture supports it.
- Keep user context empty/anonymous if no application user token exists.
- Let ASP.NET Core authentication/authorization handle API access.

## Optional Client Context

If useful and consistent with current architecture, introduce:

```text
IAizenClientInfoAccessor
AizenClientInfo
AizenKeycloakClientInfo
```

Fields may include:

```text
ClientId
AuthorizedParty
Audience
Issuer
Scope
Subject
PreferredUsername
```

Do not over-engineer if current consumers only need user context.

## Required Output

Produce:

```md
# Keycloak Token Safe Handling Result

## Keycloak Claims Observed
- ...

## Client Token Detection Strategy
- ...

## User Claim Parsing Guard
- ...

## Optional Client Context
- Added / Not added, reason

## Files To Change
- ...
```
