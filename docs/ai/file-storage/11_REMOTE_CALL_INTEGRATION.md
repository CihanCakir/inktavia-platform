# 11 — RemoteCall Integration

Create FileStorage RemoteCall contracts using the existing Aizen RemoteCall architecture.

## Goal

The FileStorage module must expose synchronous server-to-server contracts using the existing `IAizenRemoteCall` pattern.

Do not create a separate Refit client structure.
Do not create a custom external HTTP client abstraction if the repository already standardizes remote service calls through `IAizenRemoteCall`.

## First analyze existing RemoteCall pattern

Search the repository for:

```text
IAizenRemoteCall
AizenRemoteCallPost
AizenRemoteCallGet
AizenRemoteCallBody
AizenRemoteCallHeader
RemoteCall
```

Inspect examples similar to:

```csharp
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.Notification.Core.RemoteCall.Mail.Requests;
using Aizen.Modules.Notification.Core.RemoteCall.Mail.Responses;

namespace Aizen.Modules.Notification.Core.RemoteCall.Mail;

public interface ISendMailRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallPost("/token")]
    Task<SendLoginMailResponse> SendLoginRequest([AizenRemoteCallBody] SendLoginMailRequest request);

    [AizenRemoteCallPost("/token/refresh")]
    Task<SendRefreshTokenForAccesTokenMailResponse> SendRefreshTokenForAccessToken(
        [AizenRemoteCallBody] SendRefreshTokenForAccesTokenMailRequest request);

    [AizenRemoteCallPost("/v1/email/transactional/send")]
    Task<SendLoginMailResponse> SendMailRequest(
        [AizenRemoteCallBody] SendMailRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization);
}
```

Use the same conventions for namespace, folder location, request/response naming and DI registration.

## Folder location

Create FileStorage RemoteCall contracts under the current repository convention.

Preferred location if no stronger convention exists:

```text
Aizen.Modules.FileStorage.Abstraction/
  RemoteCall/
    File/
      IFileStorageRemoteCall.cs
      Requests/
      Responses/
```

If the repository convention uses `Core/RemoteCall`, follow it instead:

```text
Aizen.Modules.FileStorage.Core/
  RemoteCall/
```

Do not place RemoteCall contracts under `Repository`.
Do not place RemoteCall contracts under `Api/Controllers`.
Do not create Refit-specific clients.

## Required remote call interface

Create:

```text
IFileStorageRemoteCall
```

It must inherit:

```csharp
IAizenRemoteCall
```

## Required operations

Create remote call methods for synchronous operations:

```text
GetFileMetadata
ValidateFileOwnership
CreateReadUrl
CreateUploadSession
CompleteUploadSession
LinkFileToOwner
DeleteFile
```

Suggested route contracts:

```csharp
[AizenRemoteCallGet("/api/v1/file-storage/files/{fileId}")]
Task<GetFileMetadataRemoteCallResponse> GetFileMetadata(
    Guid fileId,
    [AizenRemoteCallHeader("Authorization")] string authorization);

[AizenRemoteCallPost("/api/v1/file-storage/files/{fileId}/validate-ownership")]
Task<ValidateFileOwnershipRemoteCallResponse> ValidateFileOwnership(
    Guid fileId,
    [AizenRemoteCallBody] ValidateFileOwnershipRemoteCallRequest request,
    [AizenRemoteCallHeader("Authorization")] string authorization);

[AizenRemoteCallPost("/api/v1/file-storage/files/{fileId}/read-url")]
Task<CreateFileReadUrlRemoteCallResponse> CreateReadUrl(
    Guid fileId,
    [AizenRemoteCallBody] CreateFileReadUrlRemoteCallRequest request,
    [AizenRemoteCallHeader("Authorization")] string authorization);

[AizenRemoteCallPost("/api/v1/file-storage/upload-sessions")]
Task<CreateUploadSessionRemoteCallResponse> CreateUploadSession(
    [AizenRemoteCallBody] CreateUploadSessionRemoteCallRequest request,
    [AizenRemoteCallHeader("Authorization")] string authorization);

[AizenRemoteCallPost("/api/v1/file-storage/upload-sessions/{uploadSessionId}/complete")]
Task<CompleteUploadSessionRemoteCallResponse> CompleteUploadSession(
    Guid uploadSessionId,
    [AizenRemoteCallBody] CompleteUploadSessionRemoteCallRequest request,
    [AizenRemoteCallHeader("Authorization")] string authorization);

[AizenRemoteCallPost("/api/v1/file-storage/files/{fileId}/link-owner")]
Task<LinkFileToOwnerRemoteCallResponse> LinkFileToOwner(
    Guid fileId,
    [AizenRemoteCallBody] LinkFileToOwnerRemoteCallRequest request,
    [AizenRemoteCallHeader("Authorization")] string authorization);
```

