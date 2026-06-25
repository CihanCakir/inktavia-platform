# 06 — Wire Startup Import and Environment Guards

Wire mock data seeding into each target module startup using the existing Aizen/module bootstrap convention.

## Configuration

Add or reuse:

```json
{
  "MockData": {
    "Enabled": false,
    "RunOnStartup": false,
    "EnvironmentGuard": ["Local", "Development"],
    "SeedMode": "InsertMissingOnly",
    "DataSet": "admin-demo"
  }
}
```

Local/development files may enable it explicitly.

## Module startup

For each module:

- Register the seeder in DI.
- Run seeder on startup only if enabled and environment is allowed.
- Log seed start/skip/complete with counts.
- Ensure failure is visible in local/dev.
- Avoid running seed during production by default.

## Idempotency

- Repeated startup should not duplicate rows/documents.
- Use stable IDs and insert-missing-only behavior.
- Do not truncate tables.
- Do not delete non-seed data.

## Report

Create:

```text
docs/reports/mock-data-startup-import-report.md
```
