# 05 — Repository Context, AWS S3 Provider and Services

Create Repository layer.

## FileStorageDbContext

Create `Aizen.Modules.FileStorage.Repository/Persistence/FileStorageDbContext.cs`.

Rules:

```text
Use same Aizen DbContext base as ReferenceData/Vessel.
Use schema: file_storage.
Add DbSet for PostgreSQL entities.
Do not include Mongo documents in EF context.
```

## EF configurations

Create configurations for:

```text
FileEntity
FileVersionEntity
FileAccessPolicyEntity
FileOwnerReferenceEntity
FileUploadSessionEntity
FileProcessingJobEntity
```

Important indexes:

```text
FileCode unique
ObjectKey unique
BucketName + ObjectKey unique
Checksum index
Status index
OwnerReference: OwnerModule + OwnerEntityType + OwnerEntityId
UploadSessionCode unique
ProcessingJob: FileId + ProcessingType + Status
```

## AWS S3 provider

Create:

```text
Repository/Providers/S3/S3ObjectStorageProvider.cs
Repository/Providers/S3/S3ObjectStorageOptions.cs
```

Implement `IObjectStorageProvider` with:

```csharp
Task<string> GenerateUploadUrlAsync(string bucketName, string objectKey, string contentType, TimeSpan expiresIn, CancellationToken cancellationToken = default);
Task<string> GenerateReadUrlAsync(string bucketName, string objectKey, TimeSpan expiresIn, CancellationToken cancellationToken = default);
Task<bool> ObjectExistsAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default);
Task<ObjectMetadataResult> GetObjectMetadataAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default);
Task DeleteObjectAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default);
```

Use AWS SDK for S3 according to the repository package standard.

## Repository services

Create:

```text
FileStorageService
FileUploadSessionService
FileValidationService
FileAccessService
FileOwnershipService
FileProcessingService
FileCacheKeyService
FileCacheInvalidationService
```

## Responsibilities

### FileUploadSessionService
Create file metadata in Created status, create upload session, generate S3 object key, generate S3 pre-signed upload URL, store requested file name/content type/size, set session expiration.

### FileStorageService
Complete upload session, check S3 object exists, validate content type/size/checksum, mark file Uploaded or Ready, publish processing message if needed.

### FileValidationService
Allowed content type validation, extension validation, max file size validation, checksum validation, category-specific validation.

### FileAccessService
Validate file access, generate pre-signed read URL, enforce visibility and ownership.

### FileOwnershipService
Link file to owner module/entity, validate owner reference, prevent invalid ownership changes.

### FileProcessingService
Create processing job, update processing job status, store Mongo processing result if needed, publish/consume processing messages.

## Redis/cache services

Use existing Aizen cache abstraction for upload session cache, signed URL cache, rate-limit state and processing status cache.

## DependencyInjection

Create/update `Aizen.Modules.FileStorage.Repository/DependencyInjection.cs` and register FileStorageDbContext, AWS S3 client/options/provider, repositories, repository services, Mongo repositories if used and cache services.