If `AizenRemoteCallDelete` exists, use it for delete:

```csharp
[AizenRemoteCallDelete("/api/v1/file-storage/files/{fileId}")]
Task<DeleteFileRemoteCallResponse> DeleteFile(
    Guid fileId,
    [AizenRemoteCallBody] DeleteFileRemoteCallRequest request,
    [AizenRemoteCallHeader("Authorization")] string authorization);
```

If `AizenRemoteCallDelete` does not exist, use the existing supported method attribute pattern and report the limitation.

## Request and response models

Create request/response models under:

```text
RemoteCall/File/Requests
RemoteCall/File/Responses
```

Required models:

```text
CreateUploadSessionRemoteCallRequest
CreateUploadSessionRemoteCallResponse
CompleteUploadSessionRemoteCallRequest
CompleteUploadSessionRemoteCallResponse
GetFileMetadataRemoteCallResponse
ValidateFileOwnershipRemoteCallRequest
ValidateFileOwnershipRemoteCallResponse
CreateFileReadUrlRemoteCallRequest
CreateFileReadUrlRemoteCallResponse
LinkFileToOwnerRemoteCallRequest
LinkFileToOwnerRemoteCallResponse
DeleteFileRemoteCallRequest
DeleteFileRemoteCallResponse
```

Request/response models should mirror existing FileStorage DTOs but must remain stable remote contracts.

Do not expose EF entities.
Do not expose Mongo documents.
Do not expose S3 credentials.
Do not permanently expose pre-signed URLs beyond response TTL.

## How other modules should use FileStorage

For Vessel document/media commands:

```text
Vessel Application -> IFileStorageRemoteCall -> FileStorage API
```

Use RemoteCall for:

```text
Validate FileId exists
Validate file ownership
Validate file status
Create temporary read URL
```

Use RabbitMQ/Aizen MessageBus for async events only:

```text
FileUploadedMessage
FileLinkedToOwnerMessage
FileProcessingRequestedMessage
FileDeletedMessage
OrphanFileCleanupRequestedMessage
```

## DI / configuration

Inspect the existing RemoteCall registration mechanism.

Add FileStorage RemoteCall registration only according to the existing architecture.

Do not invent a new registration method.

Report:

```text
1. RemoteCall registration location
2. Base URL configuration key
3. Auth header forwarding approach
4. Any missing framework support
```

## DocumentationInfo

Every RemoteCall interface, request and response class must include `DocumentationInfo`.

Example:

```csharp
[DocumentationInfo(
    Title = "FileStorage RemoteCall Contract",
    Description = "Defines synchronous server-to-server operations for FileStorage.",
    Purpose = "Allows modules such as Vessel to validate files, link ownership and request temporary access URLs without creating custom Refit clients.",
    Layer = "Abstraction.RemoteCall")]
public interface IFileStorageRemoteCall : IAizenRemoteCall
{
}
```

## Validation

Validate:

```text
[ ] No Refit/external client structure was created.
[ ] IFileStorageRemoteCall inherits IAizenRemoteCall.
[ ] RemoteCall methods use AizenRemoteCall attributes.
[ ] Request body uses AizenRemoteCallBody.
[ ] Authorization uses AizenRemoteCallHeader("Authorization").
[ ] Request/response models are under RemoteCall folders.
[ ] DTOs/entities/documents are not leaked incorrectly.
[ ] RabbitMQ messages still remain under Abstraction/Message.
[ ] Consumers still remain under Api/Consumers.
[ ] dotnet build succeeds.
```

## Final output

Report:

```text
1. RemoteCall examples found
2. Created RemoteCall interface
3. Created RemoteCall request/response models
4. Registration/configuration changes
5. Refit/external client removal or prevention
6. Build result
7. Remaining manual tasks
```
