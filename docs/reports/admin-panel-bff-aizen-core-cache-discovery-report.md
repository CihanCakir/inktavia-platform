# AdminPanel BFF Aizen Core/Cache Discovery Report

## Discovery Date
2026-06-10

## Location
```
Core/Cache/src/Aizen.Core.Cache.Abstraction/
Core/Cache/src/Aizen.Core.Cache/
```

---

## Abstractions Discovered

### `IAizenCache` (base interface)
```csharp
Task<T> GetAsync<T>(CancellationToken token = default);
Task<T> GetAsync<T>(string key, CancellationToken token = default);
Task<(bool keyExists, T cacheItem)> TryGetAsync<T>(CancellationToken token = default);
Task<(bool keyExists, T cacheItem)> TryGetAsync<T>(string key, CancellationToken token = default);
Task SetAsync<T>(T cacheItem, AizenCacheOptions? cacheOptions = null, CancellationToken token = default);
Task SetAsync<T>(T cacheItem, string key, AizenCacheOptions? cacheOptions = null, CancellationToken token = default);
Task<bool> ExistsAsync<T>(CancellationToken token = default);
Task<bool> ExistsAsync<T>(string key, CancellationToken token = default);
Task RemoveAsync<T>(CancellationToken token = default);
Task RemoveAsync<T>(string key, CancellationToken token = default);
```

### `IAizenDistributedCache` (extends `IAizenCache`)
Adds low-level Redis operations that bypass the Aizen JSON wrapper:
```csharp
Task<T> GetNoHash<T>(string key);
Task<bool> SetNoHash<T>(string key, T value, TimeSpan ttl);
Task<bool> ExistNoHash(string key);
Task<bool> RemoveNoHash(string key);
Task<AizenStringCacheItem<T>> GetNoHashWitTtl<T>(string key);
Task<dynamic> GetDirectStringToDynamic(string key, CancellationToken token = default);
```

### `AizenCacheOptions`
```csharp
public class AizenCacheOptions {
    public DateTimeOffset? AbsoluteExpiration { get; set; }
    public TimeSpan? SlidingExpiration { get; set; }
    public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
}
```

### `AizenDistributedCache` (implementation)
- Backed by `StackExchange.Redis` and `IDistributedCache`.
- `SetNoHash` uses `Database.StringSetAsync(key, jsonString, ttl)` — precise TTL control.
- `GetNoHash` uses `Database.StringGetAsync(key)` with JSON deserialization.
- `ExistNoHash` uses `Database.KeyExistsAsync(key)`.

### `AizenMemoryCache`
- Backed by `Microsoft.Extensions.Caching.Memory`.
- Not used for service token caching (tokens need Redis durability).

---

## DI Registration

```csharp
services.AddAizenCache(configuration);
// Registers: AizenMemoryCache, AizenDistributedCache, IAizenMemoryCache, IAizenDistributedCache
// Requires: "DistributedCache:Configuration" Redis connection string in appsettings
```

---

## Decision for Keycloak Service Token Caching

### Chosen approach
Use `IAizenDistributedCache.SetNoHash<CachedKeycloakServiceToken>(key, value, ttl)` with explicit `TimeSpan` TTL.

### Reasoning
- `SetNoHash` bypasses the Aizen type-name-based key wrapper, allowing arbitrary explicit cache keys.
- Direct Redis `StringSet` with TTL ensures precise expiry matching `expires_in - CacheSecondsBeforeExpiry`.
- `ExistNoHash(key)` provides O(1) existence check before attempting deserialization.
- Thread-safe for concurrent reads — multiple requests may populate the cache simultaneously in a race, but each sets the same valid token. No stampede protection mechanism (distributed lock / single-flight) was found in the Aizen cache layer.

### Stampede protection note
No distributed lock or `GetOrCreateAsync` semantics were found in the Aizen cache layer. In a race condition at expiry, multiple BFF instances may call Keycloak simultaneously. This is acceptable because:
1. Keycloak `client_credentials` is idempotent.
2. Each call yields a valid token.
3. The risk is low-frequency (only at cache expiry).

**Security follow-up:** Consider adding a distributed lock (e.g., via `SET NX` on a lock key) if high-frequency cache expiry races are observed in production.

---

## Token Value Protection

`IAizenDistributedCache.SetNoHash` serializes to plain JSON. The `CachedKeycloakServiceToken.AccessToken` is stored as a JSON string in Redis.

**No encryption/protection utility** was found in the Aizen cache layer.

**Security follow-up:** Consider encrypting the `AccessToken` field at rest in Redis using a Data Protection key or symmetric encryption. Document this as a future security hardening task.
