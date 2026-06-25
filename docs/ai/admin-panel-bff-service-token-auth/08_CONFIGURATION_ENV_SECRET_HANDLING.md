# 08 — Configuration, Environment and Secret Handling

Add or update configuration for Keycloak service token and internal services.

Suggested options:

```json
{
  "KeycloakServiceToken": {
    "Authority": "http://localhost:8080/realms/inktavia-realm",
    "TokenEndpoint": "http://localhost:8080/realms/inktavia-realm/protocol/openid-connect/token",
    "ClientId": "admin-panel-bff",
    "ClientSecret": "__FROM_SECRET__",
    "CacheSecondsBeforeExpiry": 60,
    "CacheKeyPrefix": "inktavia:admin-panel-bff:keycloak-service-token"
  }
}
```

Rules:

- Do not commit real client secrets.
- Bind options using existing Options pattern.
- Validate required values if the repository has startup validation conventions.
- Use environment variable `KeycloakServiceToken__ClientSecret` for local/dev secret injection.
- Document appsettings and env usage.

Generate or update:

```text
docs/reports/admin-panel-bff-configuration-and-secret-handling-report.md
```
