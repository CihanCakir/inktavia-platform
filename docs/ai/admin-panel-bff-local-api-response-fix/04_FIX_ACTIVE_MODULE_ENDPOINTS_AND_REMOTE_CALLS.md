# 04 — Fix Active Module Endpoints and RemoteCalls

For active modules only:

```text
Identity
ReferenceData
Vessel
FileStorage
ServiceRequest
```

Fix or implement missing BFF endpoints required by Admin Web.

Every internal RemoteCall must send:

```http
Authorization: Bearer <admin-panel-bff-keycloak-service-token>
X-Aizen-User-Token: Bearer <identityAccessToken>
```

Do not forward incoming browser `Authorization`.

Debug remote call failures with full module context and response.

Generate:

```text
docs/reports/admin-panel-bff-active-module-remote-call-debug-report.md
```
