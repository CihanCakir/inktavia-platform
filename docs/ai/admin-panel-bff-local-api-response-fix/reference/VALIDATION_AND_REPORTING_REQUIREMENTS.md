# Validation and Reporting Requirements

Run validation:

```bash
dotnet restore
dotnet build
dotnet test
```

If there is a local BFF API test runner, run it again after fixes and generate a new report directory.

Required reports under `docs/reports/`:

```text
admin-panel-bff-local-response-fix-summary-report.md
admin-panel-bff-local-failure-classification-report.md
admin-panel-bff-auth-scheme-and-admin-policy-fix-report.md
admin-panel-bff-endpoint-coverage-fix-report.md
admin-panel-bff-active-module-remote-call-debug-report.md
admin-panel-bff-future-inactive-endpoint-decision-report.md
admin-panel-bff-test-runner-postman-alignment-report.md
admin-panel-bff-local-api-rerun-report.md
admin-panel-bff-local-response-fix-final-gap-report.md
```

Every report must include:

```text
Scope
Files inspected
Files changed
Failures fixed
Failures intentionally reclassified
Validation commands
Validation results
Remaining gaps
Follow-ups
```
