# 03 — Keycloak Service Token Cache Provider

Implement or update a cache-backed service token provider.

Suggested contract:

```csharp
public interface IAdminPanelBffKeycloakServiceTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
```

Adapt naming and folder placement to repository conventions.

Requirements:

- Use Keycloak `client_credentials` grant server-side.
- Use confidential client `admin-panel-bff`.
- Use Aizen Core/Cache abstraction.
- Cache token until shortly before expiry.
- TTL = `expires_in - CacheSecondsBeforeExpiry`.
- Use token stampede protection if available.
- Never log tokens or secrets.
- Never use raw Identity token in Keycloak cache key.
- Never use raw token values as cache keys.

Suggested cache key:

```text
inktavia:admin-panel-bff:keycloak-service-token:{realmHash}:{clientId}:{audienceOrScopeHash}
```

Suggested report:

```text
docs/reports/admin-panel-bff-cache-backed-keycloak-service-token-report.md
```
