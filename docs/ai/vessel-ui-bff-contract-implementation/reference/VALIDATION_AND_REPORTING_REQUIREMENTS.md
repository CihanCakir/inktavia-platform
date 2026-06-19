# Validation and Reporting Requirements

## Commands

Run, if available:

```bash
dotnet restore
dotnet build
dotnet test
```

Then run BFF smoke tests for the MVP GET endpoints.

## Reports to create

Create or update:

```text
docs/reports/vessel-ui-contract-audit-report.md
docs/reports/vessel-module-entity-and-migration-plan-report.md
docs/reports/vessel-module-application-query-command-report.md
docs/reports/admin-panel-bff-vessel-endpoint-implementation-report.md
docs/reports/vessel-cross-module-integration-report.md
docs/reports/vessel-ui-bff-smoke-test-report.md
docs/reports/vessel-ui-bff-final-gap-report.md
```

## Report content

Every report must include:

- files inspected
- files created/modified
- routes added/updated
- request/response DTOs added
- query/command changes
- entity fields added/skipped
- migrations added/skipped
- build result
- smoke test result
- remaining gaps
- exact reason for any 501 or null-safe fallback

Do not write “completed” unless build and MVP smoke tests are executed or explicitly blocked by a documented missing service.
