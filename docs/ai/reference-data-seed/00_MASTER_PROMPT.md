# 00 — Master Prompt: ReferenceData JSON Seed Architecture

You are GitHub Copilot Agent. Build a JSON-based seed architecture for `Aizen.Modules.ReferenceData`.

## Main goal

Create seed JSON files, seed model classes and seed services for all ReferenceData entities and documents.

The seed system must:

```text
1. Read JSON files from ReferenceData.Repository/Seed/Json.
2. Deserialize JSON into seed model classes.
3. Map seed models into EF entities or Mongo documents.
4. Insert or update records idempotently.
5. Avoid duplicate records.
6. Support Turkey-first location data.
7. Support Inktavia Marine OS lookup group tree and lookup items.
8. Integrate with DependencyInjection and startup/starter operation.
```

## Execution order

Run these files in order:

```text
01_ANALYZE_EXISTING_SEED_AND_REFERENCEDATA.md
02_CREATE_SEED_FOLDER_AND_JSON_CONVENTION.md
03_CREATE_SEED_MODELS_AND_JSON_READER.md
04_CREATE_EF_ENTITY_JSON_SEEDS.md
05_CREATE_TURKEY_LOCATION_JSON_SEEDS.md
06_CREATE_LOOKUP_TREE_AND_ITEMS_SEEDS.md
07_CREATE_SEED_SERVICES.md
08_DI_STARTUP_OPERATION_INTEGRATION.md
09_VALIDATION_BUILD_AND_REPORT.md
```

## Non-negotiable rules

```text
Do not create ReferenceData.Infrastructure.
Do not seed map/radius/geo-index/nearby discovery data.
Do not duplicate seed records.
Do not return Mongo documents from query handlers.
Do not hardcode large seed data inside C# classes.
Read seed data from JSON files.
Every public class/interface must include DocumentationInfo.
```
