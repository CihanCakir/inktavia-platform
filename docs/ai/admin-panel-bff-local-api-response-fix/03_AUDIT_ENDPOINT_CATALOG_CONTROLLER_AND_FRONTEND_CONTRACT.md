# 03 — Audit Endpoint Catalog, Controllers, and Frontend Contract

Read:

```text
docs/admin-web-client/admin-panel-bff-endpoint-catalog.md
Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Controllers/**
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/**
```

Also inspect the active React Admin Web API clients if they exist in the repository.

For each failed endpoint, classify as:

```text
Existing controller route mismatch
Missing BFF endpoint for active module
Missing internal module contract
Future/inactive module endpoint
Test expectation mismatch
Frontend client contract mismatch
```

Generate:

```text
docs/reports/admin-panel-bff-endpoint-coverage-fix-report.md
```
