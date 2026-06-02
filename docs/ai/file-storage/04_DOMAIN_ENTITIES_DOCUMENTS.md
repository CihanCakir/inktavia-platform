# 04 — Domain Entities and Documents

Create domain entities and documents.

## PostgreSQL entities

```text
FileEntity
FileVersionEntity
FileAccessPolicyEntity
FileOwnerReferenceEntity
FileUploadSessionEntity
FileProcessingJobEntity
```

## FileEntity

Purpose: main metadata record for an object stored in AWS S3.

Fields:

```text
Id, FileCode, OriginalFileName, StoredFileName, BucketName, ObjectKey, ContentType, Extension, SizeInBytes, Checksum, StorageProvider, Visibility, Category, Status, UploadedAt, UploadedByUserId, DeletedAt, DeletedByUserId, IsDeleted, CreatedAt, ModifiedAt
```

Domain methods:

```text
Create, MarkUploadUrlGenerated, MarkUploaded, MarkProcessing, MarkReady, Reject, SoftDelete, UpdateChecksum, UpdateVisibility
```

## FileOwnerReferenceEntity

Fields:

```text
FileId, OwnerModule, OwnerEntityType, OwnerEntityId, LinkedAt, LinkedByUserId, IsActive
```

## FileUploadSessionEntity

Fields:

```text
FileId, UploadSessionCode, BucketName, ObjectKey, ExpiresAt, Status, RequestedFileName, RequestedContentType, RequestedSizeInBytes, RequestedByUserId, ClientId, DeviceId, CompletedAt
```

## FileAccessPolicyEntity

Fields:

```text
FileId, Visibility, AllowedOwnerModule, AllowedOwnerEntityType, AllowedOperations, ExpiresAt, IsActive
```

## FileVersionEntity

Fields:

```text
FileId, VersionNo, BucketName, ObjectKey, SizeInBytes, Checksum, CreatedAt, CreatedByUserId
```

## FileProcessingJobEntity

Fields:

```text
FileId, ProcessingType, Status, RequestedAt, StartedAt, CompletedAt, ErrorCode, ErrorMessage, ResultDocumentId, RetryCount
```

## Mongo documents

Create under `Domain/Documents` and inherit from `AizenDocumentBase`:

```text
FileRichMetadataDocument
FileProcessingResultDocument
FileThumbnailMetadataDocument
```

## Repository interfaces

Create under `Domain/Interface/Repository`:

```text
IFileRepository
IFileVersionRepository
IFileOwnerReferenceRepository
IFileUploadSessionRepository
IFileAccessPolicyRepository
IFileProcessingJobRepository
IFileMetadataDocumentRepository
```

## Service interfaces

Create under `Domain/Interface/Service`:

```text
IObjectStorageProvider
IFileStorageService
IFileUploadSessionService
IFileValidationService
IFileAccessService
IFileOwnershipService
IFileProcessingService
IFileCacheKeyService
IFileCacheInvalidationService
```

Every entity, document and interface must include DocumentationInfo.
