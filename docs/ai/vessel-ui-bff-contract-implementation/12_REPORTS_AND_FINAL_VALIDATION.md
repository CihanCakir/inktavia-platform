# 12 — Reports and Final Validation

Run:

```bash
dotnet restore
dotnet build
dotnet test
```

Create final reports:

```text
docs/reports/vessel-ui-contract-audit-report.md
docs/reports/vessel-module-entity-and-migration-plan-report.md
docs/reports/vessel-module-application-query-command-report.md
docs/reports/admin-panel-bff-vessel-endpoint-implementation-report.md
docs/reports/vessel-cross-module-integration-report.md
docs/reports/vessel-ui-bff-smoke-test-report.md
docs/reports/vessel-ui-bff-final-gap-report.md
```

Final gap report must include:

- completed MVP endpoints
- Post-MVP endpoints left as 501 or feature-gated
- migrations generated
- fields skipped and why
- CargoDry/ServiceRequest join status
- Refit serialization risks eliminated
- build/test/smoke test result
- exact next actions

Do not claim completion if build fails.
