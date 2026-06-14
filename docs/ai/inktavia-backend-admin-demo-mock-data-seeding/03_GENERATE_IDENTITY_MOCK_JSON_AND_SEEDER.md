# 03 — Generate Identity Mock JSON and Seeder

Generate Identity mock data only after the audit step is complete.

## JSON files

Place files according to existing convention. If no stronger convention exists, use:

```text
Aizen.Modules.Identity.Repository/Seed/MockData/admin-demo/identity-users.json
Aizen.Modules.Identity.Repository/Seed/MockData/admin-demo/identity-profiles.json
Aizen.Modules.Identity.Repository/Seed/MockData/admin-demo/identity-roles-or-contexts.json
```

Adapt filenames to real entities.

## Seeder

Create module-owned seed/import code such as:

```text
Aizen.Modules.Identity.Repository/Seed/MockData/IdentityMockDataSeeder.cs
```

Adapt to existing DI/startup conventions.

## Rules

- Use existing password hashing/credential seeding conventions.
- Do not store production secrets.
- Use local/dev mock credentials only.
- Insert missing records only.
- Respect required fields and unique constraints.
- Seed AdminPanel-capable identity context if the module supports it.

## Report

Create:

```text
docs/reports/identity-mock-data-seeding-report.md
```
