# RUN THIS FIRST — AdminPanel BFF Cache-Backed Service Token Auth

You are working inside the Inktavia Marine OS backend repository.

Target projects:

```text
Bff/src/AdminPanel/Aizen.Bff.AdminPanel
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application
```

Read this file first. Then use every file in this package as mandatory context:

```text
manifest.json
reference/*.md
ai/admin-panel-bff-service-token-auth/*.md
```

## Main objective

Update AdminPanel BFF to match the final Inktavia Marine OS security model:

Browser to AdminPanel BFF:

```http
X-Aizen-User-Token: Bearer <identityAccessToken>
```

AdminPanel BFF to internal APIs:

```http
Authorization: Bearer <admin-panel-bff-keycloak-service-token>
X-Aizen-User-Token: Bearer <identityAccessToken>
```

The browser client must not provide or manage Keycloak tokens.

AdminPanel BFF must acquire the Keycloak service token server-side using the confidential Keycloak client `admin-panel-bff`.

The Keycloak service token must be cached using the existing Aizen Framework cache infrastructure under `Core/Cache`, backed by Redis where configured.

## Mandatory correction

Do not store the Keycloak service token as a one-to-one pair with the Identity token.

Reason:

- Keycloak service token belongs to the BFF service identity.
- Identity token belongs to the real user.
- One valid `admin-panel-bff` service token can be reused for many users.

Therefore:

1. Keycloak service-token cache must be based on realm/client/audience/scope.
2. Identity session or token-context cache must be based on user/device/session only where needed.
3. The request pipeline combines both tokens only when calling internal APIs.

## Required implementation

1. Audit existing AdminPanel BFF auth/token/RemoteCall usage.
2. Discover existing Aizen `Core/Cache` abstractions and Redis conventions.
3. Implement cache-backed Keycloak service-token provider.
4. Use cache TTL based on `expires_in - CacheSecondsBeforeExpiry`.
5. Add stampede protection if the cache layer provides lock/single-flight/GetOrCreate semantics.
6. Update all internal RemoteCall header injection to use cached service token plus incoming Identity token.
7. Remove forwarding of incoming browser `Authorization` headers to internal APIs.
8. Refactor inbound BFF auth policy so browser requests can be authenticated by Identity token only.
9. Keep Identity login/refresh endpoints delegated to Identity through AdminPanel BFF.
10. Add device-aware Identity session/cache boundary only if it fits the existing Identity contract.
11. Do not invent endpoints.
12. Do not store raw token values as Redis keys.
13. Do not log tokens or secrets.
14. Generate reports and validate build.

## Required step order

Execute step prompts in this order:

```text
ai/admin-panel-bff-service-token-auth/01_AUDIT_CURRENT_BFF_AUTH_BOUNDARY_AND_CACHE.md
ai/admin-panel-bff-service-token-auth/02_AIZEN_CORE_CACHE_DISCOVERY.md
ai/admin-panel-bff-service-token-auth/03_KEYCLOAK_SERVICE_TOKEN_CACHE_PROVIDER.md
ai/admin-panel-bff-service-token-auth/04_IDENTITY_DEVICE_SESSION_CACHE_BOUNDARY.md
ai/admin-panel-bff-service-token-auth/05_INTERNAL_REMOTE_CALL_HEADER_INJECTION.md
ai/admin-panel-bff-service-token-auth/06_INBOUND_BFF_AUTH_POLICY_AND_CONTROLLERS.md
ai/admin-panel-bff-service-token-auth/07_IDENTITY_AUTH_ENDPOINTS_REFRESH_AND_DEVICE_FLOW.md
ai/admin-panel-bff-service-token-auth/08_CONFIGURATION_ENV_SECRET_HANDLING.md
ai/admin-panel-bff-service-token-auth/09_REALM_POSTMAN_SECURITY_VALIDATION.md
ai/admin-panel-bff-service-token-auth/10_FINAL_REPORTS_AND_BUILD_VALIDATION.md
```

## Required reports

Generate or update:

```text
docs/reports/admin-panel-bff-cache-backed-keycloak-service-token-report.md
docs/reports/admin-panel-bff-identity-device-session-cache-report.md
docs/reports/admin-panel-bff-token-refresh-boundary-report.md
docs/reports/admin-panel-bff-internal-remote-call-forwarding-report.md
docs/reports/admin-panel-bff-final-gap-report.md
```

## Validation

Run:

```bash
dotnet restore
dotnet build
dotnet test
```

If tests are unavailable, state that clearly.

Do not fake success. Document blockers with failing command, project, error summary, and likely fix.
