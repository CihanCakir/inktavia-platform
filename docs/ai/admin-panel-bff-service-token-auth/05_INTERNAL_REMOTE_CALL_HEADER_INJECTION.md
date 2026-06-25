# 05 — Internal RemoteCall Header Injection

Update all AdminPanel BFF internal RemoteCall flows.

Every internal API call must send:

```http
Authorization: Bearer <cached-admin-panel-bff-keycloak-service-token>
X-Aizen-User-Token: Bearer <incoming-identityAccessToken>
```

Requirements:

- Use `IAdminPanelBffKeycloakServiceTokenProvider` or equivalent.
- Do not forward incoming browser `Authorization` header.
- Do not require browser Keycloak token.
- Preserve AizenRemoteCall architecture.
- Do not introduce direct EF/repository access in BFF.
- Prefer a shared helper/service for constructing outbound auth headers.

Generate or update:

```text
docs/reports/admin-panel-bff-internal-remote-call-forwarding-report.md
```
