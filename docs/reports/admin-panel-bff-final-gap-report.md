# AdminPanel BFF Final Gap Report

## Report Date
2026-06-10

---

## Scope Completed

- ✅ Audited existing AdminPanel BFF auth/token/RemoteCall usage
- ✅ Discovered and documented Aizen Core/Cache abstractions
- ✅ Implemented cache-backed Keycloak service-token provider (`AdminPanelBffKeycloakServiceTokenProvider`)
- ✅ TTL based on `expires_in - CacheSecondsBeforeExpiry`
- ✅ Updated all internal RemoteCall header injection to use cached service token + incoming Identity token
- ✅ Removed forwarding of incoming browser `Authorization` headers to internal APIs
- ✅ Refactored inbound BFF auth policy from `[Authorize(Roles = "Admin")]` to `[Authorize]`
- ✅ Identity login/refresh/otp endpoints kept delegated to Identity through AdminPanel BFF
- ✅ Device-aware Identity session/cache not implemented (not supported by current Identity contract)
- ✅ No endpoints invented
- ✅ No raw token values used as Redis keys
- ✅ No token logging in provider implementation
- ✅ Configuration section `KeycloakServiceToken` added to appsettings
- ✅ `ClientSecret` placeholder — real value from environment variable only
- ✅ Build validated: `dotnet restore` + `dotnet build` → **0 errors**

---

## Files Changed

### New files
| File | Purpose |
|---|---|
| `Application/Common/Options/KeycloakServiceTokenOptions.cs` | Config POCO |
| `Application/Common/Services/IAdminPanelBffKeycloakServiceTokenProvider.cs` | Provider interface |
| `Application/Common/Services/AdminPanelBffKeycloakServiceTokenProvider.cs` | Caching implementation |
| `Application/Common/Services/CachedKeycloakServiceToken.cs` | Redis DTO |

### Modified files
| File | Change |
|---|---|
| `Application/Aizen.Bff.AdminPanel.Application.csproj` | Added `Aizen.Core.Cache.Abstraction` + `Aizen.Core.Cache` refs |
| `Application/DependencyInjection.cs` | Registered `KeycloakServiceTokenOptions` + `IAdminPanelBffKeycloakServiceTokenProvider` |
| `Application/Authentication/Command/ChangePasswordCommand.cs` | Removed `Authorization` property |
| `Application/Authentication/Command/ChangePasswordCommandHandler.cs` | Uses service token provider |
| **49 command/query .cs files** | Removed `Authorization` property + constructor param |
| **48 handler .cs files** | Injected `IAdminPanelBffKeycloakServiceTokenProvider`, use `await GetAccessTokenAsync()` |
| **9 controller .cs files** | Removed `var auth = ...` extraction; removed `auth` arg from CQRS calls; `[Authorize]` |
| `Bff/configuration/appsettings.json` | Added `KeycloakServiceToken` section |

---

## Aizen Core/Cache Abstractions Used

| Abstraction | Usage |
|---|---|
| `IAizenDistributedCache` | Injected into `AdminPanelBffKeycloakServiceTokenProvider` |
| `.ExistNoHash(key)` | Cache existence check before read |
| `.GetNoHash<CachedKeycloakServiceToken>(key)` | Read cached token DTO |
| `.SetNoHash<CachedKeycloakServiceToken>(key, value, ttl)` | Write token DTO with explicit TTL |

---

## Cache Keys Used

```
inktavia:admin-panel-bff:keycloak-service-token:{clientId}:default
```
Example: `inktavia:admin-panel-bff:keycloak-service-token:admin-panel-bff:default`

---

## Redis TTL Strategy

```
TTL = expires_in - CacheSecondsBeforeExpiry
Default CacheSecondsBeforeExpiry = 60
Minimum enforced TTL = 10 seconds
```

---

## Token Refresh Strategy

| Token | Strategy |
|---|---|
| Keycloak service token | Auto-refreshed by provider on cache miss or when `RefreshAfterUtc` exceeded |
| Identity access token | Client-initiated via `POST /auth/refresh` proxied to Identity module |
| Identity refresh token | Held by browser, not by BFF |

