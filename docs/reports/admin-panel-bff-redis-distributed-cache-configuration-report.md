# AdminPanel BFF Redis Distributed Cache Configuration Report

## Report Date
2026-06-10

---

## Critical Issue Found and Fixed

**`IAizenDistributedCache` was not registered** in the AdminPanel BFF.

Root cause: `AizenBffServiceConfiguration` (the BFF starter service configuration) does NOT call `services.AddAizenCache(configuration)`, unlike `AizenApiServiceConfiguration` which does.

Fix: Added `services.AddAizenCache(configuration)` to `DependencyInjection.cs` before the Keycloak service token provider registration.

Without this fix, `AdminPanelBffKeycloakServiceTokenProvider` would throw a DI resolution exception at runtime on the first request.

---

## Aizen Core/Cache Registration

### How `AddAizenCache` works

```csharp
// Core/Cache/src/Aizen.Core.Cache/Extention/BuilderExtensions.cs
public static IServiceCollection AddAizenCache(this IServiceCollection services, IConfiguration configuration)
{
    services.Configure<MemoryCacheOptions>(configuration.GetSection("MemoryCache"));
    services.Configure<RedisCacheOptions>(configuration.GetSection("DistributedCache"));
    Connection.RedisConnection = configuration["DistributedCache:Configuration"];

    services.AddSingleton<AizenMemoryCache>();
    services.AddSingleton<AizenDistributedCache>();
    services.AddSingleton(typeof(IMemoryCache), x => x.GetRequiredService<AizenMemoryCache>());
    services.AddSingleton(typeof(IAizenMemoryCache), x => x.GetRequiredService<AizenMemoryCache>());
    services.AddSingleton(typeof(IDistributedCache), x => x.GetRequiredService<AizenDistributedCache>());
    services.AddSingleton(typeof(IAizenDistributedCache), x => x.GetRequiredService<AizenDistributedCache>());
    return services;
}
```

### Registration location
`Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/DependencyInjection.cs`

```csharp
services.AddAizenCache(configuration);
```

---

## Redis Configuration

### Configuration section (`DistributedCache`)

The section is bound to `RedisCacheOptions` (for `IDistributedCache`) and also read directly as `Connection.RedisConnection` (for raw `StackExchange.Redis` operations used by `SetNoHash`/`GetNoHash`).

Required field: `DistributedCache:Configuration`

Optional fields:
- `DistributedCache:InstanceName` — prefix applied to IDistributedCache keys only (not to `SetNoHash`/`GetNoHash` raw Redis operations)

### `InstanceName`

Set to `"AdminPanelBff:"` — this differentiates keys in the `IDistributedCache` namespace from other modules. Note: all existing modules use `"Identity:"` (a copy-paste artefact). BFF correctly uses its own namespace.

**Important:** `SetNoHash`/`GetNoHash` bypass `IDistributedCache` and use the raw `StackExchange.Redis` `IDatabase`. The `InstanceName` prefix is NOT applied to these keys. The Keycloak service token cache key is raw:
```
inktavia:admin-panel-bff:keycloak-service-token:{environment}:{clientId}:default
```

### `defaultDatabase`

Database `13` — consistent with all module convention.

### Redis connection string format

Single node:
```
redis:6379,abortConnect=False,defaultDatabase=13
```

With auth/TLS for production:
```
redis-host:6379,password=<secret>,ssl=True,abortConnect=False,defaultDatabase=13
```

**Secret injection:** `DistributedCache__Configuration` environment variable.

---

## Per-Environment Redis Configuration

| Environment | File | Value |
|---|---|---|
| Base (placeholder) | `appsettings.json` | `"__FROM_ENV__"` |
| Development (Docker) | `appsettings.Development.json` | `"redis:6379,abortConnect=False,defaultDatabase=13"` |
| Local (non-Docker) | `appsettings.Local.json` | `"localhost:6379,abortConnect=False,defaultDatabase=13"` |
| Production | `appsettings.Production.json` | `"__FROM_ENV__"` (injected via `DistributedCache__Configuration` env var) |

---

## Cache Key Strategy

Keys for the Keycloak service token:
```
{CacheKeyPrefix}:{environment}:{clientId}:default
```
Example (development):
```
inktavia:admin-panel-bff:keycloak-service-token:development:admin-panel-bff:default
```

The `{environment}` segment (lowercased `ASPNETCORE_ENVIRONMENT`) is injected via `IHostEnvironment` into the provider. This prevents cross-environment key collisions when multiple environments share the same Redis instance.

---

## Remaining Gaps

- Redis TLS/password for production is not documented in any config file — must be injected via `DistributedCache__Configuration` env var with the full connection string.
- No Redis health check registered — consider adding `AddHealthChecks().AddRedis(...)` for production readiness probes.
