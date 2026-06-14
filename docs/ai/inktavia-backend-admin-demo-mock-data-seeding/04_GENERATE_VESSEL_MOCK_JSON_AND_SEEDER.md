# 04 — Generate Vessel Mock JSON and Seeder

Generate Vessel mock data linked to Identity mock UserIds.

## JSON files

Place files according to existing convention. If no stronger convention exists, use:

```text
Aizen.Modules.Vessel.Repository/Seed/MockData/admin-demo/vessels.json
Aizen.Modules.Vessel.Repository/Seed/MockData/admin-demo/vessel-ownerships.json
Aizen.Modules.Vessel.Repository/Seed/MockData/admin-demo/vessel-technical-profiles.json
Aizen.Modules.Vessel.Repository/Seed/MockData/admin-demo/vessel-documents.json
```

Only create files that map to real entities.

## Seeder

Create module-owned seed/import code such as:

```text
Aizen.Modules.Vessel.Repository/Seed/MockData/VesselMockDataSeeder.cs
```

Adapt to existing DI/startup conventions.

## Rules

- Use Identity mock UserIds from the manifest.
- Do not validate Identity DB directly unless the module already has a supported remote validation convention.
- Use stable VesselIds.
- Insert missing records only.
- Respect aggregate/entity constraints.
- Do not seed FileStorage DB from Vessel.

## Report

Create:

```text
docs/reports/vessel-mock-data-seeding-report.md
```
