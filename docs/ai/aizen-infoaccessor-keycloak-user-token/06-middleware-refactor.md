# 06 - Middleware Refactor

Refactor:

```text
Middlewares/AizenUserInfoMiddleware
```

## Goal

Make middleware safe for two-token architecture.

## Required Behavior

### Case 1: Only Keycloak API token exists

Expected:

- Do not attempt to parse it as application user token.
- Do not throw because application user claims are missing.
- Allow request to continue if ASP.NET Core authorization has accepted the request.
- User context remains empty/anonymous or client-only depending on design.

### Case 2: Keycloak API token + valid application user token exists

Expected:

- API access is protected by Keycloak.
- User token is parsed separately.
- Current user context is populated.

### Case 3: Keycloak API token + invalid application user token exists

Expected depends on configuration:

- If `ThrowOnInvalidUserToken = true`, return/reject as unauthorized or throw framework auth exception.
- If `false`, log warning and continue with empty user context.

### Case 4: Missing both tokens

Protected endpoints should already be rejected by Keycloak authorization before user-context logic becomes meaningful.

## Middleware Design

Refactor the middleware so that it delegates parsing logic to reader/accessor services.

Avoid having claim parsing logic directly inside the middleware if the architecture supports services.

## Logging

Add safe debug/warning logs if logging infrastructure exists. Do not log raw token values.

## Required Output

Produce:

```md
# Middleware Refactor Result

## Changed Behavior
- ...

## Token Flow
- ...

## Error Handling
- ...

## Logging
- ...

## Files Changed
- ...
```
