# Validation and Reporting Requirements

## Required build checks

Run from repository root:

```bash
dotnet restore
dotnet build --no-incremental
```

Run targeted builds if full solution is too large:

```bash
dotnet build Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest/Aizen.Modules.ServiceRequest.csproj --no-incremental
dotnet build Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Aizen.Bff.AdminPanel.csproj --no-incremental
```

## Smoke tests

With Keycloak, Redis, ServiceRequest module, and AdminPanel BFF running:

- admin token can call ServiceRequest BFF list/detail/history endpoints
- participant/customer-only token receives 403
- no token receives 401
- BFF forwards both service token and user token to ServiceRequest module
- Vessel Detail returns populated `serviceHistory[]` when ServiceRequest records exist
- Vessel Detail still returns `serviceHistory: []` plus warning when ServiceRequest module is unavailable

## Required reports

Generate:

- `docs/reports/servicerequest-bff-contract-audit-report.md`
- `docs/reports/servicerequest-module-query-command-gap-report.md`
- `docs/reports/servicerequest-module-admin-read-model-implementation-report.md`
- `docs/reports/admin-panel-bff-servicerequest-remote-call-report.md`
- `docs/reports/admin-panel-bff-servicerequest-endpoint-implementation-report.md`
- `docs/reports/vessel-detail-service-history-integration-report.md`
- `docs/reports/servicerequest-bff-auth-and-token-forwarding-report.md`
- `docs/reports/servicerequest-bff-smoke-test-report.md`
- `docs/reports/servicerequest-bff-final-gap-report.md`
