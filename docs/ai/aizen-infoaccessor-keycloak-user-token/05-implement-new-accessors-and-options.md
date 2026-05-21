# 05 - Implement New Accessors and Options

Implement the selected design inside:

```text
Core/InfoAccessor/src/Aizen.Core.InfoAccessor
```

## Main Goal

Support separate accessors/readers for:

1. Keycloak/API client context
2. Application user context

## Implementation Requirements

### Options

Add or extend options for token sources and behavior.

Recommended properties:

```csharp
public string UserTokenHeaderName { get; set; } = "X-Aizen-User-Token";
public string AuthorizationHeaderName { get; set; } = "Authorization";
public bool ThrowOnMissingUserToken { get; set; } = false;
public bool ThrowOnInvalidUserToken { get; set; } = false;
```

Use existing options conventions if present.

### User Token Reader

Create or adapt a component responsible only for application user token parsing.

Responsibilities:

- Read configured user token header.
- Strip `Bearer ` prefix if present.
- Parse JWT safely.
- Validate expected user claims.
- Return a result object instead of throwing for normal missing claim scenarios.
- Populate existing user info model if valid.

### Keycloak Token Reader

Create or adapt a component that understands Keycloak token shape.

Responsibilities:

- Read `Authorization` header or `HttpContext.User.Claims`.
- Extract client-related claims safely.
- Never require application user claims.
- Return a client context if implemented.

### Result Type

Prefer a safe result type or existing project result pattern.

## Do Not

Do not:

- Throw for missing user token by default.
- Throw for Keycloak client token missing application user claims.
- Break existing accessor interface consumers.
- Hard-code headers where options should be used.

## Required Output

Produce:

```md
# New Accessors and Options Result

## New Types
- ...

## Extended Types
- ...

## Options
- ...

## User Token Reader
- ...

## Keycloak Token Reader
- ...

## Compatibility Notes
- ...
```
