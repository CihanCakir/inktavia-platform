# Keycloak Service Token Cache Provider Design

## Suggested interface

Adapt naming to repository conventions, but implement a service equivalent to:

```csharp
public interface IAdminPanelBffKeycloakServiceTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
```

## Suggested implementation

```csharp
public sealed class AdminPanelBffKeycloakServiceTokenProvider : IAdminPanelBffKeycloakServiceTokenProvider
{
    // Use Aizen Core/Cache abstraction.
    // Use configured Keycloak token endpoint.
    // Cache token until shortly before expiry.
}
```

## Token request

```http
POST /realms/inktavia-realm/protocol/openid-connect/token
Content-Type: application/x-www-form-urlencoded
```

Body:

```text
grant_type=client_credentials
client_id=admin-panel-bff
client_secret=<admin-panel-bff-secret>
```

## Cache key

Suggested format:

```text
inktavia:admin-panel-bff:keycloak-service-token:{realmHash}:{clientId}:{audienceOrScopeHash}
```

If no audience/scope is explicitly requested:

```text
inktavia:admin-panel-bff:keycloak-service-token:{realmHash}:{clientId}:default
```

Do not include Identity tokens or raw secrets in this cache key.

## Cache value

Suggested DTO:

```csharp
public sealed class CachedKeycloakServiceToken
{
    public string AccessToken { get; init; } = default!;
    public string TokenType { get; init; } = "Bearer";
    public DateTimeOffset ExpiresAtUtc { get; init; }
    public DateTimeOffset RefreshAfterUtc { get; init; }
}
```

If the repository already has a reusable cache entry structure, use it.

## TTL strategy

```text
Redis TTL = expires_in - CacheSecondsBeforeExpiry
```

Example:

```text
expires_in = 300 seconds
CacheSecondsBeforeExpiry = 60
Redis TTL = 240 seconds
```

## Stampede protection

If available, use Aizen cache:

```text
GetOrCreateAsync
Distributed lock
Single-flight lock
Atomic SetIfNotExists
```

Goal: when the token expires and many requests arrive, only one request should call Keycloak.