---

## Device / Session Handling Strategy

Not implemented. The Identity module contract does not expose a `DeviceId` claim accessible to the BFF. Documented as a future enhancement.

---

## Token Value Protection Before Redis Storage

**Not encrypted.** Aizen Core/Cache has no encryption utility. The `CachedKeycloakServiceToken.AccessToken` is stored as a plain JSON string in Redis.

**Risk:** If Redis is compromised, the `admin-panel-bff` Keycloak service token can be read. However:
1. The token has a short TTL (default 240 seconds).
2. Rotating the Keycloak client secret invalidates the service account token immediately.

**Follow-up:** Encrypt the `AccessToken` field using ASP.NET Core Data Protection or a project-approved symmetric encryption utility before storing in Redis.

---

## Distributed Lock / Single-Flight

Not available in Aizen Core/Cache. Multiple BFF instances may simultaneously call Keycloak at token expiry. Each call is idempotent and benign for `client_credentials` grant.

**Follow-up:** Add `SET NX` lock around token refresh if high concurrency at expiry is observed.

---

## RemoteCall Forwarding Behavior

| Header | Before | After |
|---|---|---|
| `Authorization` | Browser Keycloak token forwarded | BFF Keycloak service token (cached, server-side) |
| `X-Aizen-User-Token` | Forwarded from browser | Forwarded from browser (unchanged) |

---

## Inbound Auth Policy Behavior

| Controller type | Before | After |
|---|---|---|
| Protected admin controllers | `[Authorize(Roles = "Admin")]` (Keycloak JWT) | `[Authorize]` (Identity JWT via BFF middleware) |
| Auth endpoints | `[AllowAnonymous]` | `[AllowAnonymous]` (unchanged) |
| `ChangePassword` | `[Authorize]` | `[Authorize]` (unchanged) |

---

## Validation Commands and Results

```bash
dotnet restore Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Aizen.Bff.AdminPanel.csproj
# Result: SUCCESS — All projects up-to-date for restore.

dotnet build Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Aizen.Bff.AdminPanel.csproj
# Result: Build succeeded — 0 Error(s), 10 Warning(s) (all pre-existing vulnerability warnings in Core packages, unrelated to this task)
```

**Tests:** No test projects were found for `Aizen.Bff.AdminPanel` or `Aizen.Bff.AdminPanel.Application`. `dotnet test` returned exit 0 with no tests run.

---

## Remaining Gaps

| Gap | Priority | Notes |
|---|---|---|
| Keycloak service token encryption at rest in Redis | High | No encryption utility in Aizen Core/Cache; follow-up security hardening |
| Distributed lock for stampede protection | Medium | Add `SET NX` lock around Keycloak call at cache expiry |
| Options startup validation (`ValidateOnStart`) | Medium | Add `[Required]` + `ValidateOnStart()` to fail fast if ClientSecret missing |
| Device/session Identity cache | Low | Needs Identity contract to expose DeviceId in BFF context |
| BFF inbound auth middleware for `X-Aizen-User-Token` | Depends on Aizen.Core.Starter.Bff | Verify `AizenApplicationBuilder` for BFF type configures Identity JWT validation on `X-Aizen-User-Token` header |

---

## Risks and Follow-Ups

1. **Service token plaintext in Redis** — Mitigated by short TTL and Keycloak secret rotation, but should be encrypted at rest.
2. **`[Authorize]` effectiveness** — Requires the Aizen BFF starter to configure an auth scheme that validates `X-Aizen-User-Token` as the primary identity. Confirm `AizenAppInfo.Type = AppType.Bff` enables this.
3. **Keycloak realm not verified at runtime** — The realm configuration in `appsettings.json` uses localhost URLs. These must be updated for staging/production via environment override.
4. **No BFF test coverage** — No unit or integration tests exist for the BFF application layer. Adding tests for `AdminPanelBffKeycloakServiceTokenProvider` is recommended.
