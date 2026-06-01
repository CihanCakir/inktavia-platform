# Copilot Instructions — ReferenceData Cache and Validation

You are working inside the Inktavia Marine OS repository.

## Core rules

- Preserve the existing Aizen Framework architecture.
- Do not introduce `ReferenceData.Infrastructure`.
- Keep persistence, EF Core, Mongo, seed, repository services and DI under `Aizen.Modules.ReferenceData.Repository`.
- Keep enums, DTOs and request contracts under `Aizen.Modules.ReferenceData.Abstraction`.
- Keep service interfaces under `Aizen.Modules.ReferenceData.Domain/Interface/Service`.
- Keep repository interfaces under the existing Domain interface convention.
- Keep application commands and queries grouped under Currency, ExchangeRate, Lookup, Measurement, Location and SystemParameter.
- Every public class and interface must include `DocumentationInfo`, using the existing project standard if one exists.
- Do not add map discovery, nearby yacht owners, radius query, geo-index, marker or live location logic to ReferenceData.

## Cache-specific rules

- Inspect the existing Core/cache service before changing ReferenceData cache logic.
- Search for `IAizenQueryHandlerCacheable`, `IAizenDistributedCache`, `MetropolCacheType`, `MetropolCacheOptions`, `AizenQueryHandler`, `MetropolQueryHandler`, `CacheOptions`, and `CacheType`.
- Use `IAizenQueryHandlerCacheable` for query handlers that are safely cacheable.
- Use manual `IAizenDistributedCache` only where explicit token/key/data validation or custom invalidation behavior is required.
- Do not cache commands, write operations, seed operations or Mongo index operations.
- Cache only stable or read-heavy ReferenceData queries.

## Required validation items

After implementation, validate:

```text
dotnet restore
dotnet build
ReferenceDataDbContext migration
Mongo index initializer execution
Seed service idempotency
LookupGroup tree correctness
MoveLookupGroup cycle prevention
ExchangeRate update creates history
Location queries return DTOs, not Mongo documents
Cache invalidation is triggered correctly
Controller endpoints are wired to commands/queries
```
