# 03 — Abstraction Contracts and Message Contracts

Create all contracts under `Aizen.Modules.FileStorage.Abstraction`.

## Enums

Create under `Enum/`:

```text
StorageProviderType
FileStatus
FileVisibility
FileCategory
FileOwnerModule
FileProcessingType
FileProcessingStatus
UploadSessionStatus
FileAccessOperation
FileDeleteBehavior
```

Suggested values:

```csharp
public enum StorageProviderType { AwsS3 = 1, Minio = 2, Local = 3 }
public enum FileStatus { Created = 1, UploadUrlGenerated = 2, Uploaded = 3, Processing = 4, Ready = 5, Rejected = 6, Deleted = 7, Orphaned = 8 }
public enum FileVisibility { Private = 1, Internal = 2, Public = 3 }
public enum FileCategory { Image = 1, Document = 2, Video = 3, Invoice = 4, Certificate = 5, Report = 6, Other = 99 }
```

## DTOs

Create:

```text
FileDto
FileMetadataDto
FileUploadSessionDto
FileUploadUrlDto
FileAccessUrlDto
FileValidationResultDto
FileOwnerReferenceDto
FileProcessingJobDto
FileProcessingResultDto
```

Important fields:

```text
FileId, FileCode, OriginalFileName, StoredFileName, ObjectKey, BucketName, ContentType, Extension, SizeInBytes, Checksum, StorageProvider, OwnerModule, OwnerEntityType, OwnerEntityId, Visibility, Status, UploadedAt, UploadedByUserId
```

## Requests

Create:

```text
CreateUploadSessionRequest
CompleteUploadSessionRequest
CreateReadUrlRequest
ValidateFileOwnershipRequest
LinkFileToOwnerRequest
DeleteFileRequest
StartFileProcessingRequest
UpdateFileProcessingResultRequest
FilterFilesRequest
```

## Client contract

Create `IFileStorageClient` under `Client/` with:

```csharp
Task<FileMetadataDto?> GetFileMetadataAsync(Guid fileId, CancellationToken cancellationToken = default);
Task<FileValidationResultDto> ValidateFileOwnershipAsync(Guid fileId, string ownerModule, string ownerEntityType, Guid ownerEntityId, CancellationToken cancellationToken = default);
Task<FileAccessUrlDto> CreateReadUrlAsync(Guid fileId, TimeSpan expiresIn, CancellationToken cancellationToken = default);
```

## Message contracts

Create all message and result contracts under `Aizen.Modules.FileStorage.Abstraction/Message`.

All messages must inherit from `AizenBaseMessage`.
All result messages must inherit from `AizenMessageResult`.

### Request/response messages

```text
CreateUploadSessionProcessMessage
CreateUploadSessionProcessMessageResult
CompleteUploadSessionProcessMessage
CompleteUploadSessionProcessMessageResult
ValidateFileOwnershipProcessMessage
ValidateFileOwnershipProcessMessageResult
CreateFileReadUrlProcessMessage
CreateFileReadUrlProcessMessageResult
LinkFileToOwnerProcessMessage
LinkFileToOwnerProcessMessageResult
DeleteFileProcessMessage
DeleteFileProcessMessageResult
```

### Fire-and-forget messages

```text
FileUploadedMessage
FileProcessingRequestedMessage
FileVirusScanRequestedMessage
FileThumbnailRequestedMessage
FileMetadataExtractionRequestedMessage
FileProcessingCompletedMessage
FileLinkedToOwnerMessage
OrphanFileCleanupRequestedMessage
FileDeletedMessage
```

## Message design rules

- Use simple serializable properties.
- Do not put Stream or byte[] in messages.
- Use FileId, ObjectKey, BucketName, owner fields, processing type/status.
- Include UserId, ClientId, DeviceId only if needed and if IAizenInfoAccessor provides them.
- Every DTO, request, client, message and result class must include DocumentationInfo.
