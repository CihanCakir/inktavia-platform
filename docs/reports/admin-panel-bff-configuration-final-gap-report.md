# AdminPanel BFF Configuration Final Gap Report

## Report Date
2026-06-10

---

## Scope of This Task

Audit and fix AdminPanel BFF configuration and environment separation for:
- Keycloak service-token caching
- Redis-backed Aizen Core/Cache
- RemoteCalls environment separation
- Cache key environment isolation
- Options startup validation

---

## Issues Found and Fixed

| # | Issue | Severity | Status |
|---|---|---|---|
| 1 | `IAizenDistributedCache` not registered in BFF (missing `AddAizenCache`) | **Critical** | ✅ Fixed |
| 2 | `appsettings.Production.json` contained Identity module content (wrong file) | **Critical** | ✅ Fixed — replaced with correct BFF production config |
| 3 | `DistributedCache` section missing from all BFF appsettings files | **Critical** | ✅ Fixed — added to all 4 environment files |
| 4 | `KeycloakServiceToken` section missing from `Development` and `Local` | **High** | ✅ Fixed — added to both environment files |
| 5 | `appsettings.json` had real localhost URLs (not safe for prod) | **Medium** | ✅ Fixed — replaced with `__FROM_ENV__` placeholders |
| 6 | Cache key had no environment segment (cross-env collision risk) | **Medium** | ✅ Fixed — `IHostEnvironment.EnvironmentName` injected; cache key now includes env |
| 7 | `KeycloakServiceTokenOptions` lacked `[Required]` validation | **Medium** | ✅ Fixed — added `[Required]` on all mandatory fields |
| 8 | `DependencyInjection.cs` used `services.Configure<T>()` — no startup validation | **Medium** | ✅ Fixed — changed to `AddOptions<T>().ValidateDataAnnotations().ValidateOnStart()` |

---

## Files Changed

| File | Change |
|---|---|
| `Application/Common/Options/KeycloakServiceTokenOptions.cs` | Added `[Required]` validation annotations |
| `Application/Common/Services/AdminPanelBffKeycloakServiceTokenProvider.cs` | Injected `IHostEnvironment`, included env in cache key |
| `Application/DependencyInjection.cs` | Added `services.AddAizenCache(configuration)`, switched to `AddOptions<T>().ValidateDataAnnotations().ValidateOnStart()` |
| `configuration/appsettings.json` | Added `DistributedCache` section; changed `RemoteCalls` and `KeycloakServiceToken` URLs to `__FROM_ENV__` |
| `configuration/appsettings.Development.json` | Added `DistributedCache` (Docker Redis) and `KeycloakServiceToken` (Docker Keycloak) sections |
| `configuration/appsettings.Local.json` | Added `DistributedCache` (localhost Redis) and `KeycloakServiceToken` (localhost Keycloak) sections |
| `configuration/appsettings.Production.json` | **Replaced entirely** — removed wrong Identity content; correct BFF production config with `__FROM_ENV__` placeholders |

---

## Configuration Environment Strategy

| Environment | File | Redis | Keycloak |
|---|---|---|---|
| Base (prod-safe placeholder) | `appsettings.json` | `__FROM_ENV__` | `__FROM_ENV__` |
| Docker dev | `appsettings.Development.json` | `redis:6379` | `http://keycloak:8080` |
| Local non-Docker | `appsettings.Local.json` | `localhost:6379` | `http://localhost:8080` |
| Production / Staging | `appsettings.Production.json` + env vars | `__FROM_ENV__` | `__FROM_ENV__` |

---

## Cache Key Format (Post-Fix)

```
inktavia:admin-panel-bff:keycloak-service-token:{environment}:{clientId}:default
```

Examples:
```
inktavia:admin-panel-bff:keycloak-service-token:development:admin-panel-bff:default
inktavia:admin-panel-bff:keycloak-service-token:production:admin-panel-bff:default
inktavia:admin-panel-bff:keycloak-service-token:local:admin-panel-bff:default
```

The `{environment}` comes from `IHostEnvironment.EnvironmentName.ToLowerInvariant()` which equals `ASPNETCORE_ENVIRONMENT`. This is safe — does not contain secrets or tokens.

---

## RemoteCalls Configuration Strategy

| Environment | Source |
|---|---|
| Local dev | `appsettings.Local.json` (localhost ports) |
| Docker dev | `appsettings.Development.json` (service hostnames) |
| Production | `appsettings.Production.json` (Docker service hostnames) + env var overrides |

**Env var override pattern:**
```
RemoteCalls__IIdentityAdminBffRemoteCall__BaseUrl
RemoteCalls__IVesselAdminBffRemoteCall__BaseUrl
RemoteCalls__IFileStorageAdminBffRemoteCall__BaseUrl
RemoteCalls__IServiceRequestAdminBffRemoteCall__BaseUrl
RemoteCalls__IReferenceDataAdminBffRemoteCall__BaseUrl
```

---

## Startup Validation Status

| Check | Mechanism | Status |
|---|---|---|
| `KeycloakServiceToken:Authority` not null | `[Required]` + `ValidateOnStart()` | ✅ |
| `KeycloakServiceToken:TokenEndpoint` not null | `[Required]` + `ValidateOnStart()` | ✅ |
| `KeycloakServiceToken:ClientId` not null | `[Required]` + `ValidateOnStart()` | ✅ |
| `KeycloakServiceToken:ClientSecret` not null/empty | `[Required]` + `ValidateOnStart()` | ✅ (null/empty detected; sentinel `__FROM_SECRET__` not detected) |
| `DistributedCache:Configuration` not null | None yet | ⚠️ Follow-up |
| Sentinel value rejection (`__FROM_ENV__`, `__FROM_SECRET__`) | Not implemented | ⚠️ Follow-up |

---

## Validation Commands and Results

```bash
dotnet restore Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Aizen.Bff.AdminPanel.csproj
# Result: ✅ SUCCESS — All projects up-to-date

dotnet build Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Aizen.Bff.AdminPanel.csproj
# Result: ✅ Build succeeded — 0 Error(s), warnings only (pre-existing in Core packages)

dotnet test
# Result: No test projects found for AdminPanel BFF — no tests run
```

---

## Remaining Gaps

| Gap | Priority | Action |
|---|---|---|
| Sentinel value rejection for `__FROM_SECRET__`/`__FROM_ENV__` in non-dev | High | Add `IValidateOptions<KeycloakServiceTokenOptions>` that rejects sentinel strings outside Development/Local |
| `DistributedCache:Configuration` startup validation | High | Add `IStartupFilter` or custom validator to fail fast on missing Redis config |
| Redis health check | Medium | Add `AddHealthChecks().AddRedis(configuration["DistributedCache:Configuration"])` |
| `MessageBroker.QueueSettings.Password` in Production | Medium | Should be injected via `MessageBroker__QueueSettings__Password` env var |
| No `appsettings.Staging.json` | Low | Create if staging has different topology from Production |
| BFF test coverage | Low | No unit/integration tests exist for `AdminPanelBffKeycloakServiceTokenProvider` |
