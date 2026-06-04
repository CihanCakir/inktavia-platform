# Copilot Instructions — FileStorage Module

You are working inside the Inktavia Marine OS repository.

## General architecture rules

- Follow the existing Aizen Framework architecture.
- Inspect `Identity`, `ReferenceData`, `Vessel`, existing MessageBus consumers and RabbitMQ request/response patterns before generating code.
- Do not create `FileStorage.Infrastructure`.
- Use these projects:
  - `Aizen.Modules.FileStorage.Api`
  - `Aizen.Modules.FileStorage.Application`
  - `Aizen.Modules.FileStorage.Abstraction`
  - `Aizen.Modules.FileStorage.Domain`
  - `Aizen.Modules.FileStorage.Repository`
- Keep DTOs, Requests, Enums, Clients and Message contracts in `Aizen.Modules.FileStorage.Abstraction`.
- Keep message contracts under `Aizen.Modules.FileStorage.Abstraction/Message`.
- Keep message consumers under `Aizen.Modules.FileStorage.Api/Consumers`.
- Keep controllers under `Aizen.Modules.FileStorage.Api/Controllers/V1`.
- Keep EF entities and Mongo documents in `Aizen.Modules.FileStorage.Domain`.
- Keep persistence, AWS S3 provider, repositories, services, Redis/cache helpers, seed and DI in `Aizen.Modules.FileStorage.Repository`.
- Every public class/interface must include `DocumentationInfo` using the existing project standard.
- Use `IAizenMessagePublisher` from Application command/query handlers when publishing async messages.
- For request/response consumers use `AizenBaseMessageConsumer<TMessage, TResult>`.
- For fire-and-forget consumers use `AizenBaseMessageConsumer<TMessage>`.
- Use `IAizenCQRSProcessor` inside consumers when consumer work delegates to Application command/query processing.

## Storage decision

Production provider: `AWS S3`.

Optional local provider later: `MinIO`.

Do not implement Azure Blob unless explicitly requested later.

## Module responsibility

FileStorage owns upload sessions, file metadata, S3 bucket/object key management, pre-signed upload/read URLs, private/public access policy, content type validation, extension validation, file size validation, checksum, soft delete, lifecycle status, file owner relation, processing jobs, virus scan state, thumbnail state and rich metadata when required.

## Out of scope

Do not implement Vessel, ServiceProvider, CargoDry, Payment or Profile domain logic inside FileStorage. Other modules only reference `FileId`.

## RemoteCall integration rule

FileStorage synchronous server-to-server calls must use the existing Aizen RemoteCall architecture.

Do not create a new Refit client or custom external client structure.

The agent must inspect and follow the existing pattern based on:

```csharp
using Aizen.Core.RemoteCall.Abstraction;

public interface ISendMailRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallPost("/token")]
    Task<SendLoginMailResponse> SendLoginRequest([AizenRemoteCallBody] SendLoginMailRequest request);

    [AizenRemoteCallPost("/v1/email/transactional/send")]
    Task<SendLoginMailResponse> SendMailRequest(
        [AizenRemoteCallBody] SendMailRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization);
}
```

For FileStorage, create RemoteCall contracts under the current repository convention. Preferred location if no stronger convention exists:

```text
Aizen.Modules.FileStorage.Abstraction/RemoteCall
```

If the repository uses a `Core/RemoteCall` convention, follow that convention instead.

Use RemoteCall for synchronous operations:

```text
GetFileMetadata
ValidateFileOwnership
CreateReadUrl
CreateUploadSession
CompleteUploadSession
LinkFileToOwner
DeleteFile
```

Keep RabbitMQ/Aizen MessageBus for asynchronous operations:

```text
FileUploadedMessage
FileProcessingRequestedMessage
FileLinkedToOwnerMessage
FileDeletedMessage
OrphanFileCleanupRequestedMessage
Virus scan
Thumbnail generation
Metadata extraction
```

# Copilot Instructions — Vessel + FileStorage Integration

You are working inside the Inktavia Marine OS repository.

## Hard rules

- Follow the existing Aizen Framework architecture.
- Do not create `Vessel.Infrastructure`.
- Do not create `FileStorage.Infrastructure`.
- Do not create Refit clients or custom external HTTP clients for FileStorage calls.
- Use the existing `IAizenRemoteCall` pattern for synchronous FileStorage calls.
- Use RabbitMQ/Aizen MessageBus for asynchronous events and background side effects.
- Use `IAizenMessagePublisher` from Application command/query handlers when publishing async messages.
- Use `IAizenInfoAccessor` in Vessel command handlers to read UserInfo, Client and Device data.
- Every public class/interface/command/query/handler/validator/mapping/service/message must include `DocumentationInfo` using the existing project standard.
- Command handlers must return typed DTO/response/bool only. Never return `object`.
- Vessel must not store AWS S3 bucket names, object keys, storage provider internals or permanent signed URLs.
- Vessel may store `FileId` and optional snapshot fields such as original file name, content type and file size.
- Temporary read URLs must be generated by FileStorage on demand.

