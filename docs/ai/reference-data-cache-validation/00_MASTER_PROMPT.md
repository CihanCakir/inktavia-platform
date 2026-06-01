# 00 — Master Prompt: ReferenceData Cache and Validation

You are GitHub Copilot Agent working on `Aizen.Modules.ReferenceData`.

Your task is to review and update the ReferenceData module cache usage according to the existing Aizen cache architecture, then validate the module through restore, build, migration, Mongo index, seed, tree, exchange-rate, location DTO and endpoint checks.

## Main goals

```text
1. Analyze existing Core/cache infrastructure.
2. Detect how IAizenQueryHandlerCacheable is used.
3. Detect how IAizenDistributedCache is used.
4. Apply cacheable query handler pattern to suitable ReferenceData queries.
5. Keep manual IAizenDistributedCache only where explicit custom cache checks are required.
6. Ensure cache invalidation is triggered from write commands.
7. Validate restore, build, migration, Mongo index initializer, seed idempotency, lookup tree, MoveLookupGroup, exchange-rate history, location DTO responses and endpoint wiring.
```

## Do not change architectural boundaries

```text
Do not create ReferenceData.Infrastructure.
Do not add map/radius/geo-index/nearby marker logic.
Do not cache commands.
Do not cache mutation handlers.
Do not return Mongo documents from query responses.
```

## Execution order

Run these prompts in order:

```text
01_ANALYZE_CORE_CACHE_AND_EXISTING_PATTERNS.md
02_APPLY_CACHEABLE_QUERY_HANDLER_PATTERN.md
03_REVIEW_MANUAL_CACHE_USAGE.md
04_CACHE_INVALIDATION_AND_KEYS.md
05_RESTORE_BUILD_MIGRATION.md
06_MONGO_SEED_LOOKUP_EXCHANGE_LOCATION_VALIDATION.md
07_CONTROLLER_ENDPOINT_WIRING_VALIDATION.md
08_FINAL_REPORT.md
```

At the end of each step, list files inspected, files changed, decisions made, build status if applicable, and remaining risks.
