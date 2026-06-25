# 00 — Master Prompt

You are working inside the Inktavia Marine OS / Aizen backend repository.

Your objective is to generate local/development mock data seeding for Admin Panel testing across:

```text
Identity
Vessel
ServiceRequest
```

## Main rule

Inspect the real code first. Generate mock data only after understanding actual DbContexts, entities, relationships, enums, constructors, validators, seed conventions, DI conventions, and startup conventions.

## Target architecture

Each module owns its own data import:

```text
Identity service imports Identity mock JSON into Identity DB.
Vessel service imports Vessel mock JSON into Vessel DB.
ServiceRequest service imports ServiceRequest mock JSON into ServiceRequest DB.
```

Cross-module relation is logical and ID-based:

```text
Vessel -> Identity UserId
ServiceRequest -> Identity UserId
ServiceRequest -> VesselId
```

No module should directly write into another module's database.

## Required output

Create JSON mock data files and seed/import code in module-owned locations, following existing repository conventions.

Also create:

```text
docs/mock-data/admin-demo/admin-demo-id-manifest.json
```

And reports under:

```text
docs/reports/
```

## Validation

Run:

```bash
dotnet restore
dotnet build
dotnet test
```

If the repository uses specific solution/project commands, use the most appropriate commands and document them.