## Use IAizenRemoteCall for immediate operations

```text
GetFileMetadata
ValidateFileOwnership
CreateReadUrl
LinkFileToOwner
Validate file status
Validate file category/content type
```

## Use MessageBus for async operations

```text
VesselDocumentAddedMessage
VesselDocumentUpdatedMessage
VesselDocumentRemovedMessage
VesselMediaAddedMessage
VesselMediaUpdatedMessage
VesselMediaRemovedMessage
VesselCoverMediaChangedMessage
VesselMediaSortOrderChangedMessage
Audit/notification events
Orphan cleanup requests
```
# Copilot Instructions - Inktavia Marine OS ServiceRequest Module

You are working inside the Inktavia Marine OS repository.

## Language

Use English for generated code comments, documentation summaries, markdown reports, class descriptions, commit-style summaries, and final reports.

## Architectural rules

Follow the existing Aizen/Inktavia architecture. Before generating code, inspect similar modules such as Identity, ReferenceData, Vessel, FileStorage, ProviderOperations if present, and reuse their patterns.

Expected module layers:

```text
Aizen.Modules.ServiceRequest.Api
Aizen.Modules.ServiceRequest.Application
Aizen.Modules.ServiceRequest.Abstraction
Aizen.Modules.ServiceRequest.Domain
Aizen.Modules.ServiceRequest.Repository
```

If the repository uses a slightly different physical folder layout, adapt to the actual repository layout while preserving the same logical layers.

Do not introduce a separate `Infrastructure` project if the existing architecture uses a `Repository` project for persistence and dependency injection.

## Mandatory standards

- Use CQRS for commands and queries.
- Use typed DTO/response returns. Do not return `object` from handlers.
- Use request models under the Abstraction layer for controller input models.
- Use DTOs under the Abstraction layer for all API output models.
- Use enums under the Abstraction layer unless the existing solution has a different enum convention.
- Use FluentValidation if existing modules use it.
- Use `DocumentationInfo` on every public class, interface, command, query, handler, validator, DTO, request, controller, service, repository, and mapping type if the project uses this attribute/metadata pattern.
- Use `IAizenInfoAccessor` or the existing user/client/device accessor pattern for current user, client, tenant, and device context.
- Use `IAizenQueryHandlerCacheable` or the existing cacheable-query contract for cacheable queries.
- Invalidate related cache after write commands.
- Respect current error/result/response wrapper patterns.
- Respect current namespace, folder, project reference, and dependency injection conventions.
- Use `DateTimeOffset` or the project-standard UTC time type consistently. For PostgreSQL `timestamptz`, always use UTC-compatible values.
- Use soft delete/audit conventions from existing base entities.
- Do not use broad unrelated refactoring.
- Do not break existing modules.
- Keep each change focused and buildable.

## Realtime requirement

Inspect and reuse:

```text
Core/Realtime/src/Aizen.Core.Realtime
```

Use the framework's current SignalR abstractions, hubs, publishers, DI extensions, group management, user connection model, serialization, authentication, and authorization approach.

Do not invent a separate SignalR architecture if `Aizen.Core.Realtime` already provides a reusable base.

## ServiceRequest module responsibility

ServiceRequest is the operational lifecycle module for marine service marketplace workflows:

```text
Boat Owner -> Vessel -> Service Request -> Provider Offer -> Assignment -> WorkLog -> Completion -> Owner Approval / Dispute
```

It must cover request, offer, assignment, realtime communication, messages, work execution, completion evidence, dispute flow, and admin operations in the first phase.

# Copilot Instructions - Inktavia Postman Module Docs v2

You are generating Postman documentation and test collections for Inktavia Marine OS.

## Active scope

Generate artifacts for:

- Identity
- ReferenceData
- Vessel
- FileStorage
- ServiceRequest

Skip active test generation for:

- Payment
- Profile

## Do not invent contracts

Request bodies must be derived from real request DTOs, controller action parameters, validators, and command/query contracts.

Response examples and tests must be derived from real response DTOs, action return types, handler return types, or existing captured examples.

If inference is uncertain, mark it clearly in a report instead of silently inventing.

## Auth model

Preserve the existing two-token approach:

- Keycloak `access_token` -> `active_access_token` -> Authorization Bearer token
- Identity `body.token.accessToken` -> `identityAccessToken` and `X-Aizen-User-Token`

Default API request headers:

- Authorization: Bearer `{{active_access_token}}`
- X-Aizen-User-Token: `{{X-Aizen-User-Token}}`

## Local service roots

- Identity: `http://localhost:7101`
- ReferenceData: `http://localhost:7104`
- Vessel: `http://localhost:7105`
- FileStorage: `http://localhost:7106`
- ServiceRequest: `http://localhost:7107`

Create both `*_root_url` and `*_base_url` variables.

## Output convention

Create `docs/postman` under every active module directory.


# Copilot Instructions - Inktavia Marine OS FileStorage Object Storage

