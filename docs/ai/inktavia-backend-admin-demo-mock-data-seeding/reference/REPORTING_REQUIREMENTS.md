# Reporting Requirements

Generate reports under:

```text
docs/reports/
```

Required reports:

```text
mock-data-module-entity-audit-report.md
mock-data-cross-module-id-manifest-report.md
identity-mock-data-seeding-report.md
vessel-mock-data-seeding-report.md
service-request-mock-data-seeding-report.md
mock-data-startup-import-report.md
admin-panel-demo-data-validation-report.md
mock-data-final-gap-report.md
```

Each report must include:

```text
Scope
Files inspected
DbContexts discovered
Entities discovered
Relationships discovered
JSON files created
Seeder classes created
Startup wiring changes
Configuration changes
Cross-module ID strategy
Idempotency strategy
Validation commands
Validation results
Remaining gaps
Risks / follow-ups
```

Do not fake validation success.

If a module cannot be seeded due to missing constructors/required data/reference data, document the blocker and implement the safest partial seed only.
