# Aizen Core/Cache Token Strategy

## Mandatory discovery

Before implementing token caching, inspect the repository for:

```text
Core/Cache
Core/Cache/src
Aizen.Core.Cache
IAizenQueryHandlerCacheable
Redis cache services
Distributed cache abstractions
Cache key builders
GetOrCreateAsync patterns
Distributed lock or single-flight patterns
DependencyInjection extensions
Options binding patterns
```

Use the real Aizen cache abstraction names and conventions.

Do not create a custom Redis client if the repository already provides Aizen cache abstractions.

Do not bypass the existing cache layer.

## Cache responsibilities

Use separate cache responsibilities:

1. `KeycloakServiceTokenCache`
   - service/client/realm/audience based
   - not user-specific
   - stores BFF service token response

2. `IdentitySessionOrTokenContextCache`
   - user/device/session based only where needed
   - should prefer metadata unless protected token storage exists

## Key rule

Do not store Keycloak service token and Identity token as a single raw token pair.

They meet in the request pipeline, not as one logical security credential.

## Redis token storage

If token values are stored in Redis:

- Prefer repository-approved protection/encryption utility.
- Never use raw token as Redis key.
- Hash token values if a token lookup key is required.
- Apply TTL based on actual token expiry.
- Never log token values.

If no protection/encryption utility exists, document this as a security follow-up.
