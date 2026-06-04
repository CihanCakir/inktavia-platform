# 03 - Fix CQRS File Structure

Find and fix incorrectly structured Application files.

## Required pattern for queries

```text
Application/<BusinessArea>/Query/<FeatureName>/<FeatureName>Query.cs
Application/<BusinessArea>/Query/<FeatureName>/<FeatureName>QueryHandler.cs
Application/<BusinessArea>/Query/<FeatureName>/<FeatureName>Response.cs
Application/<BusinessArea>/Query/<FeatureName>/<FeatureName>Validator.cs
```

## Required pattern for commands

```text
Application/<BusinessArea>/Command/<FeatureName>/<FeatureName>Command.cs
Application/<BusinessArea>/Command/<FeatureName>/<FeatureName>CommandHandler.cs
Application/<BusinessArea>/Command/<FeatureName>/<FeatureName>Response.cs
Application/<BusinessArea>/Command/<FeatureName>/<FeatureName>Validator.cs
```

Rules:

- Query and QueryHandler must be separate files.
- Command and CommandHandler must be separate files.
- Use one feature folder per use case.
- Keep typed response DTOs.
- Do not use `object` returns.
- Update namespaces and DI registrations.
- Keep existing business logic behavior while only improving structure.

Generate:

```text
Bff/src/AdminPanel/docs/admin-panel-bff-cqrs-structure-fix-report.md
```
