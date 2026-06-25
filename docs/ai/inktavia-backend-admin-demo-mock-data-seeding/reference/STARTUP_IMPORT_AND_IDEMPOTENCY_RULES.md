# Startup Import and Idempotency Rules

## Startup import objective

Each module should import its own mock JSON files into its own database when startup mock seeding is explicitly enabled.

## Configuration guard

Add or reuse configuration:

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

Local/development may explicitly enable:

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

## Production safety

- Never enable mock seed by default in staging/production.
- If `ASPNETCORE_ENVIRONMENT` is not Local/Development, seed must be skipped unless an explicit repository-approved override exists.
- Add clear logs when skipping.

## Idempotency

Seeders must be repeatable:

```text
If ID exists -> skip or update only safe seed-owned fields.
If unique key exists -> skip.
Never duplicate records.
Never delete real data.
Never truncate tables.
```

## Execution order

Because modules are separate services, each module seeds independently.

Recommended startup order for local demo:

```text
1. Identity service starts and seeds Identity mock users/profiles.
2. Vessel service starts and seeds vessels referencing known Identity UserIds.
3. ServiceRequest service starts and seeds requests referencing known Identity UserIds and VesselIds.
```

If a service starts before its related module, this should still be acceptable because cross-module IDs are stable constants. The relationship is logical across services, not enforced through cross-database foreign keys.

## JSON loading

Prefer embedded files or content files depending on existing repo convention.

Acceptable approaches:

```text
CopyToOutputDirectory JSON files loaded by file path
EmbeddedResource JSON files loaded by assembly stream
Existing repository seed file loader
```

Follow the existing convention if one exists.
