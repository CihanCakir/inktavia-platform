# Inktavia Marine OS - ServiceRequest Advanced MVP Realtime CQRS

Use this prompt with GitHub Copilot Agent inside the repository.

All generated implementation must be in English and must follow the existing Aizen/Inktavia architecture.

## Step 02 - Create ServiceRequest module structure

Create the ServiceRequest module following the existing repository conventions discovered in Step 01.

## Expected logical projects

```text
Aizen.Modules.ServiceRequest.Api
Aizen.Modules.ServiceRequest.Application
Aizen.Modules.ServiceRequest.Abstraction
Aizen.Modules.ServiceRequest.Domain
Aizen.Modules.ServiceRequest.Repository
```

If the repository uses a physical module folder convention, place the projects accordingly.

## Required project references

Wire project references based on the existing architecture. Typical direction:

```text
Api -> Application -> Abstraction + Domain + Repository contracts as needed
Repository -> Domain + Abstraction + Aizen Core data packages
Domain -> Abstraction if existing modules do this, otherwise keep Domain independent
```

Adapt to actual project standards.

## Required dependency injection

Implement module registration matching existing style. Examples of expected registration areas:

- Application services
- MediatR handlers or equivalent CQRS handlers
- Validators
- Repository services
- DbContext
- Mongo repositories/documents if used
- Redis/cache services if used
- Realtime publishers/subscribers
- API controller assembly registration if required

## Required folder structure

Create folders according to actual repository convention. Logical minimum:

```text
Abstraction/
  Dto/
  Enum/
  Request/
  Response/
  Realtime/
  Constants/

Domain/
  Entities/
  Events/
  ValueObjects/
  Rules/

Repository/
  Context/
  Configurations/
  Repositories/
  Services/
  Mongo/
  Cache/
  DependencyInjection/

Application/
  Commands/
  Queries/
  Services/
  Mapping/
  Validators/
  Realtime/

Api/
  Controllers/V1/
  DependencyInjection/
```

## Output required

Create/update:

```text
ai/service-request-advanced-mvp-realtime-cqrs/reports/02_MODULE_STRUCTURE_REPORT.md
```

Include created projects, paths, references, and DI registrations.

Do not generate business-heavy code yet. First ensure the module skeleton is consistent and buildable.
