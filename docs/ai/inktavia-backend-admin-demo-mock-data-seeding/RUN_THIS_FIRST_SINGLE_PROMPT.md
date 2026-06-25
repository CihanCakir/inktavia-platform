# RUN THIS FIRST — Inktavia Backend Admin Demo Mock Data Seeding

You are working inside the Inktavia Marine OS / Aizen backend repository.

Your task is to inspect the real backend modules and generate realistic mock/demo data for Admin Panel testing.

Target modules:

```text
Identity
Vessel
ServiceRequest
```

The Admin Panel designs are now ready, but the application needs relationally consistent data that can be used to view and test Identity, Vessel, and ServiceRequest screens.

## Mandatory package references

Read and follow:

```text
docs/ai/inktavia-backend-admin-demo-mock-data-seeding/manifest.json
```

Then read every file under:

```text
docs/ai/inktavia-backend-admin-demo-mock-data-seeding/reference/
```

Then execute step prompts in order:

```text
docs/ai/inktavia-backend-admin-demo-mock-data-seeding/ai/inktavia-backend-admin-demo-mock-data-seeding/00_MASTER_PROMPT.md
docs/ai/inktavia-backend-admin-demo-mock-data-seeding/ai/inktavia-backend-admin-demo-mock-data-seeding/01_AUDIT_MODULE_DB_CONTEXTS_AND_ENTITIES.md
docs/ai/inktavia-backend-admin-demo-mock-data-seeding/ai/inktavia-backend-admin-demo-mock-data-seeding/02_DEFINE_CROSS_MODULE_STABLE_IDS_AND_MANIFEST.md
docs/ai/inktavia-backend-admin-demo-mock-data-seeding/ai/inktavia-backend-admin-demo-mock-data-seeding/03_GENERATE_IDENTITY_MOCK_JSON_AND_SEEDER.md
docs/ai/inktavia-backend-admin-demo-mock-data-seeding/ai/inktavia-backend-admin-demo-mock-data-seeding/04_GENERATE_VESSEL_MOCK_JSON_AND_SEEDER.md
docs/ai/inktavia-backend-admin-demo-mock-data-seeding/ai/inktavia-backend-admin-demo-mock-data-seeding/05_GENERATE_SERVICE_REQUEST_MOCK_JSON_AND_SEEDER.md
docs/ai/inktavia-backend-admin-demo-mock-data-seeding/ai/inktavia-backend-admin-demo-mock-data-seeding/06_WIRE_STARTUP_IMPORT_AND_ENV_GUARDS.md
docs/ai/inktavia-backend-admin-demo-mock-data-seeding/ai/inktavia-backend-admin-demo-mock-data-seeding/07_VALIDATE_ADMIN_PANEL_BFF_AND_POSTMAN_SCENARIOS.md
docs/ai/inktavia-backend-admin-demo-mock-data-seeding/ai/inktavia-backend-admin-demo-mock-data-seeding/08_REPORTS_AND_FINAL_GAP_ANALYSIS.md
```

If this package is placed under a different path, adapt paths accordingly.

## Critical architecture rules

1. Each module is a separate service and owns its own database.
2. Do not write from one module into another module's database.
3. Identity owns users and user/profile/context data.
4. Vessel and ServiceRequest must reference Identity users through stable `UserId` values.
5. Generate deterministic mock data IDs so startup imports are idempotent.
6. Use JSON files as the source of mock data.
7. On service startup, when mock seeding is enabled, deserialize the JSON files and import records into the corresponding database entities.
8. Inspect each module's real DbContext, entities, base entity conventions, value objects, enums, validators, and existing seed patterns before creating data.
9. Do not invent fields that are not present in the real domain model.
10. Do not activate Payment/Profile/future modules.
11. Do not introduce a new unrelated seed architecture if the repository already has a seed/import convention.
12. Add `DocumentationInfo` to new public classes if this convention exists.
13. Mock data seeding must be disabled by default outside Local/Development.
14. Generate detailed reports under `docs/reports/`.

## Expected data scenario

Create a coherent marine operations demo dataset for Admin Panel testing:

- Admin users for AdminPanel viewing/testing
- Boat-owner/customer-like users
- Provider/operator-like users only if supported by the existing Identity model
- Multiple vessels with ownership/technical/document/location data where supported
- Service requests across different lifecycle states:
  - draft/new
  - submitted/open
  - offer received
  - assigned
  - in progress
  - waiting for completion approval
  - completed
  - disputed
  - cancelled
- Work logs, messages, status history, offers, assignments, completion evidence, disputes, and attachments only where the entities exist in the real ServiceRequest module.

## Required output shape

Prefer module-local structure if no existing convention is stronger:

```text
Aizen.Modules.Identity.Repository/Seed/MockData/*.json
Aizen.Modules.Identity.Repository/Seed/MockData/*Seeder*.cs

Aizen.Modules.Vessel.Repository/Seed/MockData/*.json
Aizen.Modules.Vessel.Repository/Seed/MockData/*Seeder*.cs

Aizen.Modules.ServiceRequest.Repository/Seed/MockData/*.json
Aizen.Modules.ServiceRequest.Repository/Seed/MockData/*Seeder*.cs
```

Also create a cross-module manifest if useful:

```text
docs/mock-data/admin-demo/admin-demo-id-manifest.json
```

This manifest should document stable IDs used across modules, especially Identity UserIds referenced by Vessel and ServiceRequest.

## Required configuration

Add or adapt configuration such as:

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

Local/development config may enable it explicitly:

```json
{
  "MockData": {
    "Enabled": true,
    "RunOnStartup": true,
    "EnvironmentGuard": ["Local", "Development"],
    "SeedMode": "InsertMissingOnly",
    "DataSet": "admin-demo"
  }
}
```

Do not enable mock data import in staging/production unless explicitly configured.

## Validation

Run the appropriate validation commands:

```bash
dotnet restore
dotnet build
dotnet test
```

If no tests exist, state that clearly.

Do not fake success. Report blockers clearly.

## Required reports

Generate:

```text
docs/reports/mock-data-module-entity-audit-report.md
docs/reports/mock-data-cross-module-id-manifest-report.md
docs/reports/identity-mock-data-seeding-report.md
docs/reports/vessel-mock-data-seeding-report.md
docs/reports/service-request-mock-data-seeding-report.md
docs/reports/mock-data-startup-import-report.md
docs/reports/admin-panel-demo-data-validation-report.md
docs/reports/mock-data-final-gap-report.md
```
