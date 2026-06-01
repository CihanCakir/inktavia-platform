# 01 — Analyze Existing Seed and ReferenceData Structure

Do not write code in this step.

## Inspect

Search for:

```text
Seed
Seeder
DataSeed
SeedService
ReferenceDataSeedService
MongoIndexInitializer
DocumentationInfo
ReferenceDataDbContext
ILocationRepository
AizenDocumentBase
LookupGroupEntity
LookupItemEntity
```

Inspect:

```text
Aizen.Modules.ReferenceData.Abstraction
Aizen.Modules.ReferenceData.Domain
Aizen.Modules.ReferenceData.Repository
Aizen.Modules.Identity
Aizen.Modules.InktaviaStore.Repository
```

## Report

Produce a report containing:

```text
1. Existing seed pattern in the repository
2. Existing JSON reading utilities if any
3. Existing DocumentationInfo usage
4. Existing ReferenceData entity/document list
5. Existing repository interfaces
6. Existing ReferenceDataDbContext details
7. Existing Mongo location repository details
8. Missing seed components
9. Recommended implementation plan
```

No code changes in this step.
