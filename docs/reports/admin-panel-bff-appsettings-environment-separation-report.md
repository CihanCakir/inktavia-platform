# AdminPanel BFF Appsettings Environment Separation Report

## Report Date
2026-06-10

---

## Configuration Files Inspected

| File | Location |
|---|---|
| `appsettings.json` | `Bff/src/AdminPanel/Aizen.Bff.AdminPanel/configuration/appsettings.json` |
| `appsettings.Development.json` | `Bff/src/AdminPanel/Aizen.Bff.AdminPanel/configuration/appsettings.Development.json` |
| `appsettings.Local.json` | `Bff/src/AdminPanel/Aizen.Bff.AdminPanel/configuration/appsettings.Local.json` |
| `appsettings.Production.json` | `Bff/src/AdminPanel/Aizen.Bff.AdminPanel/configuration/appsettings.Production.json` |
| `launchSettings.json` | `Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Properties/launchSettings.json` |
| `DependencyInjection.cs` | `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/DependencyInjection.cs` |
| `Program.cs` | `Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Program.cs` |
| `AizenApplicationBuilder.cs` | `Core/Starter/src/Aizen.Core.Starter/AizenApplicationBuilder.cs` |
| `AizenBffServiceConfiguration.cs` | `Core/Starter/src/Aizen.Core.Starter.Bff/AizenBffServiceConfiguration.cs` |

---

## Issues Found (Pre-Fix)

| Issue | Severity | Description |
|---|---|---|
| `appsettings.Production.json` wrong content | **Critical** | Contained Identity module settings (DatabaseSettings, PushNotification, Firebase, wrong RemoteCalls, wrong TokenOption). This was a copy-paste artefact. |
| `DistributedCache` section missing from all BFF appsettings | **Critical** | `IAizenDistributedCache` requires `DistributedCache:Configuration` Redis connection string |
| `KeycloakServiceToken` missing from `Development` and `Local` | **High** | Dev/Docker and local environments had no Keycloak config |
| `appsettings.json` had real localhost URLs | **Medium** | Base config should use `__FROM_ENV__` placeholders, not concrete URLs |
| `RemoteCalls` BaseUrls were concrete localhost values in base config | **Low** | Should be `__FROM_ENV__` in base, with concrete values in environment-specific files |

---

## Configuration Loading Order (from `AizenApplicationBuilder`)

```
1. appsettings.json                          (base defaults / placeholders)
2. appsettings.{EnvironmentName}.json        (environment override)
3. appsettings.Local.json                    (local dev only, skipped in Docker)
4. Environment variables                     (highest priority)
```

`launchSettings.json` sets `ASPNETCORE_ENVIRONMENT=Development` for local launch profiles.

---

## Environment-Specific Strategy

### `appsettings.json` (base — safe defaults and placeholders)
- `DistributedCache.Configuration` → `"__FROM_ENV__"` (must be overridden)
- `RemoteCalls.*.BaseUrl` → `"__FROM_ENV__"` (must be overridden)
- `KeycloakServiceToken.Authority` → `"__FROM_ENV__"`
- `KeycloakServiceToken.TokenEndpoint` → `"__FROM_ENV__"`
- `KeycloakServiceToken.ClientSecret` → `"__FROM_SECRET__"` (must come from secret manager)

### `appsettings.Development.json` (Docker Compose dev environment)
- `DistributedCache.Configuration` → `"redis:6379,abortConnect=False,defaultDatabase=13"`
- `RemoteCalls` base URLs → Docker service hostnames (`http://identity:7101`, etc.)
- `KeycloakServiceToken.Authority/TokenEndpoint` → `http://keycloak:8080/realms/inktavia-realm`

### `appsettings.Local.json` (developer machine, no Docker)
- `DistributedCache.Configuration` → `"localhost:6379,abortConnect=False,defaultDatabase=13"`
- `RemoteCalls` base URLs → `http://localhost:{port}` (explicit localhost ports)
- `KeycloakServiceToken.Authority/TokenEndpoint` → `http://localhost:8080/realms/inktavia-realm`

### `appsettings.Production.json` (production / staging Docker)
- `DistributedCache.Configuration` → `"__FROM_ENV__"` (override via env var)
- `RemoteCalls` → Docker service hostnames (same as Development)
- `KeycloakServiceToken` → `"__FROM_ENV__"` / `"__FROM_SECRET__"`

---

## Environment Variable Names

| Setting | Environment Variable |
|---|---|
| Redis connection string | `DistributedCache__Configuration` |
| Keycloak authority | `KeycloakServiceToken__Authority` |
| Keycloak token endpoint | `KeycloakServiceToken__TokenEndpoint` |
| Keycloak client ID | `KeycloakServiceToken__ClientId` |
| Keycloak client secret | `KeycloakServiceToken__ClientSecret` |
| Cache seconds before expiry | `KeycloakServiceToken__CacheSecondsBeforeExpiry` |
| Cache key prefix | `KeycloakServiceToken__CacheKeyPrefix` |
| Identity base URL | `RemoteCalls__IIdentityAdminBffRemoteCall__BaseUrl` |
| Vessel base URL | `RemoteCalls__IVesselAdminBffRemoteCall__BaseUrl` |
| FileStorage base URL | `RemoteCalls__IFileStorageAdminBffRemoteCall__BaseUrl` |
| ServiceRequest base URL | `RemoteCalls__IServiceRequestAdminBffRemoteCall__BaseUrl` |
| ReferenceData base URL | `RemoteCalls__IReferenceDataAdminBffRemoteCall__BaseUrl` |
| ElasticAPM server | `ElasticApm__ServerUrl` |

---

## Remaining Gaps

- `appsettings.Production.json` uses Docker service hostnames for `RemoteCalls` (same as `Development`). If the production topology differs, these should be overridden via environment variables.
- `MessageBroker.QueueSettings.Password` in `Production` is currently `"aizenpw"` — should be injected via `MessageBroker__QueueSettings__Password` env var for real production deployments.
- No `appsettings.Staging.json` exists; staging-specific config would rely on environment variables or a separate file.
