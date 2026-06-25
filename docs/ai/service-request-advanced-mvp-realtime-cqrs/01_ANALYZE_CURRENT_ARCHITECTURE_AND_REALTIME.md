# Inktavia Marine OS - ServiceRequest Advanced MVP Realtime CQRS

Use this prompt with GitHub Copilot Agent inside the repository.

All generated implementation must be in English and must follow the existing Aizen/Inktavia architecture.

## Step 01 - Analyze current architecture and realtime framework

Before writing ServiceRequest code, inspect the repository.

## Inspect existing module patterns

Analyze these modules if present:

```text
Aizen.Modules.Identity.*
Aizen.Modules.ReferenceData.*
Aizen.Modules.Vessel.*
Aizen.Modules.FileStorage.*
Aizen.Modules.ProviderOperations.*
```

Identify and document:

- physical folder layout
- project naming convention
- namespace convention
- dependency injection pattern
- Program/Starter/Operation bootstrap pattern
- controller route/versioning pattern
- request/DTO/enum folder layout
- command/query handler pattern
- result/response wrapper pattern
- validator pattern
- repository interface/implementation pattern
- DbContext naming and schema conventions
- EF Core configuration pattern
- migration convention
- Mongo repository/document conventions if used
- Redis/query cache conventions
- `IAizenQueryHandlerCacheable` usage
- `IAizenInfoAccessor` or equivalent current-user accessor usage
- `DocumentationInfo` usage
- authorization policy/role convention
- exception/error code convention

## Inspect realtime framework

Analyze this exact path:

```text
Core/Realtime/src/Aizen.Core.Realtime
```

Document:

- public abstractions
- hub/base hub classes
- publisher/broadcaster services
- connection manager if available
- group naming and group membership patterns
- authentication and authorization behavior
- DI extension methods
- endpoint mapping methods
- event DTO conventions
- serialization settings
- logging conventions
- error handling conventions
- sample usage from existing modules if any

## Output required

Create or update an implementation note file:

```text
ai/service-request-advanced-mvp-realtime-cqrs/reports/01_ARCHITECTURE_AND_REALTIME_ANALYSIS.md
```

Include:

- which existing patterns will be reused
- exact project/folder paths that will be created
- realtime integration approach
- assumptions
- risks/gaps

Do not write ServiceRequest implementation until this analysis is complete.