You are working inside the Inktavia Marine OS backend repository.

Follow the current repository architecture. Do not introduce unrelated patterns. Prefer the existing AizenFramework conventions for:

- CQRS
- DependencyInjection
- Options binding
- Repository/service abstractions
- DocumentationInfo metadata
- Postman documentation generation
- Build validation reports

Current active modules:

- Identity
- ReferenceData
- Vessel
- FileStorage
- ServiceRequest

Inactive/future modules:

- Payment
- Profile

Do not add active flows depending on Payment or Profile.

Object storage decision:

- Local development and local integration tests must use MinIO.
- Dev/Test/Production cloud environments must use AWS S3-compatible `S3ObjectStorage` settings.
- Secrets must not be committed into the repository.
- AccessKey and SecretKey must be supplied through environment variables, secret managers, or Kubernetes secrets.
- Local MinIO default credentials may be used only in local-only `.env.example` or docker-compose examples.

Do not break existing FileStorage APIs or existing Postman flows.

# Copilot Instructions - Inktavia Marine OS Admin Panel BFF

You are working in the Inktavia Marine OS backend repository.

## Existing target projects

Use the already-created BFF projects:

```text
Bff/src/AdminPanel/Aizen.Bff.AdminPanel
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application
```

Do not create an unrelated BFF architecture. Follow existing AizenFramework conventions in the repository.

## Active modules

The currently active backend modules are:

- Identity
- ReferenceData
- Vessel
- FileStorage
- ServiceRequest

The following modules may exist but must be treated as inactive/future integration for this task:

- Payment
- Profile

Do not generate active Admin Panel BFF flows that depend on Payment or Profile.

## Main implementation rules

1. The Admin Panel BFF must not contain domain business logic.
2. Domain rules remain inside internal modules.
3. The BFF is responsible only for:
   - orchestration
   - aggregation
   - token forwarding
   - response shaping
   - admin-specific DTOs/view models
   - limited caching if the existing architecture supports it
4. Internal module calls must use `AizenRemoteCall` and existing Aizen remote-call conventions.
5. Do not use direct EF Core DbContext access from the BFF.
6. Do not use repositories from internal modules in the BFF.
7. The BFF Application layer may reference active modules' Abstraction class libraries for request/response contracts.
8. Prefer typed command/query responses. Do not return raw `object` unless existing framework contracts absolutely require it.
9. Add `DocumentationInfo` to public classes, commands, queries, handlers, services, controllers, request/response DTOs, and interfaces if this is the existing repository convention.
10. Run build validation and create a final report.

## Authentication forwarding contract

Every Admin BFF request to internal module APIs must forward:

```text
Authorization: Bearer <incoming Keycloak access token>
X-Aizen-User-Token: <incoming identity user token>
```

The existing Postman standard uses:

```text
Authorization: Bearer {{active_access_token}}
X-Aizen-User-Token: {{X-Aizen-User-Token}}
```

The implementation must extract those values from the incoming request context and pass them through every `AizenRemoteCall` request.

## Internal service local endpoints

Use these local defaults in development appsettings:

```text
Identity:        http://localhost:7101/api/v1
ReferenceData:  http://localhost:7104/api/v1
Vessel:         http://localhost:7105/api/v1
FileStorage:    http://localhost:7106/api/v1
ServiceRequest: http://localhost:7107/api/v1
```

Use environment variables for deployable configuration.


# Copilot Instructions - AdminPanel BFF Corrective Fix

You are working inside the Inktavia Marine OS backend repository.

Your task is to correct and complete the AdminPanel BFF implementation under:

```text
Bff/src/AdminPanel/Aizen.Bff.AdminPanel
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application
```

Do not rewrite the entire solution. Apply targeted fixes.

## Non-negotiable rules

1. Re-scan all active module controller files directly from source.
2. Do not rely only on existing endpoint inventory files.
3. Every discovered active endpoint that is relevant to AdminPanel must be represented in AdminPanel BFF.
4. Identity authentication and authorization endpoints must be explicitly included.
5. ReferenceData endpoints must be fully audited and added where missing.
6. Do not activate Payment or Profile module flows.
7. BFF must not contain internal module domain rules.
8. BFF may orchestrate, aggregate, enrich, map and shape AdminPanel-specific responses.
9. Internal API calls must use AizenRemoteCall.
10. Internal API calls must forward:
    - Authorization: Bearer {current Keycloak token}
    - X-Aizen-User-Token: {current Identity user token}
11. Query and QueryHandler must not be in the same file.
12. Command and CommandHandler must not be in the same file.
13. Use typed DTO/response classes. Do not return object.
14. Add DocumentationInfo to all public classes, interfaces, commands, queries, handlers and controllers if this convention exists in the repository.
15. Run build validation and generate reports.

# Current Task Override - AdminPanel BFF Fix Only

For the current Copilot Agent run, focus only on fixing and completing the AdminPanel BFF implementation.

Target projects:

```text
Bff/src/AdminPanel/Aizen.Bff.AdminPanel
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application