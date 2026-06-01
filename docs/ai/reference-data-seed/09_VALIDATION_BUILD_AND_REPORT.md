# 09 — Validation, Build and Final Report

Validate the ReferenceData JSON seed implementation.

## Run

```bash
dotnet restore
dotnet build
```

Fix all errors.

## Validate seed data

Check:

```text
[ ] currencies.json deserializes
[ ] measurement-units.json deserializes
[ ] lookup-groups.json deserializes
[ ] lookup-items.json deserializes
[ ] system files deserialize
[ ] Turkey country.json deserializes
[ ] Turkey cities.json deserializes
[ ] district files are discovered recursively
[ ] neighborhood files are discovered recursively
[ ] street files are discovered recursively
```

## Validate idempotency

Run seed twice and confirm no duplicates for all EF entities and Mongo documents.

## Validate lookup tree

Confirm:

```text
[ ] ParentCode is resolved
[ ] ParentLookupGroupId is set
[ ] Level is calculated
[ ] HierarchyPath is calculated
[ ] Root groups have Level = 0
[ ] Children have Level = parent.Level + 1
```

## Validate no GeoDiscovery pollution

Confirm ReferenceData seed does not contain:

```text
nearby providers
nearby yacht owners
map markers
radius query data
geo-index discovery models
live location data
```

## Final report

Produce:

```text
1. Created seed JSON files
2. Created seed model classes
3. Created seed reader classes
4. Created seed service classes
5. Created DI registrations
6. Startup/Operation integration
7. Turkey location dataset status
8. Lookup tree seed summary
9. Idempotency validation result
10. dotnet restore result
11. dotnet build result
12. Remaining manual tasks
```

## Turkey location dataset status

If only schema/sample data was created, report clearly:

```text
The full official Turkey city/district/neighborhood/street dataset is not included.
The seed architecture and JSON schema are ready.
Import the official dataset into the generated JSON structure before production use.
```
