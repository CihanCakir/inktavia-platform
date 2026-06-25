# 10 — Validation, Final Reports, and Rerun

Run:

```bash
dotnet restore
dotnet build
dotnet test
```

If available, rerun the local BFF API test suite and generate a new report directory.

Required final reports:

```text
docs/reports/admin-panel-bff-local-response-fix-summary-report.md
docs/reports/admin-panel-bff-local-api-rerun-report.md
docs/reports/admin-panel-bff-local-response-fix-final-gap-report.md
```

Final report must include:

```text
Original pass/fail count
New pass/fail count
Fixed failures
Reclassified inactive/future failures
Remaining blockers
Exact validation commands
Exact validation results
```

Do not fake success.
