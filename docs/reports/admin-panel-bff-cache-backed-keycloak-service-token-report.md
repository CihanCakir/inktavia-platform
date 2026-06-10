# AdminPanel BFF Cache-Backed Keycloak Service Token Report

## Implementation Date
2026-06-10

## Objective
Implement a server-side cache-backed Keycloak service token provider for the `admin-panel-bff` confidential client, using the existing Aizen Core/Cache (`IAizenDistributedCache`) infrastructure.

---

## New Files

| File | Purpose |
|---|---|
| `Common/Options/KeycloakServiceTokenOptions.cs` | Configuration POCO bound to `KeycloakServiceToken` section |
| `Common/Services/IAdminPanelBffKeycloakServiceTokenProvider.cs` | Provider interface |
| `Common/Services/AdminPanelBffKeycloakServiceTokenProvider.cs` | Caching implementation |
| `Common/Services/CachedKeycloakServiceToken.cs` | Redis-stored DTO |

---

## Provider Interface

```csharp
public interface IAdminPanelBffKeycloakServiceTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
```

---

## Cache Strategy

### Cache key format
```
inktavia:admin-panel-bff:keycloak-service-token:{clientId}:default
```
Example: `inktavia:admin-panel-bff:keycloak-service-token:admin-panel-bff:default`

### Cache key is NOT:
- User-token based
- Identity-token based
- Containing raw secrets

### TTL strategy
```
Redis TTL = expires_in - CacheSecondsBeforeExpiry
Minimum enforced TTL = 10 seconds
```

Example:
```
expires_in = 300 seconds
CacheSecondsBeforeExpiry = 60 (default)
Redis TTL = 240 seconds
```

The `RefreshAfterUtc` field in the cached DTO is compared at read time as a secondary guard against returning a token that is already within the buffer window.

### Aizen cache methods used
- `IAizenDistributedCache.ExistNoHash(key)` — fast existence check
- `IAizenDistributedCache.GetNoHash<CachedKeycloakServiceToken>(key)` — deserializes stored DTO
- `IAizenDistributedCache.SetNoHash<CachedKeycloakServiceToken>(key, entry, ttl)` — stores with explicit TTL

---

## Token Acquisition

```http
POST /realms/inktavia-realm/protocol/openid-connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials
client_id=admin-panel-bff
client_secret=<from-secret>
```

The `HttpClient` named `"KeycloakServiceToken"` is used via `IHttpClientFactory`.

---

## Stampede Protection

No distributed lock mechanism was found in Aizen Core/Cache. In a race at expiry, multiple BFF instances may call Keycloak simultaneously. Each call yields a valid token and overwrites the same cache key with the same value. This is benign for `client_credentials`.

**Follow-up:** Add `SET NX` distributed lock if high concurrency is observed.

---

## Token Value at Rest

Token values are stored as plain JSON in Redis (no encryption). No Aizen-approved encryption utility was found.

**Security follow-up:** Encrypt `AccessToken` field before Redis storage using ASP.NET Core Data Protection or a symmetric encryption utility.

---

## DI Registration

```csharp
services.Configure<KeycloakServiceTokenOptions>(configuration.GetSection(KeycloakServiceTokenOptions.SectionName));
services.AddScoped<IAdminPanelBffKeycloakServiceTokenProvider, AdminPanelBffKeycloakServiceTokenProvider>();
```

---

## Configuration

```json
"KeycloakServiceToken": {
  "Authority": "http://localhost:8080/realms/inktavia-realm",
  "TokenEndpoint": "http://localhost:8080/realms/inktavia-realm/protocol/openid-connect/token",
  "ClientId": "admin-panel-bff",
  "ClientSecret": "__FROM_SECRET__",
  "CacheSecondsBeforeExpiry": 60,
  "CacheKeyPrefix": "inktavia:admin-panel-bff:keycloak-service-token"
}
```

`ClientSecret` must be injected via environment variable: `KeycloakServiceToken__ClientSecret`
