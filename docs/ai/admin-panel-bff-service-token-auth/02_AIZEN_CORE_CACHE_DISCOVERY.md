# 02 — Aizen Core/Cache Discovery

Inspect:

```text
Core/Cache
Core/Cache/src
Aizen.Core.Cache
```

Also search for:

```text
IAizenQueryHandlerCacheable
Redis
DistributedCache
GetOrCreateAsync
Lock
SingleFlight
CacheKey
CacheOptions
DependencyInjection
```

Document the exact cache abstractions and DI patterns discovered.

Then decide the smallest repository-consistent way to cache Keycloak service tokens.

Do not create a custom Redis client if existing Aizen cache abstractions exist.

Generate or update:

```text
docs/reports/admin-panel-bff-aizen-core-cache-discovery-report.md
```
