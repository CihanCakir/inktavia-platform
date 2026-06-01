# 05 — Restore, Build and Migration Validation

Run and fix restore/build/migration issues.

## Restore

```bash
dotnet restore
```

Fix package, project reference or namespace issues.

## Build

```bash
dotnet build
```

Fix all compile errors. Pay attention to DocumentationInfo namespace, cache interface namespace, cache options type name, query handler base class, missing usings, circular dependencies, Mongo base class, `AizenDocumentBase` namespace, `ReferenceDataDbContext` base class and UnitOfWork/MiniUow dependencies.

## Migration

Find the repository migration standard and create ReferenceData migration if applicable:

```bash
dotnet ef migrations add InitialReferenceDataSchema --context ReferenceDataDbContext
```

Use the correct startup project and target project.

Validate:

```text
Schema is ref
Tables are created for EF entities
Mongo documents are not included in EF migration
LookupGroup self-reference is configured
LookupItem relation is configured
ExchangeRate indexes are configured
Currency unique indexes are configured
```

## Output

Report restore result, build result, migration command, migration files, issues fixed and remaining manual actions.
