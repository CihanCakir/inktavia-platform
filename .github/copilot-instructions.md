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
