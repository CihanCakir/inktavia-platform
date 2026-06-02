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
