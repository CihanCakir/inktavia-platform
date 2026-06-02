# FileStorage Module Guide

## Module name

`Aizen.Modules.FileStorage`

## Projects

```text
Aizen.Modules.FileStorage.Api
Aizen.Modules.FileStorage.Application
Aizen.Modules.FileStorage.Abstraction
Aizen.Modules.FileStorage.Domain
Aizen.Modules.FileStorage.Repository
```

## Data placement

### AWS S3
Stores the binary file/object.

### PostgreSQL
Stores main transactional metadata under schema `file_storage`:

```text
FileEntity
FileVersionEntity
FileAccessPolicyEntity
FileOwnerReferenceEntity
FileUploadSessionEntity
FileProcessingJobEntity
```

### MongoDB optional
Stores flexible/rich metadata:

```text
FileRichMetadataDocument
FileProcessingResultDocument
FileThumbnailMetadataDocument
```

### Redis/Aizen cache
Stores temporary upload session state, signed URL cache, rate-limit state and processing status cache.

## RabbitMQ / MessageBus boundary

Use RabbitMQ/Aizen MessageBus for background/asynchronous operations such as file uploaded events, virus scan request, thumbnail generation request, metadata extraction request, file linked to owner event, orphan cleanup request and processing result callback.

Use HTTP/Refit/FileStorage client for synchronous validation/read operations from modules like Vessel:

```text
GetFileMetadata
ValidateFileOwnership
CreateReadUrl
```

## Message contract location

`Aizen.Modules.FileStorage.Abstraction/Message`

## Consumer location

`Aizen.Modules.FileStorage.Api/Consumers`

## Application publishing

Application command/query handlers may inject:

```csharp
private readonly IAizenMessagePublisher _publisher;
```

and publish:

```csharp
await _publisher.PublishAsync(new FileProcessingRequestedMessage
{
    FileId = fileId,
    ProcessingType = "VirusScan"
}, cancellationToken: cancellationToken);
```

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
