# 07 — Validate Admin Panel BFF and Postman Scenarios

Validate the generated mock data from an Admin Panel testing perspective.

## Build validation

Run:

```bash
dotnet restore
dotnet build
dotnet test
```

Use project-specific commands if needed.

## Data validation

Where possible, verify:

- Identity mock users/profiles inserted.
- Vessel records reference existing Identity mock UserIds from manifest.
- ServiceRequest records reference existing VesselIds and Identity UserIds from manifest.
- Re-running seed does not duplicate records.
- AdminPanel BFF endpoints can retrieve seeded data after all services are started.

## Postman/Admin Panel checks

If existing Postman collection exists, document sample requests to verify:

```text
Auth login
Identity profile list/detail
Vessel list/detail
ServiceRequest list/detail/timeline
```

Do not invent missing endpoints.

## Report

Create:

```text
docs/reports/admin-panel-demo-data-validation-report.md
```
